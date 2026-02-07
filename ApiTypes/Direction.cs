using System.Text.Json.Serialization;

namespace ApiTypes
{
    /// <summary>
    /// Specifies the four cardinal directions: North, East, South, and West.
    /// </summary>
       [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum Direction
    {

        North,

        East,

        South,

        West
    }
}
