using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.Avatars
{
    public interface IBlendShapeController : IAssetsController<BlendShapeAsset>
    {
        UniTask LoadAndEquipPresetAsync(string assetId);
        UniTask EquipPresetAsync(BlendShapePresetAsset preset);
        string GetEquippedBlendShapeForSlot(string slot);
        UniTask<bool> IsPresetEquippedAsync(string presetId);
        bool IsPresetEquipped(BlendShapePresetAsset preset);
        bool IsPresetEquipped(IEnumerable<string> assetIds);
    }
}