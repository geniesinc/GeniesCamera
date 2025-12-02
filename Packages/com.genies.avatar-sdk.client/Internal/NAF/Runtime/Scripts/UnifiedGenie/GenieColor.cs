using UnityEngine;

namespace Genies.Naf
{
    public static class GenieColor
    {
        public const string Skin = "unified-skin";

        // eyes
        public const string LeftEyeSclera      = "unified-leftEyeSclera";
        public const string LeftEyeRing        = "unified-leftEyeRing";
        public const string LeftEyeIrisMain    = "unified-leftEyeIrisMain";
        public const string LeftEyeIrisCenter  = "unified-leftEyeIrisCenter";
        public const string LeftEyePupil       = "unified-leftEyePupil";
        public const string RightEyeSclera     = "unified-rightEyeSclera";
        public const string RightEyeRing       = "unified-rightEyeRing";
        public const string RightEyeIrisMain   = "unified-rightEyeIrisMain";
        public const string RightEyeIrisCenter = "unified-rightEyeIrisCenter";
        public const string RightEyePupil      = "unified-rightEyePupil";

        // makeup
        public const string Freckles     = "unified-freckles";
        public const string BlushAll     = "unified-blushAll";
        public const string Blush1       = "unified-blush1";
        public const string Blush2       = "unified-blush2";
        public const string Blush3       = "unified-blush3";
        public const string LipstickAll  = "unified-lipstickAll";
        public const string Lipstick1    = "unified-lipstick1";
        public const string Lipstick2    = "unified-lipstick2";
        public const string Lipstick3    = "unified-lipstick3";
        public const string EyeshadowAll = "unified-eyeshadowAll";
        public const string Eyeshadow1   = "unified-eyeshadow1";
        public const string Eyeshadow2   = "unified-eyeshadow2";
        public const string Eyeshadow3   = "unified-eyeshadow3";
        public const string FaceGemsAll  = "unified-faceGemsAll";
        public const string FaceGems1    = "unified-faceGems1";
        public const string FaceGems2    = "unified-faceGems2";
        public const string FaceGems3    = "unified-faceGems3";

        // hair
        public const string HairAll  = "gear-hairAll";
        public const string HairBase = "gear-hairBase";
        public const string HairR    = "gear-hairR";
        public const string HairG    = "gear-hairG";
        public const string HairB    = "gear-hairB";

        // facial hair
        public const string FacialhairAll  = "gear-facialhairAll";
        public const string FacialhairBase = "gear-facialhairBase";
        public const string FacialhairR    = "gear-facialhairR";
        public const string FacialhairG    = "gear-facialhairG";
        public const string FacialhairB    = "gear-facialhairB";

        // eyebrows
        public const string EyebrowsAll  = "gear-eyebrowsAll";
        public const string EyebrowsBase = "gear-eyebrowsBase";
        public const string EyebrowsR    = "gear-eyebrowsR";
        public const string EyebrowsG    = "gear-eyebrowsG";
        public const string EyebrowsB    = "gear-eyebrowsB";

        // eyelashes
        public const string EyelashesAll  = "gear-eyelashesAll";
        public const string EyelashesBase = "gear-eyelashesBase";
        public const string EyelashesR    = "gear-eyelashesR";
        public const string EyelashesG    = "gear-eyelashesG";
        public const string EyelashesB    = "gear-eyelashesB";
    }

    public struct GenieColorEntry
    {
        public string ColorId;
        public Color? Value;

        public GenieColorEntry(string colorId = null, Color? value = null)
        {
            ColorId = colorId;
            Value   = value;
        }
    }
}
