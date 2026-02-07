using Labyrinth.Items;

namespace Labyrinth.Tiles
{
    /// A room in the labyrinth.
    public class Room(ICollectable? item = null) : Tile(item)
    {
        public override bool IsTraversable => true;
    }
}
