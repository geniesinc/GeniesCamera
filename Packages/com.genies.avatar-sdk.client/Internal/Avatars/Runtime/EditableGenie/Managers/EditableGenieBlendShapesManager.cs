using System.Collections.Generic;
using Genies.Utilities;
using UnityEngine;

namespace Genies.Avatars
{
    internal sealed class EditableGenieBlendShapesManager
    {
        // dependencies
        private readonly EditableGenie _genie;
        private readonly Dictionary<string, float> _blendShapes = new();

        public EditableGenieBlendShapesManager(EditableGenie genie)
        {
            _genie = genie;
        }

        public void ApplyBlendShapes()
        {
            foreach (SkinnedMeshRenderer renderer in _genie.Renderers)
            {
                if (!renderer)
                {
                    continue;
                }

                Mesh mesh = renderer.sharedMesh;
                if (!mesh)
                {
                    continue;
                }

                renderer.ResetBlendShapeWeights();
                foreach ((string shapeName, float value) in _blendShapes)
                {
                    int shapeIndex = mesh.GetBlendShapeIndex(shapeName);
                    if (shapeIndex >= 0 && shapeIndex < mesh.blendShapeCount)
                    {
                        renderer.SetBlendShapeWeight(shapeIndex, value * 100.0f);
                    }
                }
            }
        }

        public void SetBlendShape(string name, float value)
        {
            if (string.IsNullOrEmpty(name))
            {
                return;
            }

            _blendShapes[name] = value;
            ApplyBlendShapeValue(name, value);
        }

        public void SetBlendShape(string name, float value, bool baked)
        {
            SetBlendShape(name, value);
        }

        public float GetBlendShape(string name)
        {
            if (_blendShapes.TryGetValue(name, out float value))
            {
                return value;
            }

            foreach (SkinnedMeshRenderer renderer in _genie.Renderers)
            {
                Mesh mesh = renderer.sharedMesh;
                if (!mesh)
                {
                    continue;
                }

                int shapeIndex = mesh.GetBlendShapeIndex(name);
                if (shapeIndex >= 0 && shapeIndex < mesh.blendShapeCount)
                {
                    return 0.01f * renderer.GetBlendShapeWeight(shapeIndex);
                }
            }

            return 0.0f;
        }

        public bool RemoveBlendShape(string name)
        {
            return !string.IsNullOrEmpty(name) && _blendShapes.Remove(name);
        }

        public bool IsBlendShapeBaked(string name)
        {
            return false;
        }

        public bool ContainsBlendShape(string name)
        {
            if (!string.IsNullOrEmpty(name) && _blendShapes.ContainsKey(name))
            {
                return true;
            }

            foreach (SkinnedMeshRenderer renderer in _genie.Renderers)
            {
                Mesh mesh = renderer.sharedMesh;
                if (!mesh)
                {
                    continue;
                }

                int shapeIndex = mesh.GetBlendShapeIndex(name);
                if (shapeIndex >= 0 && shapeIndex < mesh.blendShapeCount)
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyBlendShapeValue(string name, float value)
        {
            foreach (SkinnedMeshRenderer renderer in _genie.Renderers)
            {
                Mesh mesh = renderer.sharedMesh;
                if (!mesh)
                {
                    continue;
                }

                int shapeIndex = mesh.GetBlendShapeIndex(name);
                if (shapeIndex >= 0 && shapeIndex < mesh.blendShapeCount)
                {
                    renderer.SetBlendShapeWeight(shapeIndex, value);
                }
            }
        }

        public void Dispose()
        {
            _blendShapes.Clear();
        }
    }
}
