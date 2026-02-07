namespace Labyrinth.Sys
{
    /// Random generator for enum types.
    public interface IEnumRandomizer<TEnum> where TEnum : struct, Enum
    {
        /// Generates a random value within an enum. 
        TEnum Next();
    }
}
