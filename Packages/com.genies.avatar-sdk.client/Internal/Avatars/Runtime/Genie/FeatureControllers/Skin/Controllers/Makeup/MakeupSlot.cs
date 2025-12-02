using System.Collections.Generic;

namespace Genies.Avatars
{
    /// <summary>
    /// Contains all the makeup slot IDs available. Since this is specific to the decorated skin shader and not
    /// the unified species I'm including the file here rather than in the unified folder.
    /// </summary>
    public static class MakeupSlot
    {
        public const string Stickers = "Stickers";
        public const string Lipstick = "Lipstick";
        public const string Freckles = "Freckles";
        public const string FaceGems = "FaceGems";
        public const string Eyeshadow = "Eyeshadow";
        public const string Blush = "Blush";

        public static readonly IReadOnlyList<string> All = new List<string>
        {
            Stickers,
            Lipstick,
            Freckles,
            FaceGems,
            Eyeshadow,
            Blush
        }.AsReadOnly();
    }
}