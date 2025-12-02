using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Utilities;

namespace Genies.Avatars
{
    internal sealed class UmaGenieOutfitManager : IDisposable
    {
        public IReadOnlyCollection<OutfitAsset> OutfitAssets => _outfitAssets;

        // dependencies
        private readonly UmaGenie _genie;
        
        // state
        private readonly TrackedHashSet<OutfitAsset> _outfitAssets;
        private readonly HashSet<IOutfitAssetProcessor> _outfitAssetProcessors;
        private readonly Dictionary<OutfitAsset, UniTaskCompletionSource> _assetLocks;
        private readonly Dictionary<OutfitAsset, List<GenieComponent>> _assetComponents;
        
        public UmaGenieOutfitManager(UmaGenie genie)
        {
            _genie = genie;
            
            _outfitAssets = new TrackedHashSet<OutfitAsset>();
            _outfitAssetProcessors = new HashSet<IOutfitAssetProcessor>();
            _assetLocks = new Dictionary<OutfitAsset, UniTaskCompletionSource>();
            _assetComponents = new Dictionary<OutfitAsset, List<GenieComponent>>();
            
            _outfitAssets.BeginTracking();
        }
        
        public async UniTask AddOutfitAssetAsync(OutfitAsset asset)
        {
            // lock the asset so any calls to Add/Remove it will await until its unlocked (if the asset is locked this will await for unlock)
            await LockAssetAsync(asset);

            if (_outfitAssets.Contains(asset))
            {
                UnlockAsset(asset);
                return;
            }
            
            // process the outfit asset
            await UniTask.WhenAll(_outfitAssetProcessors.Select(processor => processor.ProcessAddedAssetAsync(asset)));
            
            // set the UMA recipe to the avatar and mark it as dirty
            _genie.Avatar.SetSlot(asset.Recipe);
            _genie.SetUmaAvatarDirty();
            
            // add the asset and unlock it
            _outfitAssets.Add(asset);
            UnlockAsset(asset);
        }

        public async UniTask RemoveOutfitAssetAsync(OutfitAsset asset)
        {
            // lock the asset so any calls to Add/Remove it will await until its unlocked (if the asset is locked this will await for unlock)
            await LockAssetAsync(asset);

            if (!_outfitAssets.Contains(asset))
            {
                UnlockAsset(asset);
                return;
            }
            
            // process the outfit asset
            await UniTask.WhenAll(_outfitAssetProcessors.Select(processor => processor.ProcessRemovedAssetAsync(asset)));
            
            // clear the UMA slot from the avatar and mark it as dirty
            _genie.Avatar.ClearSlot(asset.Recipe.wardrobeSlot);
            _genie.SetUmaAvatarDirty();
            
            // remove the asset and unlock it
            _outfitAssets.Remove(asset);
            UnlockAsset(asset);
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
            _outfitAssets.Clear();
            _outfitAssetProcessors.Clear();
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