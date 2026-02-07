using Labyrinth.Items;

namespace Labyrinth.Tiles
{
    /// A door tile in the labyrinth either locked with no key or opened with key (no "closed and unlocked" state).
    public class Door : Tile
    {
        public Door() : base(new Key()) =>
            _key = (Key)LocalInventory.Items.First();

        public override bool IsTraversable => IsOpened;

        /// True if the door is opened, false if closed and locked.
        public bool IsOpened => !IsLocked;

        /// True if the door is locked, false if unlocked and opened.
        public bool IsLocked => !LocalInventory.HasItems; // A key in the door

        /// Try to open the door with the first key of the provided inventory.
                public bool Open(LocalInventory keySource)
        {
            if (IsOpened)
            {
                throw new InvalidOperationException("Door is already unlocked.");
            }
            LocalInventory.MoveFirst(keySource);
            if (LocalInventory.Items.First() != _key)
            {
                keySource.MoveFirst(LocalInventory);
            }
            return IsOpened;
        }

        /// Lock the door and removes the key.
              public void LockAndTakeKey(LocalInventory whereKeyGoes)
        {
            if (IsLocked)
            {
                throw new InvalidOperationException("Door is already locked.");
            }
            whereKeyGoes.MoveFirst(LocalInventory);
        }

        private readonly Key _key;
    }
}
