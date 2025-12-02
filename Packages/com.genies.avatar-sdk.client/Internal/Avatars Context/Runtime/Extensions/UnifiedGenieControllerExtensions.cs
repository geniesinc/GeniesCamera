using System;
using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.ServiceManagement;
using Genies.FeatureFlags;
using Genies.MakeupPresets;
using Genies.Models.MaterialData;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars.Context
{
    public static class UnifiedGenieControllerExtensions
    {
        [Obsolete]
        public static UniTask BakeSkinAsync(this UnifiedGenieController controller)
        {
            //The implementation was removed
            return UniTask.CompletedTask;
        }

        // TODO this is a tmp method since for some reason we are not saving the color preset IDs on the decorated skin definition, we should aim to remove this
        public static bool IsMakeupColorEquipped(this UnifiedGenieController controller, MaterialDataMakeupColor materialData)
        {
            string slotId = materialData.makeupPresetCategory.ToString();
            if (!controller.MakeupColors.TryGetEquippedAsset(slotId, out _, out Ref<MakeupColorAsset> assetRef))
            {
                return false;
            }

            MakeupColorAsset makeupColorAsset = assetRef.Item;
            assetRef.Dispose();

            return materialData.makeupPresetCategory switch
            {
                MakeupPresetCategory.Stickers => false,
                MakeupPresetCategory.Lipstick => Compare(makeupColorAsset.Color1, materialData.IconColor)
                                                 && Compare(makeupColorAsset.Color2, materialData.IconColor2)
                                                 && Compare(makeupColorAsset.Color3, materialData.IconColor3),
                MakeupPresetCategory.Freckles => Compare(makeupColorAsset.Color1, materialData.IconColor),
                MakeupPresetCategory.FaceGems => Compare(makeupColorAsset.Color1, materialData.IconColor)
                                                 && Compare(makeupColorAsset.Color2, materialData.IconColor2)
                                                 && Compare(makeupColorAsset.Color3, materialData.IconColor3),
                MakeupPresetCategory.Eyeshadow => Compare(makeupColorAsset.Color1, materialData.IconColor)
                                                 && Compare(makeupColorAsset.Color2, materialData.IconColor2)
                                                 && Compare(makeupColorAsset.Color3, materialData.IconColor3),
                MakeupPresetCategory.Blush => Compare(makeupColorAsset.Color1, materialData.IconColor)
                                                 && Compare(makeupColorAsset.Color2, materialData.IconColor2)
                                                 && Compare(makeupColorAsset.Color3, materialData.IconColor3),
                _ => false
            };

            bool Compare(Color left, Color right)
                => ColorUtility.ToHtmlStringRGBA(left) == ColorUtility.ToHtmlStringRGBA(right);
        }
    }
}
