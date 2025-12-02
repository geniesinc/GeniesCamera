using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// Contains all the material slot IDs available for the unified species.
    /// </summary>
    public static class UnifiedMaterialSlot
    {
        public const string Skin = "Skin";
        public const string Hair = "Hair";
        public const string FacialHair = "FacialHair";
        public const string Eyes = "Eyes";
        public const string Eyebrows = "Eyebrows";
        public const string Eyelashes = "Eyelashes";

        public static readonly IReadOnlyList<string> All = new List<string>
        {
            Skin,
            Hair,
            FacialHair,
            Eyes,
            Eyebrows,
            Eyelashes
        }.AsReadOnly();
        
        public static readonly Dictionary<string, string> MappedUmaIdentifiers = new Dictionary<string, string>
        {
            { Skin, "_Body_Translucency" },
            { Hair, "_Hair_Tiger" },
            { FacialHair, "FacialHair" },
            { Eyes, "_Eyes" },
            { Eyebrows, "_TexturedEyebrow" },
            { Eyelashes, "_TexturedEyelash" }
        };
    }
}