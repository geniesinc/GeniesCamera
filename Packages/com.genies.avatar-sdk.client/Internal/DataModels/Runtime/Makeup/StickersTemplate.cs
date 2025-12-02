using Genies.MakeupPresets;
using UnityEngine;

namespace Genies.Models.Makeup
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "StickersTemplate", menuName = "Genies/Makeup/StickersTemplate")]
#endif
    public class StickersTemplate : MakeupTemplate, IDynamicAsset
    {
        public override MakeupPresetCategory Category => MakeupPresetCategory.Stickers;
    }
}
