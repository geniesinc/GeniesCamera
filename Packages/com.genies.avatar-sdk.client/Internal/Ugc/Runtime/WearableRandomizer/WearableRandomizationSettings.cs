using UnityEngine;

namespace Genies.Ugc
{
    /// <summary>
    /// Settings used by the <see cref="WearableRandomizer"/> when performing randomization.
    /// </summary>
    public struct WearableRandomizationSettings
    {
        /// <summary>
        /// Whether or not to also randomize materials.
        /// </summary>
        public bool RandomizeMaterials;

        /// <summary>
        /// The base color used for randomization. Generated patterns and colors will be related to this color
        /// based on rules of color theory. If null, a random base color will be generated instead.
        /// </summary>
        public Color? BaseColor;

        /// <summary>
        /// Contains some necessary data for the randomizer like the available patterns and materials.
        /// If not provided a default hardcoded one will be used. The default data may not be updated
        /// with the latest patterns and materials.
        /// </summary>
        public WearableRandomizerData Data;
    }
}
