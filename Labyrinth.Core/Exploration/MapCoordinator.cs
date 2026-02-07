using System.Threading.Channels;

namespace Labyrinth.Exploration;

public sealed record Observation(Position From, (int dx, int dy) Dir, CellKind FacingKind, Position? MovedTo);

/// <summary>
/// Single-writer coordinator: receives observations from multiple agents and updates the shared map.
/// It also maintains frontier reservations to reduce overlap between agents.
/// </summary>
public sealed class MapCoordinator : IAsyncDisposable
{
    private readonly SharedMazeMap _map = new();
    private readonly Channel<Observation> _obs = Channel.CreateUnbounded<Observation>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false
    });

    private readonly Dictionary<Position, string> _reservations = new();
    private readonly CancellationTokenSource _cts = new();
    private Task? _loop;

    public SharedMazeMap Map => _map;

    public void Start()
    {
        _loop ??= Task.Run(LoopAsync);
    }

    public ValueTask PublishAsync(Observation obs, CancellationToken ct) =>
        _obs.Writer.WriteAsync(obs, ct);

    public IReadOnlyDictionary<Position, CellKind> Snapshot() => _map.Snapshot();

    public Position? ReserveFrontier(string agentId, Position agentPos, Func<Position, bool>? accept = null)
    {
        var frontiers = _map.GetFrontiers().Distinct().ToList();
        if (frontiers.Count == 0) return null;

        Position? best = null;
        var bestScore = int.MaxValue;

        foreach (var f in frontiers)
        {
            if (accept is not null && !accept(f)) continue;

            if (_reservations.TryGetValue(f, out var owner) && owner != agentId) continue;

            var score = Math.Abs(f.X - agentPos.X) + Math.Abs(f.Y - agentPos.Y);
            if (score < bestScore)
            {
                bestScore = score;
                best = f;
            }
        }

        if (best is null) return null;

        _reservations[best.Value] = agentId;
        return best;
    }

    public void ReleaseReservation(string agentId, Position frontier)
    {
        if (_reservations.TryGetValue(frontier, out var owner) && owner == agentId)
            _reservations.Remove(frontier);
    }

    private async Task LoopAsync()
    {
        await foreach (var o in _obs.Reader.ReadAllAsync(_cts.Token))
        {
            _map.Upsert(o.From, CellKind.Room);

            var facing = o.From + o.Dir;
            _map.Upsert(facing, o.FacingKind);

            if (o.MovedTo is { } to)
                _map.Upsert(to, CellKind.Room);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _cts.Cancel();
        _obs.Writer.TryComplete();
        if (_loop is not null)
        {
            try { await _loop; } catch { /* ignore */ }
        }
        _cts.Dispose();
    }
}
