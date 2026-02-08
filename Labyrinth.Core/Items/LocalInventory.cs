namespace Labyrinth.Items
{
	    public class LocalInventory : Inventory
	    {
	        /// <summary>
	        /// Creates a local (single-threaded) inventory.
	        /// </summary>
	        public LocalInventory(ICollectable? item = null) : base(item)
	        {
	        }

        /// <summary>
        /// Move first item from the source inventory in a single threaded context (ex: labyrinth building).
        /// </summary>
        public void MoveFirst(LocalInventory from)
        {
            if (!TryMoveItemsFrom(from, from.ItemTypes.Select((_, i) => i == 0).ToList()).Result)
            {
                throw new ArgumentException("Specified source inventory may be empty.");
            }
        }

        public override Task<bool> TryMoveItemsFrom(Inventory from, IList<bool> movesRequired)
        {
            if (from is not LocalInventory other)
            {
                throw new ArgumentException("Source inventory must be of type LocalInventory.", nameof(from));
            }
            bool ok = movesRequired.Count == other._items.Count;

            for (int i = movesRequired.Count; ok && i-- > 0;)
            {
                if (movesRequired[i])
                {
                    _items.Add(other._items[i]);
                    other._items.RemoveAt(i);
                }
            }
            return Task.FromResult(ok);
        }
    }
}
