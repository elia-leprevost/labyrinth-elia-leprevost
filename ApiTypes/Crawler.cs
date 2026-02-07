using System.Text.Json.Serialization;

namespace ApiTypes
{
    /// Crawler information, including its position, direction, movement status, and items available.
    public class Crawler
    {
        /// The unique identifier of the crawler.
        public Guid Id { get; set; }

        ///The horizontal position of the crawler on the map
        [JsonPropertyName("x")]
        public int X { get; set; }

        /// The vertical position of the crawler on the map
        [JsonPropertyName("y")]
        public int Y { get; set; }

        ///The direction the crawler is currently facing.
        [JsonPropertyName("direction")]
        public Direction Dir { get; set; }

        /// A value indicating whether the crawler is currently walking. 
        public bool Walking { get; set; }

        /// The type of tile the crawler is currently facing.
        [JsonPropertyName("facing-tile")]
        public TileType FacingTile { get; set; }

        /// An optional list of items currently held in the crawler's bag. Can be empty but not null.
        public InventoryItem[]? Bag { get; set; }

        /// An optional list of items present at the crawler's current location. Can be empty but not null.
        public InventoryItem[]? Items { get; set; }
    }


}
