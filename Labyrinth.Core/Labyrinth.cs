using Labyrinth.Build;
using Labyrinth.Crawl;
using Labyrinth.Tiles;
using System.Text;

namespace Labyrinth
{
    public partial class Labyrinth
    {
        /// Labyrinth with walls, doors and collectable items.
        public Labyrinth(IBuilder builder)
        {
            builder.StartPositionFound+= (s, e) => _start = (e.X, e.Y);
            _tiles = builder.Build();
            if (_tiles.GetLength(0) < 3 || _tiles.GetLength(1) < 3)
            {
                throw new ArgumentException("Labyrinth must be at least 3x3");
            }
            if (_start == (-1, -1))
            {
                throw new ArgumentException("Labyrinth must have at least one starting position marked with x");
            }
        }

        /// Labyrinth width (number of columns).
        public int Width { get; private init; }

        /// Labyrinth height (number of rows).
        public int Height { get; private init; }

        /// An ascii representation of the labyrinth.
        public override string ToString()
        {
            var res = new StringBuilder();

            for (int y = 0; y < _tiles.GetLength(1); y++)
            {
                for (int x = 0; x < _tiles.GetLength(0); x++)
                {
                    res.Append(_tiles[x, y] switch
                    {
                        Unknown => '?',
                        Room => ' ',
                        Wall => '#',
                        Door => '/',
                        _ => throw new NotSupportedException("Unknown tile type")
                    });
                }
                res.AppendLine();
            }
            return res.ToString();
        }

        /// Instantiate a new crawler at the starting position.
        public ICrawler NewCrawler() =>
            new LabyrinthCrawler(_start.X, _start.Y, _tiles);

        private (int X, int Y) _start = (-1, -1);

        private readonly Tile[,] _tiles;
    }
}
