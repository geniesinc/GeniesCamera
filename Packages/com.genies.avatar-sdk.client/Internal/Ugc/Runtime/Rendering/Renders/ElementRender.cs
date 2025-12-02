using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Shaders;
using Genies.Refs;
using UnityEngine;

namespace Genies.Ugc
{
    public sealed class ElementRender : IElementRender
    {
        public int RegionCount { get; }
        public bool IsAlive { get; private set; }
        public GameObject Root => IsAlive ? _gameObjectRef.Item : null;
        public Bounds Bounds { get; }
        public bool UseDefaultColors { get => _megaMaterial.UseDefaultColors; set => _megaMaterial.UseDefaultColors = value; }
        public bool RegionDebugging { get => _regionDebugging; set => SetRegionDebugging(value).Forget(); }

        private readonly Ref<GameObject> _gameObjectRef;
        private readonly MegaMaterial _megaMaterial;
        private readonly List<Vector3> _vertices;
        private readonly IMaterialAnimation[] _colorAnimations;
        private readonly IMaterialAnimation[] _patternAnimations;

        private MegaMaterial _debuggingMegaMaterial;
        private bool _regionDebugging;

        public ElementRender(int regionCount, Ref<GameObject> gameObjectRef,
            MegaMaterial megaMaterial, Bounds bounds, List<Vector3> vertices)
        {
            RegionCount = regionCount;
            _gameObjectRef = gameObjectRef;
            _megaMaterial = megaMaterial;
            Bounds = bounds;
            _vertices = vertices;

            _colorAnimations = new IMaterialAnimation[regionCount];
            _patternAnimations = new IMaterialAnimation[regionCount];

            for (int i = 0; i < regionCount; ++i)
            {
                _colorAnimations[i] = new RegionColorAnimation(megaMaterial.Material, i);
                _patternAnimations[i] = new RegionGainAnimation(megaMaterial.Material, i);
            }

            IsAlive = true;
        }

        public UniTask ApplySplitAsync(Split split)
        {
            if (!_regionDebugging)
            {
                foreach (Region region in split.Regions)
                {
                    StopRegionAnimationNoRestore(region.RegionNumber - 1);
                }
            }

            return _megaMaterial.ApplySplitAsync(split);
        }

        public UniTask ApplyRegionsAsync(IEnumerable<Region> regions)
        {
            if (!_regionDebugging)
            {
                foreach (Region region in regions)
                {
                    StopRegionAnimationNoRestore(region.RegionNumber - 1);
                }
            }

            return _megaMaterial.ApplyRegionsAsync(regions);
        }

        public UniTask ApplyRegionAsync(Region region)
        {
            if (!_regionDebugging)
            {
                StopRegionAnimationNoRestore(region.RegionNumber - 1);
            }

            return _megaMaterial.ApplyRegionAsync(region);
        }

        public UniTask ApplyStyleAsync(Style style, int regionIndex)
        {
            if (regionIndex < 0 || regionIndex >= RegionCount)
            {
                return UniTask.CompletedTask;
            }

            if (!_regionDebugging)
            {
                StopRegionAnimationNoRestore(regionIndex);
            }

            return _megaMaterial.ApplyStyleAsync(style, regionIndex);
        }

        public Bounds GetAlignedBounds(Quaternion rotation)
        {
            var min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

            for (int i = 0; i < _vertices.Count; ++i)
            {
                Vector3 vertex = rotation * _vertices[i];

                if (vertex.x < min.x)
                {
                    min.x = vertex.x;
                }

                if (vertex.y < min.y)
                {
                    min.y = vertex.y;
                }

                if (vertex.z < min.z)
                {
                    min.z = vertex.z;
                }

                if (vertex.x > max.x)
                {
                    max.x = vertex.x;
                }

                if (vertex.y > max.y)
                {
                    max.y = vertex.y;
                }

                if (vertex.z > max.z)
                {
                    max.z = vertex.z;
                }
            }

            var bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }

        public void PlayAnimation(ValueAnimation animation)
        {
            for (int i = 0; i < RegionCount; ++i)
            {
                PlayRegionAnimation(i, animation, false);
            }
        }

        public void StopAnimation()
        {
            for (int i = 0; i < RegionCount; ++i)
            {
                StopRegionAnimation(i);
            }
        }

        public void PlayRegionAnimation(int regionIndex, ValueAnimation animation, bool playAlone = false)
        {
            if (regionIndex < 0 || regionIndex >= RegionCount)
            {
                return;
            }

            if (playAlone)
            {
                // stop all other region's animations
                for (int i = 0; i < RegionCount; ++i)
                {
                    if (i != regionIndex)
                    {
                        StopRegionAnimation(i);
                    }
                }
            }

            // depending on if the pattern is active or not we will play the pattern gain or style color animations
            bool isPatternActive = _megaMaterial.Material.GetTexture(MegaShaderRegionProperty.PatternTexture, regionIndex);

            if (isPatternActive && !_regionDebugging)
            {
                _patternAnimations[regionIndex].PlayAsync(animation, true).Forget();
            }
            else
            {
                _colorAnimations[regionIndex].PlayAsync(animation, true).Forget();
            }
        }

        public void StopRegionAnimation(int regionIndex)
        {
            if (regionIndex < 0 || regionIndex >= RegionCount)
            {
                return;
            }

            _colorAnimations[regionIndex].Stop();
            _patternAnimations[regionIndex].Stop();
        }

        public void Dispose()
        {
            if (!IsAlive)
            {
                return;
            }

            _gameObjectRef.Dispose();
            _megaMaterial.Dispose();
            _debuggingMegaMaterial?.Dispose();

            IsAlive = false;
        }

        private async UniTaskVoid SetRegionDebugging(bool value)
        {
            if (_regionDebugging == value || !IsAlive)
            {
                return;
            }

            _regionDebugging = value;
            MegaMaterial megaMaterial;

            if (_regionDebugging)
            {
                _debuggingMegaMaterial ??= await _megaMaterial.CreateRegionDebuggingMaterialAsync();
                megaMaterial = _debuggingMegaMaterial;
            }
            else
            {
                megaMaterial = _megaMaterial;
            }

            // update the renderer's material (since this is for debugging purposes its ok to use GetComponentsInChildren everytime)
            if (_gameObjectRef.IsAlive)
            {
                MeshRenderer[] renderers = _gameObjectRef.Item.GetComponentsInChildren<MeshRenderer>();
                foreach (MeshRenderer renderer in renderers)
                {
                    renderer.sharedMaterial = megaMaterial.Material;
                }
            }

            // update the animation's material
            foreach (IMaterialAnimation animation in _colorAnimations)
            {
                animation.Material = megaMaterial.Material;
            }

            foreach (IMaterialAnimation animation in _patternAnimations)
            {
                animation.Material = megaMaterial.Material;
            }
        }

        private void StopRegionAnimationNoRestore(int regionIndex)
        {
            if (regionIndex < 0 || regionIndex >= RegionCount)
            {
                return;
            }

            _colorAnimations[regionIndex].StopNoRestore();
            _patternAnimations[regionIndex].StopNoRestore();
        }
    }
}
