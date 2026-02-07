namespace Labyrinth.Exploration;

/// <summary>
/// Simple BFS shortest path on known traversable cells.
/// </summary>
public static class Pathfinder
{
    public static IReadOnlyList<Position> BfsPath(
        IReadOnlyDictionary<Position, CellKind> map,
        Position start,
        Position goal,
        Func<CellKind, bool>? isTraversable = null)
    {
        isTraversable ??= (k => k is CellKind.Room or CellKind.Door);

        if (start.Equals(goal)) return new[] { start };

        var q = new Queue<Position>();
        var prev = new Dictionary<Position, Position>();

        q.Enqueue(start);
        prev[start] = start;

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            foreach (var d in SharedMazeMap.Neighbors4)
            {
                var nxt = cur + d;
                if (prev.ContainsKey(nxt)) continue;

                if (!map.TryGetValue(nxt, out var kind)) continue; // unknown => not traversable
                if (!isTraversable(kind)) continue;

                prev[nxt] = cur;
                if (nxt.Equals(goal))
                    return Reconstruct(prev, start, goal);

                q.Enqueue(nxt);
            }
        }

        return Array.Empty<Position>();
    }

    private static IReadOnlyList<Position> Reconstruct(Dictionary<Position, Position> prev, Position start, Position goal)
    {
        var path = new List<Position>();
        var cur = goal;
        while (!cur.Equals(start))
        {
            path.Add(cur);
            cur = prev[cur];
        }
        path.Add(start);
        path.Reverse();
        return path;
    }
}
