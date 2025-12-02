using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;

namespace Genies.Avatars
{
    public interface IFlairController : IAssetSlotsController<FlairAsset>
    {
        Dictionary<string, FlairColorPreset> EquippedColorPresets { get; }
        Dictionary<string, FlairAsset> EquippedPresets { get; }
        public UniTask EquipColorPreset(string presetId, Color[] colorPreset, string slot);
        public  UniTask UnequipColorPreset(string slot);
    }
}
