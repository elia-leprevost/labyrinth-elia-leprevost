using System.Text.Json.Serialization;

namespace ApiTypes
{
    /// <summary>
    /// Settings used to configure the crawler and the labyrinth (first crawler only).
    /// </summary>
    public class Settings
    {

        [JsonPropertyName("random-seed")]
        public int? RandomSeed { get; set; }

        [JsonPropertyName("corridor-walls")]
        public int[]? CorridorWalls { get; set; }

        [JsonPropertyName("wall-doors")]
        public int[][]? WallDoors { get; set; }

        [JsonPropertyName("key-rooms")]
        public int[][]? KeyRooms { get; set; }

        private HashCode HashCodeAdd<T>(HashCode hash, T val)
        {
            hash.Add(val);
            return hash;
        }
        private void AggregateArrays<T>(HashCode hash, T[][]? arrays) =>
            arrays?.SelectMany(arr => arr).Aggregate(hash, HashCodeAdd);

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();

            hash.Add(RandomSeed ?? 0);
            CorridorWalls?.Aggregate(hash, HashCodeAdd);
            AggregateArrays(hash, WallDoors);
            AggregateArrays(hash, KeyRooms);
            return hash.ToHashCode();
        }
        public override bool Equals(object? obj) => 
            obj is Settings other &&
            GetHashCode() == other.GetHashCode();
    }
}
