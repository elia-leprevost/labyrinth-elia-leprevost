using System.Text.Json.Serialization;

namespace ApiTypes
{

    public class InventoryItem
    {

        public ItemType Type { get; init; }

        [JsonPropertyName("move-required")]
        public bool? MoveRequired { get; set; }
    }
}
