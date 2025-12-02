using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Utilities;

namespace Genies.Avatars
{
    internal sealed class EditableGenieOutfitManager : IDisposable
    {
        public IReadOnlyCollection<OutfitAsset> OutfitAssets => _outfitAssets;

        // dependencies
        private readonly EditableGenie _genie;
        private readonly MeshBuilder _meshBuilder;

        // state
        private readonly TrackedHashSet<OutfitAsset> _outfitAssets;
        private readonly HashSet<IOutfitAssetProcessor> _outfitAssetProcessors;
        private readonly Dictionary<OutfitAsset, UniTaskCompletionSource> _assetLocks;
        private readonly Dictionary<OutfitAsset, List<GenieComponent>> _assetComponents;

        public EditableGenieOutfitManager(EditableGenie genie, MeshBuilder meshBuilder)
        {
            _genie = genie;
            _meshBuilder = meshBuilder;

            _outfitAssets = new TrackedHashSet<OutfitAsset>();
            _outfitAssetProcessors = new HashSet<IOutfitAssetProcessor>();
            _assetLocks = new Dictionary<OutfitAsset, UniTaskCompletionSource>();
            _assetComponents = new Dictionary<OutfitAsset, List<GenieComponent>>();

            _outfitAssets.BeginTracking();
        }

        public async UniTask AddOutfitAssetAsync(OutfitAsset asset)
        {
            if (asset == null)
            {
                return;
            }

            // lock the asset so any calls to Add/Remove it will await until its unlocked (if the asset is locked this will await for unlock)
            await LockAssetAsync(asset);

            if (_outfitAssets.Contains(asset))
            {
                UnlockAsset(asset);
                return;
            }

            // process the outfit asset
            await UniTask.WhenAll(_outfitAssetProcessors.Select(processor => processor.ProcessAddedAssetAsync(asset)));

            // add the mesh assets to the mesh builder
            _meshBuilder.Add(asset.MeshAssets);

            // add the asset and unlock it
            _outfitAssets.Add(asset);
            UnlockAsset(asset);
        }

        public async UniTask RemoveOutfitAssetAsync(OutfitAsset asset)
        {
            if (asset == null)
            {
                return;
            }

            // lock the asset so any calls to Add/Remove it will await until its unlocked (if the asset is locked this will await for unlock)
            await LockAssetAsync(asset);

            if (!_outfitAssets.Contains(asset))
            {
                UnlockAsset(asset);
                return;
            }

            // process the outfit asset
            await UniTask.WhenAll(_outfitAssetProcessors.Select(processor => processor.ProcessRemovedAssetAsync(asset)));

            // clear the mesh assets from the mesh builder
            _meshBuilder.Remove(asset.MeshAssets);

            // remove the asset and unlock it
            _outfitAssets.Remove(asset);
            UnlockAsset(asset);
        }
        
        public async UniTask ReApplyOutfitAssets()
        {
            foreach (var asset in _outfitAssets)
            {
                // process the outfit asset
                await UniTask.WhenAll(_outfitAssetProcessors.Select(processor => processor.ProcessAddedAssetAsync(asset)));
            
                // add the mesh assets to the mesh builder
                _meshBuilder.Add(asset.MeshAssets);
            }
        }

        public void RefreshAssetComponents()
        {
            // finish tracking so we get what assets were added and removed from the previous refresh
            _outfitAssets.FinishTracking();

            // remove components from unequipped assets
            foreach (OutfitAsset asset in _outfitAssets.RemovedItems)
            {
                RemoveAssetComponents(asset);
            }

            // add components from equipped assets
            foreach (OutfitAsset asset in _outfitAssets.AddedItems)
            {
                AddAssetComponents(asset);
            }

            // start tracking for the next refresh
            _outfitAssets.BeginTracking();
        }

        public void AddOutfitAssetProcessor(IOutfitAssetProcessor processor)
        {
            _outfitAssetProcessors.Add(processor);
        }

        public void RemoveOutfitAssetProcessor(IOutfitAssetProcessor processor)
        {
            _outfitAssetProcessors.Remove(processor);
        }

        public void Dispose()
        {
            foreach (OutfitAsset asset in _outfitAssets)
            {
                asset?.Dispose();
            }

            foreach (OutfitAsset asset in _assetLocks.Keys)
            {
                asset?.Dispose();
            }

            foreach (OutfitAsset asset in _assetComponents.Keys)
            {
                asset?.Dispose();
            }

            _outfitAssets.Clear();
            _outfitAssetProcessors.Clear();
            _assetLocks.Clear();
            _assetComponents.Clear();
        }

        private void RemoveAssetComponents(OutfitAsset asset)
        {
            if (!_assetComponents.TryGetValue(asset, out List<GenieComponent> components))
            {
                return;
            }

            foreach (GenieComponent component in components)
            {
                _genie.Components.Remove(component);
            }

            components.Clear();
            _assetComponents.Remove(asset);
        }

        private void AddAssetComponents(OutfitAsset asset)
        {
            if (asset.ComponentCreators is null || asset.ComponentCreators.Length == 0)
            {
                return;
            }

            // create a list that will contain the components added by this asset so we can remove them when the asset is unequipped
            var components = new List<GenieComponent>();
            _assetComponents[asset] = components;

            foreach (IGenieComponentCreator componentCreator in asset.ComponentCreators)
            {
                // try to create a component for this genie and save it if it was successfully created and added
                GenieComponent component = componentCreator?.CreateComponent();

                if (component is null)
                {
                    continue;
                }

                if (!_genie.Components.Add(component))
                {
                    continue;
                }

                components.Add(component);
            }
        }

        private async UniTask LockAssetAsync(OutfitAsset asset)
        {
            if (_assetLocks.TryGetValue(asset, out UniTaskCompletionSource assetLock))
            {
                await assetLock.Task;
            }

            _assetLocks[asset] = assetLock = new UniTaskCompletionSource();
        }

        private void UnlockAsset(OutfitAsset asset)
        {
            if (!_assetLocks.TryGetValue(asset, out UniTaskCompletionSource assetLock))
            {
                return;
            }

            _assetLocks.Remove(asset);
            assetLock.TrySetResult();
        }
    }
}
