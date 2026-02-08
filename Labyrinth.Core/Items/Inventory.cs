namespace Labyrinth.Items
{
    /// <summary>
    /// Inventory of collectable items for rooms and players.
    /// </summary>
    /// <param name="item">Optional initial item in the inventory.</param>
    public abstract class Inventory
    {
        protected Inventory(ICollectable? item = null)
        {
            if(item is not null)
            {
                _items.Add(item);
            }
        }

        /// <summary>
        /// True if the room has an items, false otherwise.
        /// </summary>
        public bool HasItems => _items.Count>0;

        /// <summary>
        /// Gets the type of the item in the room.
        /// </summary>
        public IEnumerable<Type> ItemTypes => _items.Select(item => item.GetType());

        /// <summary>
        /// Attempts to move selected items from the specified source inventory to the current inventory.
        /// </summary>
        public abstract Task<bool> TryMoveItemsFrom(
            Inventory source, 
            IList<bool> movesRequired
        );

        protected List<ICollectable> _items = new ();
    }
}
