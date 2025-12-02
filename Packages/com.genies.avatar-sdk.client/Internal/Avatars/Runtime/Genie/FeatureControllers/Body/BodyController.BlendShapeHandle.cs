using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
    public sealed partial class BodyController
    {
        private sealed class BlendShapeHandle
        {
            // total sum of the weight for this blend shape from all attributes affecting it
            public float Weight;
            // count of how many attributes are controlling this blend shape
            public float ControllingAttributes;
         
            private readonly string _name;
            private readonly IGenie _genie;
            private readonly List<(SkinnedMeshRenderer, int, float)> _renderers;
            
            public BlendShapeHandle(string name, IGenie genie)
            {
                _name = name;
                _genie = genie;
                _renderers = new List<(SkinnedMeshRenderer, int, float)>();
            }

            public void Rebuild()
            {
                if (_genie is null)
                {
                    return;
                }

                _renderers.Clear();
                foreach (SkinnedMeshRenderer renderer in _genie.Renderers)
                {
                    Mesh mesh = renderer.sharedMesh;
                    if (!mesh)
                    {
                        continue;
                    }

                    int index = mesh.GetBlendShapeIndex(_name);
                    if (index < 0)
                    {
                        continue;
                    }

                    float defaultWeight = renderer.GetBlendShapeWeight(index);
                    _renderers.Add((renderer, index, defaultWeight));
                }
            }

            public void Apply()
            {
                // get the average weight from all attributes (we can potentially just use the total added weight, clamp it, or do anything we want)
                float weight = Weight / ControllingAttributes;
                foreach ((SkinnedMeshRenderer renderer, int blendShapeIndex, _) in _renderers)
                {
                    renderer.SetBlendShapeWeight(blendShapeIndex, weight);
                }
            }

            public void ApplyDefault()
            {
                Weight = 0.0f;
                ControllingAttributes = 0.0f;
                foreach ((SkinnedMeshRenderer renderer, int blendShapeIndex, float defaultWeight) in _renderers)
                {
                    renderer.SetBlendShapeWeight(blendShapeIndex, defaultWeight);
                }
            }
            
            public void Reset()
            {
                Weight = 0.0f;
                ControllingAttributes = 0.0f;
            }
        }
    }
}