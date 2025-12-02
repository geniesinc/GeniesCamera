
namespace Genies.Avatars.Behaviors
{
    /// <summary>
    /// Utility class for resolving avatar generation/race labels used throughout the avatar behavior system.
    /// This class provides access to current and previous generation identifiers for avatar compatibility.
    /// </summary>
    public static class RaceLabelResolver
    {
        private const string PreviousGen = "gen6";
        private const string CurrentGen = "gen12";

        /// <summary>
        /// Gets the label identifier for the previous avatar generation.
        /// </summary>
        /// <returns>A string representing the previous generation label (gen6).</returns>
        public static string GetPreviousRaceLabel()
        {
            return PreviousGen;
        }

        /// <summary>
        /// Gets the label identifier for the current avatar generation.
        /// </summary>
        /// <returns>A string representing the current generation label (gen12).</returns>
        public static string GetRaceLabel()
        {
            return CurrentGen;
        }
    }
}
