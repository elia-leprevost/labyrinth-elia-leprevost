using System.Collections.Concurrent;

namespace Labyrinth.Exploration;

/// <summary>
/// Stores discovered cells. Unknown cells are simply absent from the dictionary.
/// </summary>
public sealed class SharedMazeMap
{
    private readonly ConcurrentDictionary<Position, CellKind> _cells = new();

    public CellKind GetOrUnknown(Position p) => _cells.TryGetValue(p, out var k) ? k : CellKind.Unknown;

    /// <summary>
    /// Merge new knowledge. Returns true if the cell changed.
    /// </summary>
    public bool Upsert(Position p, CellKind kind)
    {
        static int Rank(CellKind k) => k switch
        {
            CellKind.Outside => 5,
            CellKind.Wall => 4,
            CellKind.Door => 3,
            CellKind.Room => 2,
            _ => 1
        };

        while (true)
        {
            if (_cells.TryGetValue(p, out var existing))
            {
                if (Rank(existing) >= Rank(kind)) return false;
                if (_cells.TryUpdate(p, kind, existing)) return true;
                continue;
            }

            if (_cells.TryAdd(p, kind)) return true;
        }
    }

    public IReadOnlyDictionary<Position, CellKind> Snapshot() =>
        new Dictionary<Position, CellKind>(_cells);

    public IEnumerable<Position> KnownPositions() => _cells.Keys;

    public static readonly (int dx, int dy)[] Neighbors4 =
    [
        (0, -1), (1, 0), (0, 1), (-1, 0)
    ];

    public IEnumerable<Position> GetFrontiers()
    {
        foreach (var kv in _cells)
        {
            if (kv.Value is not (CellKind.Room or CellKind.Door)) continue;

            foreach (var d in Neighbors4)
            {
                var n = kv.Key + d;
                if (!_cells.ContainsKey(n))
                {
                    yield return kv.Key;
                    break;
                }
            }
        }
    }
}
