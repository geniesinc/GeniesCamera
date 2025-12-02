using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Avatars;
using GnWrappers;
using Newtonsoft.Json;
using UnityEngine;

using Object = UnityEngine.Object;

namespace Genies.Naf
{
    public sealed class NativeUnifiedGenieController : ISpeciesGenieController
    {
#if GENIES_SDK
        private const string _femaleShapeAssetId = "Static/BlendShapeContainer_body_female";
        private const string _maleShapeAssetId   = "Static/BlendShapeContainer_body_male";
#else
        private const string _femaleShapeAssetId = "AvatarBase/recmdZ4C4enmt630";
        private const string _maleShapeAssetId = "AvatarBase/recmdZ4c4ENEO817";

#endif
        public IGenie Genie => NativeBuilder.NativeGenie;

        /**
         * The native genie builder instance that this controller manages. This is a low-level class, and you should not
         * access it unless you know what you are doing.
         */
        public readonly NativeGenieBuilder NativeBuilder;

        /**
         * A service used to fetch the parameters dictionary for a given asset ID. Used when loading an avatar
         * definition since it won't contain any parameters.
         */
        public IAssetParamsService AssetParamsService;

        // Global semaphore to prevent race conditions in asset operations
        private readonly SemaphoreSlim _assetOperationSemaphore = new(1, 1);

        // Cancellation token to cancel previous operations when new ones start
        private CancellationTokenSource _currentOperationCancellation = new();

        public NativeUnifiedGenieController(NativeGenieBuilder nativeBuilder, IAssetParamsService assetParamsService)
        {
            NativeBuilder      = nativeBuilder;
            AssetParamsService = assetParamsService;

            NativeBuilder.EnsureAwake();
        }

