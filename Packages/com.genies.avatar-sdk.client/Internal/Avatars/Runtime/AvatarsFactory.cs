using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.PerformanceMonitoring;
using Genies.Refs;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Avatars
{
    /// <summary>
    /// Static factory for loading our avatars.
    /// </summary>
    public static class AvatarsFactory
    {
        private static CustomInstrumentationManager _InstrumentationManager => CustomInstrumentationManager.Instance;
        private static string _RootTransaction => CustomInstrumentationOperations.LoadAvatarTransaction;
        private static string _BakedAvatarTransaction => CustomInstrumentationOperations.LoadBakedAvatarTransaction;

#region CONTROLLERS
        public static async UniTask<ISpeciesGenieController> CreateGenieAsync(string species, string subSpecies = null,
            string definition = null, Transform parent = null, string lod = AssetLod.Default, AvatarsContext context = null)
        {
            return species switch
            {
                GenieSpecies.Unified => await CreateUnifiedGenieAsync(definition, parent, lod, context),
                GenieSpecies.UnifiedGAP => await CreateUnifiedGAPGenieAsync(subSpecies, definition, parent, lod, context),
                _ => null,
            };
        }

        public static async UniTask<ISpeciesGenieController> CreateGenieAsync(string species, IEditableGenie editableGenie,
            string definition = null, string lod = AssetLod.Default, AvatarsContext context = null)
        {
            return species switch
            {
                GenieSpecies.Unified => await CreateUnifiedGenieAsync(editableGenie, definition, lod, context),
                GenieSpecies.UnifiedGAP => await CreateUnifiedGAPGenieAsync(editableGenie, definition, lod, context),
                _ => null,
            };
        }

        public static async UniTask<UnifiedGenieController> CreateUnifiedGenieAsync(string definition = null,
            Transform parent = null, string lod = AssetLod.Default, AvatarsContext context = null)
        {
            context ??= DefaultAvatarsContext.Instance;
            UmaGenie umaGenie = await UmaGenieFactory.CreateAsync(GenieSpecies.Unified, parent, lod, context);
            UnifiedGenieController controller = await CreateUnifiedGenieAsync(umaGenie, definition, lod, context);
            return controller;
        }

        public static async UniTask<UnifiedGAPGenieController> CreateUnifiedGAPGenieAsync(string subSpecies = null,
            string definition = null, Transform parent = null, string lod = AssetLod.Default, AvatarsContext context = null)
        {
            context ??= DefaultAvatarsContext.Instance;
            EditableGenie umaGenie = await EditableGenieFactory.CreateAsync(GenieSpecies.UnifiedGAP, subSpecies, parent, lod, context);
            UnifiedGAPGenieController controller = await CreateUnifiedGAPGenieAsync(umaGenie, definition, lod, context);
            return controller;
        }

        public static async UniTask<UnifiedGenieController> CreateEditableGenieAsync(string definition = null,
            Transform parent = null, string lod = AssetLod.Default, AvatarsContext context = null)
        {
            context ??= DefaultAvatarsContext.Instance;
            EditableGenie editableGenie = await EditableGenieFactory.CreateAsync(GenieSpecies.Unified, parent : parent, context : context);
            UnifiedGenieController controller = await CreateUnifiedGenieAsync(editableGenie, definition, lod, context);
            return controller;
        }

        public static async UniTask<UnifiedGenieController> CreateUnifiedGenieAsync(IEditableGenie editableGenie,
            string definition = null, string lod = AssetLod.Default, AvatarsContext context = null)
        {
            if (editableGenie is null)
            {
                return null;
            }

            context ??= DefaultAvatarsContext.Instance;
            var controller = new UnifiedGenieController(editableGenie, context);

            await controller.InitializeAsync();

            if (!string.IsNullOrEmpty(definition))
            {
                var wasTracked = false;
                if (!_InstrumentationManager.RunningTransactions.Contains(_RootTransaction))
                {
                    _InstrumentationManager.StartTransaction(_RootTransaction, "AvatarController.SetDefinition");
                    wasTracked = true;
                }

                await controller.SetDefinitionAsync(definition);

                if (wasTracked)
                {
                    _InstrumentationManager.FinishTransaction(_RootTransaction);
                }
            }

            AddGameObjectReferences(controller);

            return controller;
        }

        public static async UniTask<UnifiedGAPGenieController> CreateUnifiedGAPGenieAsync(IEditableGenie editableGenie,
            string definition = null, string lod = AssetLod.Default, AvatarsContext context = null)
        {
            if (editableGenie is null)
            {
                return null;
            }

            context ??= DefaultAvatarsContext.Instance;
            var controller = new UnifiedGAPGenieController(editableGenie, context);
            await controller.InitializeAsync();

            if (!string.IsNullOrEmpty(definition))
            {
                var wasTracked = false;
                if (!_InstrumentationManager.RunningTransactions.Contains(_RootTransaction))
                {
                    _InstrumentationManager.StartTransaction(_RootTransaction, "AvatarController.SetDefinition");
                    wasTracked = true;
                }

                await controller.SetDefinitionAsync(definition);

                if (wasTracked)
                {
                    _InstrumentationManager.FinishTransaction(_RootTransaction);
                }
            }

            AddGameObjectReferences(controller);

            return controller;
        }
#endregion

#region BAKING
        public static async UniTask<IGenie> CreateBakedGenieAsync(string definition, Transform parent = null,
            string lod = AssetLod.Default, bool urpBake = false, AvatarsContext context = null)
        {
            // since we require a definition we can infer the species from it
            if (!TryGetSpeciesFromDefinition(definition, out string species))
            {
                Debug.LogError($"[{nameof(AvatarsFactory)}] couldn't infer the species from the given definition");
                return null;
            }

            string subSpecies = (species == GenieSpecies.UnifiedGAP) ? GetSubSpeciesFromDefinition(definition) : null;

            // create a genie controller (this is the only way to build the genie right now)
            ISpeciesGenieController controller = await CreateGenieAsync(species, subSpecies : subSpecies, definition, parent: null, lod, context);

            // bake the controller's genie
            IGenie bakedGenie = await controller.Genie.BakeAsync(parent, urpBake);

            // dispose the controller as the baked genie to dispose all the resources (baked genie is independent)
            controller.Dispose();

            return bakedGenie;
        }

        public static async UniTask<IGenieSnapshot> CreateGenieSnapshotAsync(string definition, RuntimeAnimatorController pose,
            Transform parent = null, bool urpBake = false, string lod = AssetLod.Default, AvatarsContext context = null)
        {
            if (!TryGetSpeciesFromDefinition(definition, out string species))
            {
                Debug.LogError($"[{nameof(AvatarsFactory)}] couldn't infer the species from the given definition");
                return null;
            }

            string subSpecies = (species == GenieSpecies.UnifiedGAP) ? GetSubSpeciesFromDefinition(definition) : null;

            ISpeciesGenieController controller = await CreateGenieAsync(species, subSpecies : subSpecies, definition, parent: null, lod, context);

            /**
             * This is not ideal but we need to wait for one frame before changing the animation so some bonus components
             * are initialized properly and doesn't throw errors. After changing the animator to the given pose we need
             * to wait for one more frame so it takes effect.
             */
            await UniTask.Yield();
            controller.Genie.Animator.runtimeAnimatorController = pose;
            await UniTask.Yield();

            IGenieSnapshot genieSnapshot = await controller.Genie.TakeSnapshotAsync(parent, urpBake);
            controller.Dispose();

            return genieSnapshot;
        }
#endregion

#region IMPORING
        public static UniTask<IGenie> LoadFromGltfAsync(string url, Transform parent = null, GenieGltfImporter.Settings settings = null)
        {
            return GenieGltfImporter.ImportAsync(url, parent, settings);
        }

        public static UniTask<Ref<IGeniePrefab>> LoadFromGltfAsPrefabAsync(string url, GenieGltfImporter.Settings settings = null)
        {
            return GenieGltfImporter.ImportAsPrefabAsync(url, settings);
        }

        public static UniTask<IGenie> LoadFromGltfAsync(GenieGltfImporter.LodGroupSource source, Transform parent = null, GenieGltfImporter.Settings settings = null)
        {
            return GenieGltfImporter.ImportAsync(source, parent, settings);
        }

        public static UniTask<Ref<IGeniePrefab>> LoadFromGltfAsPrefabAsync(GenieGltfImporter.LodGroupSource source, GenieGltfImporter.Settings settings = null)
        {
            return GenieGltfImporter.ImportAsPrefabAsync(source, settings);
        }

        public static async UniTask<IGenie> LoadFromLodManifestAsync(string lodManifestUrl, Transform parent = null, GenieGltfImporter.Settings settings = null)
        {
            var wasTracked = false;

            if (!_InstrumentationManager.RunningTransactions.Contains(_BakedAvatarTransaction))
            {
                _InstrumentationManager.StartTransaction(_BakedAvatarTransaction, "AvatarsFactory.LoadFromLodManifestAsync");
                wasTracked = true;
            }

            GenieGltfImporter.LodGroupSource lodGroupSource = await LodManifestUtilities.GenerateLodGroupSourceAsync(lodManifestUrl);
            if (lodGroupSource.lods is null || lodGroupSource.lods.Count == 0)
            {
                return null;
            }

            IGenie genie = await LoadFromGltfAsync(lodGroupSource, parent, settings);

            if (wasTracked)
            {
                _InstrumentationManager.FinishTransaction(_BakedAvatarTransaction);
            }

            return genie;
        }

        public static async UniTask<IGenie> LoadFromLodManifestAsync(string lodManifestUrl, int lodIndex, Transform parent = null, GenieGltfImporter.Settings settings = null)
        {
            var wasTracked = false;

            if (!_InstrumentationManager.RunningTransactions.Contains(_BakedAvatarTransaction))
            {
                _InstrumentationManager.StartTransaction(_BakedAvatarTransaction, "AvatarsFactory.LoadFromLodManifestAsync");
                wasTracked = true;
            }

            List<AvatarLodInfo> lodInfos = await LodManifestUtilities.LoadLodInfoFromManifestAsync(lodManifestUrl);

            // try to find the AvatarLodInfo with the specified LOD index
            bool foundTargetInfo = false;
            AvatarLodInfo targetInfo = default;
            foreach (AvatarLodInfo info in lodInfos)
            {
                if (info.Index != lodIndex)
                {
                    continue;
                }

                foundTargetInfo = true;
                targetInfo = info;
                break;
            }

            if (wasTracked)
            {
                _InstrumentationManager.FinishTransaction(_BakedAvatarTransaction);
            }

            if (foundTargetInfo)
            {
                return await LoadFromGltfAsync(targetInfo.Url, parent, settings);
            }
            else
            {
                return null;
            }
        }

        public static async UniTask<IGenie> LoadFromLodManifestAsync(string lodManifestUrl, string lodName, Transform parent = null, GenieGltfImporter.Settings settings = null)
        {
            var wasTracked = false;

            if (!_InstrumentationManager.RunningTransactions.Contains(_BakedAvatarTransaction))
            {
                _InstrumentationManager.StartTransaction(_BakedAvatarTransaction, "AvatarsFactory.LoadFromLodManifestAsync");
                wasTracked = true;
            }

            List<AvatarLodInfo> lodInfos = await LodManifestUtilities.LoadLodInfoFromManifestAsync(lodManifestUrl);

            // try to find the AvatarLodInfo with the specified LOD name
            bool foundTargetInfo = false;
            AvatarLodInfo targetInfo = default;
            foreach (AvatarLodInfo info in lodInfos)
            {
                if (info.Name != lodName)
                {
                    continue;
                }

                foundTargetInfo = true;
                targetInfo = info;
                break;
            }

            if (wasTracked)
            {
                _InstrumentationManager.FinishTransaction(_BakedAvatarTransaction);
            }

            if (foundTargetInfo)
            {
                return await LoadFromGltfAsync(targetInfo.Url, parent, settings);
            }
            else
            {
                return null;
            }
        }

        public static async UniTask<IGenie> LoadFromLodManifestAsync(string lodManifestUrl, IEnumerable<int> includeLodIndices, Transform parent = null, GenieGltfImporter.Settings settings = null)
        {
            List<AvatarLodInfo> lodInfos = await LodManifestUtilities.LoadLodInfoFromManifestAsync(lodManifestUrl);
            if (includeLodIndices is not List<int> indices)
            {
                indices = new List<int>(includeLodIndices);
            }

            // remove lod infos with LOD indices not included in the includes list
            for (int i = 0; i < lodInfos.Count; ++i)
            {
                if (!indices.Contains(lodInfos[i].Index))
                {
                    lodInfos.RemoveAt(i--);
                }
            }

            return lodInfos.Count switch
            {
                0 => null, // no lod infos included
                1 => await LoadFromGltfAsync(lodInfos[0].Url, parent, settings), // single lod
                _ => await LoadFromGltfAsync(LodManifestUtilities.GenerateLodGroupSource(lodInfos), parent, settings),
            };
        }

        public static async UniTask<IGenie> LoadFromLodManifestAsync(string lodManifestUrl, IEnumerable<string> includeLodNames, Transform parent = null, GenieGltfImporter.Settings settings = null)
        {
            List<AvatarLodInfo> lodInfos = await LodManifestUtilities.LoadLodInfoFromManifestAsync(lodManifestUrl);
            if (includeLodNames is not List<string> names)
            {
                names = new List<string>(includeLodNames);
            }

            // remove lod infos with LOD names not included in the includes list
            for (int i = 0; i < lodInfos.Count; ++i)
            {
                if (!names.Contains(lodInfos[i].Name))
                {
                    lodInfos.RemoveAt(i--);
                }
            }

            return lodInfos.Count switch
            {
                0 => null,
                1 => await LoadFromGltfAsync(lodInfos[0].Url, parent, settings),
                _ => await LoadFromGltfAsync(LodManifestUtilities.GenerateLodGroupSource(lodInfos), parent, settings),
            };
        }

        public static async UniTask<Ref<IGeniePrefab>> LoadFromLodManifestAsPrefabAsync(string lodManifestUrl, GenieGltfImporter.Settings settings = null)
        {
            GenieGltfImporter.LodGroupSource lodGroupSource = await LodManifestUtilities.GenerateLodGroupSourceAsync(lodManifestUrl);
            if (lodGroupSource.lods is null || lodGroupSource.lods.Count == 0)
            {
                return default;
            }

            Ref<IGeniePrefab> geniePrefabRef = await LoadFromGltfAsPrefabAsync(lodGroupSource, settings);
            return geniePrefabRef;
        }

        public static async UniTask<Ref<IGeniePrefab>> LoadFromLodManifestAsPrefabAsync(string lodManifestUrl, int lodIndex, GenieGltfImporter.Settings settings = null)
        {
            List<AvatarLodInfo> lodInfos = await LodManifestUtilities.LoadLodInfoFromManifestAsync(lodManifestUrl);

            // try to find the AvatarLodInfo with the specified LOD index
            bool foundTargetInfo = false;
            AvatarLodInfo targetInfo = default;
            foreach (AvatarLodInfo info in lodInfos)
            {
                if (info.Index != lodIndex)
                {
                    continue;
                }

                foundTargetInfo = true;
                targetInfo = info;
                break;
            }

            if (foundTargetInfo)
            {
                return await LoadFromGltfAsPrefabAsync(targetInfo.Url, settings);
            }
            else
            {
                return default;
            }
        }

        public static async UniTask<Ref<IGeniePrefab>> LoadFromLodManifestAsPrefabAsync(string lodManifestUrl, string lodName, GenieGltfImporter.Settings settings = null)
        {
            List<AvatarLodInfo> lodInfos = await LodManifestUtilities.LoadLodInfoFromManifestAsync(lodManifestUrl);

            // try to find the AvatarLodInfo with the specified LOD name
            bool foundTargetInfo = false;
            AvatarLodInfo targetInfo = default;
            foreach (AvatarLodInfo info in lodInfos)
            {
                if (info.Name != lodName)
                {
                    continue;
                }

                foundTargetInfo = true;
                targetInfo = info;
                break;
            }

            if (foundTargetInfo)
            {
                return await LoadFromGltfAsPrefabAsync(targetInfo.Url, settings);
            }
            else
            {
                return default;
            }
        }

        public static async UniTask<Ref<IGeniePrefab>> LoadFromLodManifestAsPrefabAsync(string lodManifestUrl, IEnumerable<int> includeLodIndices, GenieGltfImporter.Settings settings = null)
        {
            List<AvatarLodInfo> lodInfos = await LodManifestUtilities.LoadLodInfoFromManifestAsync(lodManifestUrl);
            if (includeLodIndices is not List<int> indices)
            {
                indices = new List<int>(includeLodIndices);
            }

            // remove lod infos with LOD indices not included in the includes list
            for (int i = 0; i < lodInfos.Count; ++i)
            {
                if (!indices.Contains(lodInfos[i].Index))
                {
                    lodInfos.RemoveAt(i--);
                }
            }

            return lodInfos.Count switch
            {
                0 => default, // no lod infos included
                1 => await LoadFromGltfAsPrefabAsync(lodInfos[0].Url, settings), // single lod
                _ => await LoadFromGltfAsPrefabAsync(LodManifestUtilities.GenerateLodGroupSource(lodInfos), settings),
            };
        }

        public static async UniTask<Ref<IGeniePrefab>> LoadFromLodManifestAsPrefabAsync(string lodManifestUrl, IEnumerable<string> includeLodNames, GenieGltfImporter.Settings settings = null)
        {
            List<AvatarLodInfo> lodInfos = await LodManifestUtilities.LoadLodInfoFromManifestAsync(lodManifestUrl);
            if (includeLodNames is not List<string> names)
            {
                names = new List<string>(includeLodNames);
            }

            // remove lod infos with LOD names not included in the includes list
            for (int i = 0; i < lodInfos.Count; ++i)
            {
                if (!names.Contains(lodInfos[i].Name))
                {
                    lodInfos.RemoveAt(i--);
                }
            }

            return lodInfos.Count switch
            {
                0 => default,
                1 => await LoadFromGltfAsPrefabAsync(lodInfos[0].Url, settings),
                _ => await LoadFromGltfAsPrefabAsync(LodManifestUtilities.GenerateLodGroupSource(lodInfos), settings),
            };
        }
#endregion

        public static bool TryGetSpeciesFromDefinition(string definition, out string species)
        {
            if (string.IsNullOrEmpty(definition))
            {
                species = null;
                return false;
            }

            try
            {
                species = JObject.Parse(definition)["Species"]?.Value<string>();
                return !string.IsNullOrEmpty(species);
            }
            catch (Exception)
            {
                species = null;
                return false;
            }
        }

        public static string GetSubSpeciesFromDefinition(string definition)
        {
            if (string.IsNullOrEmpty(definition))
            {
                return null;
            }

            return JObject.Parse(definition)["SubSpecies"]?.Value<string>();
        }


        private static void AddGameObjectReferences(ISpeciesGenieController controller)
        {
            GenieReference.Create(controller.Genie, controller.Genie.Root, disposeOnDestroy: false);
            if (controller.Genie is IEditableGenie editableGenie)
            {
                EditableGenieReference.Create(editableGenie, controller.Genie.Root, disposeOnDestroy: false);
            }

            SpeciesGenieControllerReference.Create(controller, controller.Genie.Root, disposeOnDestroy: true);
        }
    }
}
