using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Represents a <see cref="IGenie"/> instance that can be edited. Any editing will set the genie dirty,
    /// which means that <see cref="RebuildAsync"/> must be called to update the GameObject with the latest changes.
    /// </summary>
    public interface IEditableGenie : IGenie
    {
        // state
        bool IsDirty { get; }
        IReadOnlyCollection<OutfitAsset> OutfitAssets { get; }
        IReadOnlyCollection<IGenieMaterial> Materials { get; }

        UniTask RebuildAsync(bool forceRebuild = false, bool spreadCompute = false);

        // outfit
        UniTask AddOutfitAssetAsync(OutfitAsset asset);
        UniTask RemoveOutfitAssetAsync(OutfitAsset asset);
        void AddOutfitAssetProcessor(IOutfitAssetProcessor processor);
        void RemoveOutfitAssetProcessor(IOutfitAssetProcessor processor);

        // materials
        void AddMaterial(IGenieMaterial material);
        void RemoveMaterial(IGenieMaterial material);
        void ClearMaterialSlot(string slotId);
        bool TryGetMaterial(string slotId, out IGenieMaterial material);
        /// <summary>
        /// Tries to get the current material instance applied to the renderer for the given slot ID.
        /// Be aware that the returned instance may be destroyed every time the genie is rebuilt.
        /// </summary>
        bool TryGetSharedMaterial(string slotId, out Material material);

        // dna
        bool SetDna(string name, float value);
        float GetDna(string name);
        bool ContainsDna(string name);

        // blendshapes
        void SetBlendShape(string name, float value);
        void SetBlendShape(string name, float value, bool baked);
        float GetBlendShape(string name);
        bool RemoveBlendShape(string name);
        bool IsBlendShapeBaked(string name);
        bool ContainsBlendShape(string name);
    }
}
