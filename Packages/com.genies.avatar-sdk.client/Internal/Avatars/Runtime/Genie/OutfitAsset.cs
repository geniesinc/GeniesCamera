using System;
using UMA;
using Object = UnityEngine.Object;

namespace Genies.Avatars
{
    /// <summary>
    /// Contains all the resources for equipping an asset to an avatar's outfit.
    /// </summary>
    public sealed class OutfitAsset : IAsset, IDisposable
    {
        public string Id => Metadata.Id;
        public string GenieType { get; }
        public string Lod { get; }
        public bool IsDisposed { get; private set; }

        // alternative mesh and material data for the non-uma system
        public MeshAsset[] MeshAssets
        {
            get => GetMeshAssets();
            set => _meshAssets = value;
        }
        public MeshAssetTriangleFlags[] HiddenTriangles => GetHiddenTriangles();

        public readonly OutfitAssetMetadata Metadata;
        public readonly UMATextRecipe Recipe;
        public readonly SlotDataAsset[] Slots;
        public readonly OverlayDataAsset[] Overlays;
        public readonly IGenieComponentCreator[] ComponentCreators;

        private readonly IDisposable _dependencies;
        private readonly IndexedAssets _indexedAssets;

        private MeshAsset[] _meshAssets;
        private MeshAssetTriangleFlags[] _hiddenTriangles;

        public OutfitAsset(string genieType, string lod, OutfitAssetMetadata metadata,
            UMATextRecipe recipe, SlotDataAsset[] slots,
            OverlayDataAsset[] overlays, IGenieComponentCreator[] componentCreators,
            IDisposable dependencies,
            MeshAsset[] meshAssets = null,
            MeshAssetTriangleFlags[] hiddenTriangles = null)
        {
            GenieType = genieType;
            Lod = lod;
            Metadata = metadata;
            Recipe = recipe;
            Slots = slots;
            Overlays = overlays;
            ComponentCreators = componentCreators;
            _meshAssets = meshAssets;
            _hiddenTriangles = hiddenTriangles;

            _dependencies = dependencies;
            _indexedAssets = new IndexedAssets();

            IndexAssets();
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            if (MeshAssets is not null)
            {
                foreach (MeshAsset asset in MeshAssets)
                {
                    if (asset.Material)
                    {
                        Object.Destroy(asset.Material);
                    }
                }
            }

            Array.Clear(MeshAssets, 0, MeshAssets.Length);
            Array.Clear(_meshAssets, 0, _meshAssets.Length);
            _meshAssets = null;
            MeshAssets = null;
            _indexedAssets.ReleaseAll();
            _dependencies?.Dispose();
            IsDisposed = true;
        }

        private void IndexAssets()
        {
            if (IsDisposed || GenieType == GenieTypeName.NonUma)
            {
                return;
            }

            _indexedAssets.Index(Recipe);
            _indexedAssets.Index(Slots);
            _indexedAssets.Index(Overlays);
        }

        private MeshAsset[] GetMeshAssets()
        {
            if (_meshAssets is not null)
            {
                return _meshAssets;
            }

            _meshAssets = MeshAssetUtility.CreateMeshAssetsFrom(this).ToArray();
            return _meshAssets;
        }

        private MeshAssetTriangleFlags[] GetHiddenTriangles()
        {
            if (_hiddenTriangles is not null)
            {
                return _hiddenTriangles;
            }

            _hiddenTriangles = MeshAssetUtility.CreateTriangleFlagsFrom(Recipe.MeshHideAssets).ToArray();
            return _hiddenTriangles;
        }
    }
}