        /// <summary>
        /// Executes an asset operation with "latest wins" behavior - cancels any previous operations
        /// and discards queued operations if a new one starts.
        /// </summary>
        private async UniTask ExecuteAssetOperationAsync(Func<CancellationToken, UniTask> operation)
        {
            // Cancel any previous operation
            _currentOperationCancellation.Cancel();
            _currentOperationCancellation.Dispose();
            _currentOperationCancellation = new CancellationTokenSource();

            var cancellationToken = _currentOperationCancellation.Token;

            // Try to acquire the semaphore, but bail out if cancelled while waiting
            try
            {
                await _assetOperationSemaphore.WaitAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled while waiting in queue - discard it
                return;
            }

            try
            {
                // Check if cancelled before starting actual work
                cancellationToken.ThrowIfCancellationRequested();

                NativeBuilder.CancelAsyncLoads();

                // Execute the actual operation
                await operation(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled during execution
            }
            finally
            {
                _assetOperationSemaphore.Release();
            }
        }

        public async UniTask EquipAssetAsync(string assetId, Dictionary<string, string> parameters = null)
        {
            await ExecuteAssetOperationAsync(async cancellationToken =>
            {
                await NativeBuilder.LoadAndAddCombinableAssetAsync(assetId, parameters);
                cancellationToken.ThrowIfCancellationRequested();
                await NativeBuilder.RebuildAsync();
            });
        }

        public async UniTask UnequipAssetAsync(string assetId)
        {
            await ExecuteAssetOperationAsync(async cancellationToken =>
            {
                NativeBuilder.RemoveEntity(assetId);
                cancellationToken.ThrowIfCancellationRequested();
                await NativeBuilder.RebuildAsync();
            });
        }

        /**
         * Equips the given assets. Use this method if you want to equip multiple assets at once, with maximum performance.
         */
        public async UniTask EquipAssetsAsync(IEnumerable<(string assetId, Dictionary<string, string> parameters)> assets)
        {
            await ExecuteAssetOperationAsync(async cancellationToken =>
            {
                var requests = new List<AssetLoadRequest>();
                foreach ((string assetId, Dictionary<string, string> parameters) in assets)
                {
                    requests.Add(new AssetLoadRequest(assetId, parameters));
                }

                await NativeBuilder.LoadAndAddCombinableAssetsAsync(requests);
                cancellationToken.ThrowIfCancellationRequested();
                await NativeBuilder.RebuildAsync();
            });
        }

        public async UniTask UnequipAssetsAsync(IEnumerable<string> assetIds)
        {
            await ExecuteAssetOperationAsync(async cancellationToken =>
            {
                foreach (string assetId in assetIds)
                {
                    NativeBuilder.RemoveEntity(assetId);
                }

                cancellationToken.ThrowIfCancellationRequested();
                await NativeBuilder.RebuildAsync();
            });
        }

        /**
         * Sets the equipped assets to the given ones (this clears the current assets first, and then equips). Use this
         * method if you want to replace current assets with maximum performance (only one avatar rebuild will take place).
         */
        public async UniTask SetEquippedAssetsAsync(IEnumerable<(string assetId, Dictionary<string, string> parameters)> assets)
        {
            await ExecuteAssetOperationAsync(async cancellationToken =>
            {
                // we get current entities to keep them loaded until we have loaded the new ones. In that case, any common entities will not be reloaded unnecessarily
                using VectorEntity entities = NativeBuilder.AssetBuilder.Entities();

                NativeBuilder.ClearEntities();

                var requests = new List<AssetLoadRequest>();
                foreach ((string assetId, Dictionary<string, string> parameters) in assets)
                {
                    requests.Add(new AssetLoadRequest(assetId, parameters));
                }

                await NativeBuilder.LoadAndAddCombinableAssetsAsync(requests);
                cancellationToken.ThrowIfCancellationRequested();
                await NativeBuilder.RebuildAsync();
            });
        }

        public bool IsAssetEquipped(string assetId)
        {
            if (string.IsNullOrEmpty(assetId))
            {
                return false;
            }

            using VectorEntity entities = NativeBuilder.AssetBuilder.Entities();
            foreach (Entity entity in entities)
            {
                // Temporary work around to check if Entity Name contains AssetID. We
                // will do this until Inventory package switches to using UniversalID
                if (entity.Name().Contains(assetId))
                {
                    return true;
                }
            }

            return false;
        }

        public UniTask SetColorAsync(string colorId, Color color)
        {
            NativeBuilder.SetColor(colorId, color);
            NativeBuilder.RebuildColors();
            return UniTask.CompletedTask;
        }

        public UniTask SetColorsAsync(IEnumerable<GenieColorEntry> colors)
        {
            foreach (GenieColorEntry entry in colors)
            {
                if (entry.Value.HasValue)
                {
                    NativeBuilder.SetColor(entry.ColorId, entry.Value.Value);
                }
                else
                {
                    NativeBuilder.UnsetColor(entry.ColorId);
                }
            }

            NativeBuilder.RebuildColors();
            return UniTask.CompletedTask;
        }

        public Color? GetColor(string colorId)
        {
            return NativeBuilder.GetColor(colorId);
        }

        public UniTask UnsetColorAsync(string colorId)
        {
            NativeBuilder.UnsetColor(colorId);
            return NativeBuilder.RebuildAsync(); // unsetting colors requires a full rebuild (which will be quick anyways since we just changed a color)
        }

        public UniTask UnsetAllColorsAsync()
        {
            NativeBuilder.UnsetAllColors();
            return NativeBuilder.RebuildAsync();
        }

        public bool IsColorAvailable(string colorId)
        {
            return NativeBuilder.ColorAttributeExists(colorId);
        }

        public void SetBodyAttribute(string attributeId, float weight)
        {
            NativeBuilder.SetShapeAttributeWeight(attributeId, weight);
            NativeBuilder.RebuildSkeletonOffset();
        }

        public float GetBodyAttribute(string attributeId)
        {
            return NativeBuilder.GetShapeAttributeWeight(attributeId);
        }

        public void SetBodyPreset(BodyAttributesPreset preset)
        {
            NativeBuilder.SetShapeAttributes(preset);
            NativeBuilder.RebuildSkeletonOffset();
        }

#region Deprecated methods for retrocompatibility with the current MegaEditor
        public async UniTask SetBodyPresetAsync(GSkelModifierPreset preset)
        {
            string bodyVariationAssetId = null;
            if (preset.StartingBodyVariation == UnifiedBodyVariation.Female)
            {
                bodyVariationAssetId = _femaleShapeAssetId;
            }
            else if (preset.StartingBodyVariation == UnifiedBodyVariation.Male)
            {
                bodyVariationAssetId = _maleShapeAssetId;
            }

            if (!string.IsNullOrEmpty(bodyVariationAssetId))
            {
                Dictionary<string, string> parameters = await AssetParamsService.FetchParamsAsync(bodyVariationAssetId);
                await NativeBuilder.LoadAndAddCombinableAssetAsync(bodyVariationAssetId, parameters);
            }

            foreach (GSkelModValue value in preset.GSkelModValues)
            {
                NativeBuilder.SetShapeAttributeWeight(value.Name, value.Value);
            }

            if (!string.IsNullOrEmpty(bodyVariationAssetId))
            {
                await NativeBuilder.RebuildAsync();
            }
            else
            {
                NativeBuilder.RebuildSkeletonOffset();
            }
        }

        public GSkelModifierPreset GetBodyPreset()
        {
            GSkelModifierPreset preset = ScriptableObject.CreateInstance<GSkelModifierPreset>();
            preset.StartingBodyVariation = GetBodyVariation();
            List<string> attributes = NativeBuilder.GetExistingShapeAttributes();
            preset.GSkelModValues ??= new List<GSkelModValue>(attributes.Count);

            foreach (string attribute in attributes)
            {
                preset.GSkelModValues.Add(new GSkelModValue
                {
                    Name = attribute,
                    Value = NativeBuilder.GetShapeAttributeWeight(attribute),
                });
            }

            return preset;
        }

        public string GetBodyVariation()
        {
            using VectorEntity entities = NativeBuilder.AssetBuilder.Entities();
            foreach (Entity entity in entities)
            {
                string assetId = entity.Name();

                if (assetId == _femaleShapeAssetId)
                {
                    return UnifiedBodyVariation.Female;
                }

                if (assetId == _maleShapeAssetId)
                {
                    return UnifiedBodyVariation.Male;
                }
            }

            return null;
        }
#endregion

        public void ResetAllBodyAttributes()
        {
            NativeBuilder.ResetShapeAttributeWeights();
            NativeBuilder.RebuildSkeletonOffset();
        }

        public bool IsBodyAttributeAvailable(string attributeId)
        {
            return NativeBuilder.ShapeAttributeExists(attributeId);
        }

        public async UniTask EquipTattooAsync(MegaSkinTattooSlot slot, string assetId, Dictionary<string, string> parameters = null)
        {
            NativeBuilder.CancelTextureAsyncLoads();
            await NativeBuilder.LoadAndSetTattooAsync(slot, assetId, parameters);
            NativeBuilder.RebuildTattoos();
        }

        public UniTask UnequipTattooAsync(MegaSkinTattooSlot slot)
        {
            NativeBuilder.UnsetTattoo(slot);
            NativeBuilder.RebuildTattoos();
            return UniTask.CompletedTask;
        }

        public UniTask UnequipAllTattoosAsync()
        {
            NativeBuilder.UnsetAllTattoos();
            NativeBuilder.RebuildTattoos();
            return UniTask.CompletedTask;
        }

        public bool IsTattooEquipped(MegaSkinTattooSlot slot, string assetId)
        {
            return NativeBuilder.IsTattooEquipped(slot, assetId);
        }

        public string GetEquippedTattoo(MegaSkinTattooSlot slot)
        {
            using GnWrappers.Texture texture = NativeBuilder.GetTattoo(slot);
            if (texture is null || texture.IsNull())
            {
                return null;
            }

            return texture.Name();
        }

        public AvatarDefinition GetDefinitionType()
        {
            var definition = new AvatarDefinition();

            // gather all equipped assets
            AddEquippedAssetIds(definition.equippedAssetIds);

            // gather all colors
            List<string> colorIds = NativeBuilder.GetExistingColorAttributes();
            foreach (string colorId in colorIds)
            {
                Color? color = NativeBuilder.GetColor(colorId);
                if (color.HasValue)
                {
                    definition.colors.Add(colorId, color.Value);
                }
            }

            // gather all body attributes
            List<string> attributeIds = NativeBuilder.GetExistingShapeAttributes();
            foreach (string attributeId in attributeIds)
            {
                float weight = NativeBuilder.GetShapeAttributeWeight(attributeId);
                if (weight != 0.0f) // only include attributes that have a non-zero weight, which is the default
                {
                    definition.bodyAttributes.Add(attributeId, weight);
                }
            }

            // gather all equipped tattoos
            var tattooSlots = Enum.GetValues(typeof(MegaSkinTattooSlot)) as MegaSkinTattooSlot[];
            foreach (MegaSkinTattooSlot slot in tattooSlots)
            {
                using GnWrappers.Texture texture = NativeBuilder.TattooEditor.GetTattoo(slot);
                if (texture is null || texture.IsNull())
                {
                    continue;
                }

                string assetId = texture.Name();
                if (string.IsNullOrEmpty(assetId))
                {
                    Debug.LogError($"[{nameof(NativeUnifiedGenieController)}] found an equipped tattoo ({slot.ToString()}) with a null or empty ID. It won't be included in the avatar definition...");
                    continue;
                }

                definition.equippedTattooIds.Add(slot, assetId);
            }

            return definition;
        }

        public string GetDefinition()
        {
            var definition = GetDefinitionType();

            try
            {
                string json = JsonConvert.SerializeObject(definition);
                return json;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(NativeUnifiedGenieController)}] failed to serialize avatar definition:\n{exception}");
                return null;
            }
        }

