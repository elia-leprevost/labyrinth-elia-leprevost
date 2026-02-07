namespace Labyrinth.Tiles
{
    /// A wall tile in the labyrinth.
    public class Wall : Tile
    {
        private Wall() { }

        /// The singleton instance of the Wall class (memory optimization).
        public static Wall Singleton { get; } = new();

        public override bool IsTraversable => false;
    }
}
