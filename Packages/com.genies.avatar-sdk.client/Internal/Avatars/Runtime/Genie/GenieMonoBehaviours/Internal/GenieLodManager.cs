using System;
using System.Collections.Generic;
using Genies.Utilities;
using UnityEngine;

using Object              = UnityEngine.Object;
using LodConfig           = Genies.Avatars.Genie.LodConfig;
using SharedLodConfig     = Genies.Avatars.Genie.SharedLodConfig;
using RendererBonesConfig = Genies.Avatars.Genie.RendererBonesConfig;

namespace Genies.Avatars
{
    /// <summary>
    /// Encapsulates LOD management for the <see cref="Genie"/> component.
    /// </summary>
    internal sealed class GenieLodManager
    {
        private static readonly Comparison<LodConfig> LodComparison =
            (x, y) => y.screenRelativeTransitionHeight.CompareTo(x.screenRelativeTransitionHeight);

        public GameObject                         ModelRoot { get; }
        public IReadOnlyList<LodConfig>           Lods      { get; }
        public IReadOnlyList<SkinnedMeshRenderer> Renderers { get; }

        public LODFadeMode FadeMode
        {
            get => _fadeMode;
            set
            {
                _fadeMode = value;
                if (_lodGroup)
                {
                    _lodGroup.fadeMode = value;
                }
            }
        }

        public bool AnimateCrossFading
        {
            get => _animateCrossFading;
            set
            {
                _animateCrossFading = value;
                if (_lodGroup)
                {
                    _lodGroup.animateCrossFading = value;
                }
            }
        }

        public bool AutomaticallyCalculateObjectSize
        {
            get => _automaticallyCalculateObjectSize;
            set
            {
                if (_automaticallyCalculateObjectSize == value)
                {
                    return;
                }

                _automaticallyCalculateObjectSize = value;
                if (!_lodGroup)
                {
                    return;
                }

                if (value)
                {
                    _lodGroup.RecalculateBounds();
                }
                else
                {
                    _lodGroup.size = _objectSize;
                }
            }
        }

        public float ObjectSize
        {
            get => _automaticallyCalculateObjectSize && _lodGroup ? _lodGroup.size : _objectSize;
            set
            {
                _objectSize = value;
                if (_lodGroup && !_automaticallyCalculateObjectSize)
                {
                    _lodGroup.size = value;
                }
            }
        }

        // state
        private readonly List<LodConfig>           _lods;
        private readonly List<SkinnedMeshRenderer> _renderers;
        private readonly SharedLodConfig           _sharedLodConfig;

        // lod group
        private LODGroup    _lodGroup;
        private LODFadeMode _fadeMode;
        private bool        _animateCrossFading;
        private bool        _automaticallyCalculateObjectSize;
        private float       _objectSize;

        public GenieLodManager(Transform genieRoot, SharedLodConfig sharedLodConfig)
        {
            if (!genieRoot)
            {
                throw new Exception($"[{nameof(GenieLodManager)}] no genie root was given");
            }

            if (sharedLodConfig.rendererConfigs is null)
            {
                throw new Exception($"[{nameof(GenieLodManager)}] no shared lod config was given");
            }

            _lods = new List<LodConfig>(4);
            _renderers = new List<SkinnedMeshRenderer>();
            _sharedLodConfig = sharedLodConfig;

            ModelRoot = new GameObject("Model");
            ModelRoot.transform.SetParent(genieRoot, worldPositionStays: false);
            ModelRoot.transform.ResetLocalTransform();
            ModelRoot.transform.SetAsFirstSibling();
            Lods = _lods.AsReadOnly();
            Renderers = _renderers.AsReadOnly();
        }

        public void AddLods(IEnumerable<LodConfig> lods)
        {
            foreach (LodConfig lod in lods)
            {
                if (TryGetLodIndex(lod.root, out int index))
                {
                    _lods[index] = lod;
                }
                else
                {
                    _lods.Add(lod);
                }
            }

            Rebuild();
        }

