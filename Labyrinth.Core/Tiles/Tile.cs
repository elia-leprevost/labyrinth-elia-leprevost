using Labyrinth.Items;

namespace Labyrinth.Tiles
{
    /// Base class for all tiles in the labyrinth.
    public abstract class Tile(ICollectable? item = null)
    {
        /// Gets a value indicating whether the tile can be traversed.
        public abstract bool IsTraversable { get; }

        /// Actually pass through the tile. 
        public LocalInventory Pass()
        {
            if (!IsTraversable)
            {
                throw new InvalidOperationException("Cannot pass through a non-traversable tile.");
            }
            return LocalInventory;
        }

        protected MyInventory LocalInventory { get; private init; } = new (item);
    }
}
