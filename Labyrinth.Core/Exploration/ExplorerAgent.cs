using Labyrinth.Crawl;
using Labyrinth.Items;
using Labyrinth.Tiles;

namespace Labyrinth.Exploration;

/// <summary>
/// Async agent driving one ICrawler, using shared map + frontier assignment.
/// </summary>
public sealed class ExplorerAgent
{
    public ExplorerAgent(string id, ICrawler crawler, Inventory bag, MapCoordinator coordinator)
    {
        Id = id;
        _crawler = crawler;
        _bag = bag;
        _coord = coordinator;
    }

    public string Id { get; }
    public Position Position => new(_crawler.X, _crawler.Y);

    public async Task RunAsync(TimeSpan maxDuration, CancellationToken ct)
    {
        var start = DateTimeOffset.UtcNow;
        var rnd = new Random(Id.GetHashCode());

        while (!ct.IsCancellationRequested && DateTimeOffset.UtcNow - start < maxDuration)
        {
            var current = Position;

            // 1) Observe facing tile
            var facingType = await _crawler.FacingTileType;
            var facingKind = TileTypeToKind(facingType);

            await _coord.PublishAsync(new Observation(
                From: current,
                Dir: (_crawler.Direction.DeltaX, _crawler.Direction.DeltaY),
                FacingKind: facingKind,
                MovedTo: null
            ), ct);

            // 2) If we can walk, do it (greedy)
            if (facingKind is CellKind.Room or CellKind.Door)
            {
                var inv = await _crawler.TryWalk(_bag);
                if (inv is not null)
                {
                    var movedTo = Position;
                    await _coord.PublishAsync(new Observation(
                        From: current,
                        Dir: (_crawler.Direction.DeltaX, _crawler.Direction.DeltaY),
                        FacingKind: facingKind,
                        MovedTo: movedTo
                    ), ct);
                    // Small delay to avoid spamming
                    await Task.Delay(10, ct);
                    continue;
                }
            }

            // 3) Otherwise, navigate to an assigned frontier
            var frontier = _coord.ReserveFrontier(Id, current);
            if (frontier is null)
            {
                // Nothing to do, random turn
                if (rnd.NextDouble() < 0.5) _crawler.Direction.TurnLeft();
                else _crawler.Direction.TurnRight();
                await Task.Delay(10, ct);
                continue;
            }

            var snapshot = _coord.Snapshot();
            var path = Pathfinder.BfsPath(snapshot, current, frontier.Value);

            if (path.Count < 2)
            {
                // Can't reach, release and random turn
                _coord.ReleaseReservation(Id, frontier.Value);
                if (rnd.NextDouble() < 0.5) _crawler.Direction.TurnLeft();
                else _crawler.Direction.TurnRight();
                await Task.Delay(10, ct);
                continue;
            }

            var next = path[1];
            await TurnTowardAsync(current, next, ct);

            // attempt walk after turning
            var inv2 = await _crawler.TryWalk(_bag);
            if (inv2 is not null)
            {
                var movedTo = Position;
                await _coord.PublishAsync(new Observation(
                    From: current,
                    Dir: (_crawler.Direction.DeltaX, _crawler.Direction.DeltaY),
                    FacingKind: facingKind,
                    MovedTo: movedTo
                ), ct);
            }
            else
            {
                // blocked => mark as wall if we didn't already know
                var blocked = current + (_crawler.Direction.DeltaX, _crawler.Direction.DeltaY);
                await _coord.PublishAsync(new Observation(
                    From: current,
                    Dir: (_crawler.Direction.DeltaX, _crawler.Direction.DeltaY),
                    FacingKind: CellKind.Wall,
                    MovedTo: null
                ), ct);
            }

            // If we reached the frontier cell, release reservation
            if (Position.Equals(frontier.Value))
                _coord.ReleaseReservation(Id, frontier.Value);

            await Task.Delay(10, ct);
        }
    }

    private async Task TurnTowardAsync(Position from, Position to, CancellationToken ct)
    {
        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        (int dx, int dy) desired = dx switch
        {
            1 => (1, 0),
            -1 => (-1, 0),
            _ => dy switch
            {
                1 => (0, 1),
                -1 => (0, -1),
                _ => (0, 0)
            }
        };
        if (desired == (0, 0)) return;

        // rotate until direction matches; remote crawler updates direction cache through FacingTileType
        for (var i = 0; i < 4; i++)
        {
            if ((_crawler.Direction.DeltaX, _crawler.Direction.DeltaY) == desired) break;
            _crawler.Direction.TurnLeft();
            _ = await _crawler.FacingTileType; // sync remote direction if needed
        }
    }

    private static CellKind TileTypeToKind(Type t) =>
        t == typeof(Wall) ? CellKind.Wall :
        t == typeof(Room) ? CellKind.Room :
        t == typeof(Door) ? CellKind.Door :
        t == typeof(Outside) ? CellKind.Outside :
        CellKind.Unknown;

    private readonly ICrawler _crawler;
    private readonly Inventory _bag;
    private readonly MapCoordinator _coord;
}