        public async UniTask SetDefinitionAsync(AvatarDefinition definition)
        {
            if (definition == null)
            {
                return;
            }

            await ExecuteAssetOperationAsync(async cancellationToken =>
            {
                // get currently equipped entities and keep them alive until we finish, so we avoid unnecessary reloads
                using VectorEntity entities = NativeBuilder.AssetBuilder.Entities();

                NativeBuilder.UnsetAllColors();
                NativeBuilder.ResetShapeAttributeWeights();
                NativeBuilder.UnsetAllTattoos();
                NativeBuilder.ClearEntities();

                // set all the colors
                if (definition.colors is { Count: > 0 })
                {
                    foreach ((string colorId, Color color) in definition.colors)
                    {
                        NativeBuilder.SetColor(colorId, color);
                    }
                }

                // set all the body attributes
                if (definition.bodyAttributes is { Count: > 0 })
                {
                    foreach ((string attributeId, float weight) in definition.bodyAttributes)
                    {
                        NativeBuilder.SetShapeAttributeWeight(attributeId, weight);
                    }
                }

                // load all assets and tattoos in parallel
                var loadAssetsTask = new UniTask<Entity[]>();
                var loadTattoosTask = new UniTask<(MegaSkinTattooSlot slot, GnWrappers.Texture tattoo)[]>();
                if (definition.equippedAssetIds is { Count: > 0 })
                {
                    loadAssetsTask  = UniTask.WhenAll(definition.equippedAssetIds.Select(LoadCombinableAssetAsync));
                }

                if (definition.equippedTattooIds is { Count: > 0 })
                {
                    loadTattoosTask = UniTask.WhenAll(definition.equippedTattooIds.Select(LoadTattooAsync));
                }

                var (assets, tattoos) = await UniTask.WhenAll(loadAssetsTask, loadTattoosTask);

                cancellationToken.ThrowIfCancellationRequested();

                // add loaded assets and tattoos
                if (assets is { Length: > 0 })
                {
                    NativeBuilder.AddEntities(assets, disposeAfterAdding: true);
                }

                if (tattoos is { Length: > 0 })
                {
                    foreach ((MegaSkinTattooSlot slot, GnWrappers.Texture tattoo) in tattoos)
                    {
                        NativeBuilder.TattooEditor.SetTattoo(slot, tattoo);
                    }
                }

                // rebuild the native genie
                await NativeBuilder.RebuildAsync();

                return;

                // local helper methods
                async UniTask<Entity> LoadCombinableAssetAsync(string assetId)
                {
                    Dictionary<string, string> parameters = await AssetParamsService.FetchParamsAsync(assetId);
                    return await NativeBuilder.LoadCombinableAssetAsync(assetId, parameters);
                }

                async UniTask<(MegaSkinTattooSlot slot, GnWrappers.Texture tattoo)> LoadTattooAsync(KeyValuePair<MegaSkinTattooSlot, string> pair)
                {
                    Dictionary<string, string> parameters = await AssetParamsService.FetchParamsAsync(pair.Value);
                    GnWrappers.Texture tattoo = await NativeBuilder.LoadTextureAsync(pair.Value, parameters);
                    return (pair.Key, tattoo);
                }
            });
        }

