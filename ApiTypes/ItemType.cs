using System.Text.Json.Serialization;

namespace ApiTypes
{

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ItemType
    {

        Key
    }
}
