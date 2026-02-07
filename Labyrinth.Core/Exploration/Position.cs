namespace Labyrinth.Exploration;

/// <summary>
/// Immutable 2D position (X,Y) used by the shared exploration map.
/// </summary>
public readonly record struct Position(int X, int Y)
{
    public static Position operator +(Position p, (int dx, int dy) d) => new(p.X + d.dx, p.Y + d.dy);
}