        public async UniTask SetDefinitionAsync(string definition)
        {
            if (string.IsNullOrWhiteSpace(definition))
            {
                return;
            }

            AvatarDefinition avatar;
            try
            {
                avatar = JsonConvert.DeserializeObject<AvatarDefinition>(definition);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(NativeUnifiedGenieController)}] failed to deserialize avatar definition:\n{exception}");
                return;
            }

            await SetDefinitionAsync(avatar);
        }

        public void AddEquippedAssetIds(ICollection<string> results)
        {
            using VectorEntity entities = NativeBuilder.AssetBuilder.Entities();
            foreach (Entity entity in entities)
            {
                string assetId = entity.Name();
                if (string.IsNullOrEmpty(assetId))
                {
                    Debug.LogError($"[{nameof(NativeUnifiedGenieController)}] found an equipped asset with a null or empty asset ID");
                    continue;
                }

                results.Add(assetId);
            }
        }

        public List<string> GetEquippedAssetIds()
        {
            var results = new List<string>();
            AddEquippedAssetIds(results);
            return results;
        }

        public void Dispose()
        {
            // dispose NativeBuilder first
            NativeGenie genie = null;
            if (NativeBuilder)
            {
                genie = NativeBuilder.NativeGenie;
                NativeBuilder.Dispose();
            }

            // then the NativeGenie
            GameObject root = null;
            if (genie)
            {
                root = genie.Root;
                genie.Dispose();
            }

            if (root)
            {
                Object.Destroy(root);
            }
        }
    }
}
