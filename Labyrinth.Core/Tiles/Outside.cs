namespace Labyrinth.Tiles
{
    /// Outside tile used for labyrinth bounds detection only, not for walkthrough.
    public class Outside : Tile
    {
        private Outside() { }

        /// The singleton instance of the Wall class (memory optimization).
        public static Outside Singleton { get; } = new();

        public override bool IsTraversable => false;
    }
}
