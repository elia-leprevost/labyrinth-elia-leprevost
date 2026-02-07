using System.Text.Json.Serialization;

namespace ApiTypes
{
    /// <summary>
    /// Labyrinth tile types.
    /// </summary>

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TileType
    {
        Outside,
        Room,
        Wall,
        Door
    }
}
