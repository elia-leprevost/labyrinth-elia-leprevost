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

        var blockedDoors = new HashSet<Position>();

        var skippedFrontiers = new HashSet<Position>();

        while (!ct.IsCancellationRequested && DateTimeOffset.UtcNow - start < maxDuration)
        {
            var current = Position;

            var facingType = await _crawler.FacingTileType;
            var facingKind = TileTypeToKind(facingType);

            await _coord.PublishAsync(new Observation(
                From: current,
                Dir: (_crawler.Direction.DeltaX, _crawler.Direction.DeltaY),
                FacingKind: facingKind,
                MovedTo: null
            ), ct);

            if (facingKind is CellKind.Room or CellKind.Door)
            {
                var inv = await _crawler.TryWalk(_bag);
                if (inv is not null)
                {
                    await _bag.TryMoveItemsFrom(
                        inv,
                        inv.ItemTypes.Select(_ => true).ToList()
                    );

                    if (_bag.HasItems)
                    {
                        blockedDoors.Clear();
                        skippedFrontiers.Clear();
                    }

                    var movedTo = Position;
                    await _coord.PublishAsync(new Observation(
                        From: current,
                        Dir: (_crawler.Direction.DeltaX, _crawler.Direction.DeltaY),
                        FacingKind: facingKind,
                        MovedTo: movedTo
                    ), ct);

                    await Task.Delay(10, ct);
                    continue;
                }
            }

            Position? frontier = null;

            for (var attempt = 0; attempt < 8 && frontier is null; attempt++)
            {
                var candidate = _coord.ReserveFrontier(Id, current);
                if (candidate is null) break;

                var kind = _coord.Map.GetOrUnknown(candidate.Value);

                if (skippedFrontiers.Contains(candidate.Value))
                {
                    _coord.ReleaseReservation(Id, candidate.Value);
                    continue;
                }

                if (!_bag.HasItems && kind == CellKind.Door)
                {
                    skippedFrontiers.Add(candidate.Value);
                    _coord.ReleaseReservation(Id, candidate.Value);
                    continue;
                }

                if (kind == CellKind.Door && blockedDoors.Contains(candidate.Value))
                {
                    skippedFrontiers.Add(candidate.Value);
                    _coord.ReleaseReservation(Id, candidate.Value);
                    continue;
                }

                frontier = candidate;
            }

            if (frontier is null)
            {
                if (rnd.NextDouble() < 0.5) _crawler.Direction.TurnLeft();
                else _crawler.Direction.TurnRight();
                await Task.Delay(10, ct);
                continue;
            }

            var snapshot = _coord.Snapshot();

            Func<CellKind, bool> traversable =
                _bag.HasItems
                    ? (k => k is CellKind.Room or CellKind.Door)
                    : (k => k is CellKind.Room);

            var path = Pathfinder.BfsPath(snapshot, current, frontier.Value, traversable);

            if (path.Count < 2)
            {
                _coord.ReleaseReservation(Id, frontier.Value);

                skippedFrontiers.Add(frontier.Value);

                if (rnd.NextDouble() < 0.5) _crawler.Direction.TurnLeft();
                else _crawler.Direction.TurnRight();
                await Task.Delay(10, ct);
                continue;
            }

            var next = path[1];
            await TurnTowardAsync(current, next, ct);

            var facingTypeNow = await _crawler.FacingTileType;
            var facingKindNow = TileTypeToKind(facingTypeNow);

            var inv2 = await _crawler.TryWalk(_bag);
            if (inv2 is not null)
            {
                await _bag.TryMoveItemsFrom(
                    inv2,
                    inv2.ItemTypes.Select(_ => true).ToList()
                );

                if (_bag.HasItems)
                {
                    blockedDoors.Clear();
                    skippedFrontiers.Clear();
                }

                var movedTo = Position;
                await _coord.PublishAsync(new Observation(
                    From: current,
                    Dir: (_crawler.Direction.DeltaX, _crawler.Direction.DeltaY),
                    FacingKind: facingKindNow,
                    MovedTo: movedTo
                ), ct);
            }
            else
            {
                var realFacingType = await _crawler.FacingTileType;
                var realFacingKind = TileTypeToKind(realFacingType);

                await _coord.PublishAsync(new Observation(
                    From: current,
                    Dir: (_crawler.Direction.DeltaX, _crawler.Direction.DeltaY),
                    FacingKind: realFacingKind,
                    MovedTo: null
                ), ct);

                if (realFacingKind == CellKind.Door)
                {
                    var doorPos = current + (_crawler.Direction.DeltaX, _crawler.Direction.DeltaY);
                    blockedDoors.Add(doorPos);
                }

                if (rnd.NextDouble() < 0.5) _crawler.Direction.TurnLeft();
                else _crawler.Direction.TurnRight();
            }
      
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

        
        for (var i = 0; i < 4; i++)
        {
            if ((_crawler.Direction.DeltaX, _crawler.Direction.DeltaY) == desired) break;
            _crawler.Direction.TurnLeft();
            _ = await _crawler.FacingTileType; 
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
