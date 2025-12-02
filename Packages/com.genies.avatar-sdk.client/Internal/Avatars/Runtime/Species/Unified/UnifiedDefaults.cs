using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
    public static class UnifiedDefaults
    {
        // TODO we should probably set defaults through a default definition
        public const string DefaultHairColor = "HairMaterialData_RegBrownMedium";
        public const string DefaultFacialHairColor = "FacialHairMaterialData_RegBrownMedium";
        public const string DefaultEyesColor = "EyeMaterialData_NewBrown";
        public const string DefaultEyebrowsColor = "EyeBrowMaterialData_Black";
        public const string DefaultSkinColor = "SkinMaterialData_skin0007";
        public const string DefaultFacePresetId = "_female_varSilverF";
        public const string DefaultBodyVariation = "female";

        public const string DefaultEyebrowGear = "recDek4ZfQNK9KcqJ";
        public const string DefaultEyebrowTexturePreset = "recrbUJhA7aklbgHC";
        public const string DefaultEyebrowColorPreset = "recGqHxAAQ0ZDrDNL";
        public const string DefaultEyelashGear = "rec5IKMKSvajB7Con";
        public const string DefaultEyelashTexturePreset = "recZN6LlKiskbzCpq";
        public const string DefaultEyelashColorPreset = "recfPMJeJVWcOOZ8V";
        public static readonly string[] DefaultEyebrowColors = new string[]
        {
            $"#{ColorUtility.ToHtmlStringRGBA(Color.black)}",
            $"#{ColorUtility.ToHtmlStringRGBA(Color.black)}",
            $"#{ColorUtility.ToHtmlStringRGBA(Color.black)}",
            $"#{ColorUtility.ToHtmlStringRGBA(Color.black)}",
        };
        public static readonly string[] DefaultEyelashColors = new string[]
        {
            $"#{ColorUtility.ToHtmlStringRGBA(Color.black)}",
            $"#{ColorUtility.ToHtmlStringRGBA(Color.black)}",
            $"#{ColorUtility.ToHtmlStringRGBA(Color.black)}",
            $"#{ColorUtility.ToHtmlStringRGBA(Color.black)}",
        };

        public readonly static IReadOnlyList<(string assetId, string slotId)> DefaultMakeupColors = new List<(string assetId, string slotId)>
        {
            ("Lip_1_MaterialDataMakeupColor",      MakeupSlot.Lipstick),
            ("Freckles_1_MaterialDataMakeupColor", MakeupSlot.Freckles),
            ("FaceGems_1_MaterialDataMakeupColor", MakeupSlot.FaceGems),
            ("Shadow_10_MaterialDataMakeupColor",  MakeupSlot.Eyeshadow),
            ("Blush_1_MaterialDataMakeupColor",    MakeupSlot.Blush),
        }.AsReadOnly();
    }
}