        public void AddLod(LodConfig lod)
        {
            // override the lod if its root was already registered
            if (TryGetLodIndex(lod.root, out int index))
            {
                _lods[index] = lod;
            }
            else
            {
                _lods.Add(lod);
            }

            Rebuild();
        }

        public void DestroyLod(int index)
        {
            if (index < 0 || index >= _lods.Count)
            {
                return;
            }

            if (_lods[index].root)
            {
                Object.Destroy(_lods[index].root);
            }

            _lods.RemoveAt(index);
            Rebuild();
        }

        public void DestroyAllLods()
        {
            foreach (LodConfig lod in _lods)
            {
                if (lod.root)
                {
                    Object.Destroy(lod.root);
                }
            }

            _lods.Clear();
            Rebuild();
        }

        public bool TryGetLodIndex(GameObject lodRoot, out int index)
        {
            if (lodRoot)
            {
                for (int i = 0; i < _lods.Count; ++i)
                {
                    if (_lods[i].root != lodRoot)
                    {
                        continue;
                    }

                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
        }

        private void Rebuild()
        {
            // register all lods sorted in descending order by their screenRelativeTransitionHeight
            _lods.Sort(LodComparison);
            _renderers.Clear();
            var lodGroupLods = new LOD[_lods.Count];

            for (int i = 0; i < _lods.Count; ++i)
            {
                LodConfig lodConfig = _lods[i];
                SkinnedMeshRenderer[] renderers = lodConfig.root.GetComponentsInChildren<SkinnedMeshRenderer>();

                // check that this lod matches the shared lod config
                if (renderers.Length != _sharedLodConfig.rendererConfigs.Count)
                {
                    Debug.LogError($"[{nameof(GenieLodManager)}] tried to add a Genie LOD that doesn't match the current shared LOD configuration. LOD index: {i}");
                    DestroyLod(i);
                    return;
                }

                // register the lod renderers
                _renderers.AddRange(renderers);

                // make sure the lod root is parented to the model root and assigned its LOD{i} name
                lodConfig.root.transform.SetParent(ModelRoot.transform, worldPositionStays: false);
                lodConfig.root.transform.ResetLocalTransform();
                lodConfig.root.transform.SetSiblingIndex(i);
                lodConfig.root.name = $"LOD{i}";

                // create and register the LODGroup LOD struct
                lodGroupLods[i] = new LOD
                {
                    screenRelativeTransitionHeight = lodConfig.screenRelativeTransitionHeight,
                    fadeTransitionWidth = lodConfig.fadeTransitionWidth,
                    renderers = renderers, // LODGroup should never be writing to this array so this co-variant conversion should be ok
                };
            }

            // no need to have a LODGroup component if we only have one lod
            if (_lods.Count <= 1)
            {
                if (_lodGroup)
                {
                    Object.Destroy(_lodGroup);
                }

                return;
            }

            // create lod group if didn't exist already
            if (!_lodGroup)
            {
                _lodGroup = ModelRoot.AddComponent<LODGroup>();
                _lodGroup.fadeMode = _fadeMode;
                _lodGroup.animateCrossFading = _animateCrossFading;
            }

            _lodGroup.SetLODs(lodGroupLods);

            if (!_automaticallyCalculateObjectSize)
            {
                _lodGroup.size = _objectSize;
            }

            ApplySharedLodConfig();
        }

        private void ApplySharedLodConfig()
        {
            // set the same bones to all renderers
            for (int lodIndex = 0; lodIndex < _lods.Count; ++lodIndex)
            {
                for (int rendererIndex = 0; rendererIndex < _sharedLodConfig.rendererConfigs.Count; ++rendererIndex)
                {
                    int globalIndex = lodIndex * _sharedLodConfig.rendererConfigs.Count + rendererIndex;
                    SkinnedMeshRenderer renderer = _renderers[globalIndex];
                    RendererBonesConfig config = _sharedLodConfig.rendererConfigs[rendererIndex];
                    renderer.rootBone = config.rootBone;
                    renderer.bones = config.bones;
                }
            }
        }
    }
}
