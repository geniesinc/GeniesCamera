using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Internal implementation of <see cref="IGeniePrefab"/> used by the <see cref="GenieGltfImporter"/> to import
    /// multiple genies with a LODGroup.
    /// </summary>
    internal sealed class GenieGltfLodGroupPrefab : IGeniePrefab, IDisposable
    {
        // Uncomment the line below to load LODs sequentially instead of in parallel
        // #define LOAD_LODS_ONE_BY_ONE

        private static readonly Comparison<GenieGltfImporter.LodSource> LodComparison =
            (x, y) => y.screenRelativeTransitionHeight.CompareTo(x.screenRelativeTransitionHeight);

        private readonly GenieGltfImporter.LodGroupSource _source;
        private readonly GenieGltfImporter.Settings _settings;
        private readonly Ref<GenieGltfLodPrefab>[] _lodRefs;
        private readonly HashSet<Genie> _createdGenies;
        private UniTaskCompletionSource _loadOperation;
        private bool _isDisposed;

        public GenieGltfLodGroupPrefab(GenieGltfImporter.LodGroupSource source, GenieGltfImporter.Settings settings = null)
        {
            _source = source;
            _settings = settings;
            _source.lods.Sort(LodComparison);
            _lodRefs = new Ref<GenieGltfLodPrefab>[_source.lods.Count];
            _createdGenies = new HashSet<Genie>();
            _isDisposed = false;
        }

        public UniTask LoadAsync()
        {
            if (_loadOperation is not null)
            {
                return _loadOperation.Task;
            }

            _loadOperation = new UniTaskCompletionSource();

#if LOAD_LODS_ONE_BY_ONE
            LoadAllLodsOneByOne().Forget();
#else
            LoadAllLods();
#endif

            return _loadOperation.Task;
        }

#region IGenie implementation
        public IGenie Instantiate()
            => InstantiateGenie(prefab => prefab.InstantiateGenie());
        public IGenie Instantiate(Transform parent)
            => InstantiateGenie(prefab => prefab.InstantiateGenie(parent));
        public IGenie Instantiate(Transform parent, bool worldPositionStays)
            => InstantiateGenie(prefab => prefab.InstantiateGenie(parent, worldPositionStays));
        public IGenie Instantiate(Vector3 position, Quaternion rotation)
            => InstantiateGenie(prefab => prefab.InstantiateGenie(position, rotation));
        public IGenie Instantiate(Vector3 position, Quaternion rotation, Transform parent)
            => InstantiateGenie(prefab => prefab.InstantiateGenie(position, rotation, parent));
#endregion

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _isDisposed = true;

            // dispose all instances created from this prefab
            var geniesToDispose = new List<Genie>(_createdGenies);
            foreach (Genie genie in geniesToDispose)
            {
                genie.Dispose();
            }

            // dispose all LOD prefab refs
            foreach (Ref<GenieGltfLodPrefab> lodRef in _lodRefs)
            {
                lodRef.Dispose();
            }

            _createdGenies.Clear();
        }

        private Genie InstantiateGenie(Func<GenieGltfLodPrefab, Genie> instantiateFromPrefab)
        {
            Genie genie = null;
            var lodConfigs = new List<Genie.LodConfig>(_lodRefs.Length);

            for (int i = 0; i < _lodRefs.Length; ++i)
            {
                Ref<GenieGltfLodPrefab> lodRef = _lodRefs[i];
                if (!lodRef.IsAlive)
                {
                    continue;
                }

                GenieGltfImporter.LodSource lodSource = _source.lods[i];
                Genie.LodConfig lodConfig;

                if (genie is null)
                {
                    // if this is the first LOD we find then create the genie from it
                    genie = instantiateFromPrefab(lodRef.Item);
                    genie.FadeMode = _source.fadeMode;
                    genie.AnimateCrossFading = _source.animateCrossFading;

                    // get the LOD config from the genie and update the values
                    lodConfig = genie.Lods[0];
                    lodConfig.screenRelativeTransitionHeight = lodSource.screenRelativeTransitionHeight;
                    lodConfig.fadeTransitionWidth = lodSource.fadeTransitionWidth;
                }
                else
                {
                    // the genie is already instantiated so create the lod config manually from the prefab
                    lodConfig = new Genie.LodConfig
                    {
                        root                           = lodRef.Item.InstantiateLodRoot(genie.Root.transform),
                        screenRelativeTransitionHeight = lodSource.screenRelativeTransitionHeight,
                        fadeTransitionWidth            = lodSource.fadeTransitionWidth,
                    };
                }

                lodConfigs.Add(lodConfig);
            }

            if (!genie)
            {
                return null;
            }

            // add lods and register the created genie instance
            genie.AddLods(lodConfigs);
            genie.Disposed += () => _createdGenies.Remove(genie);
            _createdGenies.Add(genie);

            return genie;
        }

        private async UniTaskVoid LoadAllLodsOneByOne()
        {
            // Load LODs sequentially (one by one)
            for (int i = _source.lods.Count - 1; i >= 0; --i)
            {
                await LoadLodAsync(i);
            }
        }

        private void LoadAllLods()
        {
            // Load all LODs in parallel for better performance
            for (int i = 0; i < _source.lods.Count; ++i)
            {
                LoadLodAsync(i).Forget();
            }
        }

        private async UniTask LoadLodAsync(int lodIndex)
        {
            GenieGltfImporter.LodSource lodSource = _source.lods[lodIndex];
            Ref<GenieGltfLodPrefab> lodRef = await GenieGltfImporter.ImportLodPrefabAsync(lodSource.url, _settings);
            if (!lodRef.IsAlive)
            {
                return;
            }

            // if the prefab was disposed while loading this LOD then dispose it and return
            if (_isDisposed)
            {
                lodRef.Dispose();
                return;
            }

            _lodRefs[lodIndex] = lodRef;

            // as soon as the first lod is loaded then we signal that the prefab was loaded
            if (_loadOperation.GetStatus(0) is UniTaskStatus.Pending)
            {
                _loadOperation.TrySetResult();
                return;
            }

            // we are not the first LOD that loaded so make sure to update genie instances already created with this new lod
            foreach (Genie genie in _createdGenies)
            {
                var lodConfig = new Genie.LodConfig
                {
                    root                           = lodRef.Item.InstantiateLodRoot(genie.Root.transform),
                    screenRelativeTransitionHeight = lodSource.screenRelativeTransitionHeight,
                    fadeTransitionWidth            = lodSource.fadeTransitionWidth,
                };

                genie.AddLod(lodConfig);
            }
        }
    }
}
