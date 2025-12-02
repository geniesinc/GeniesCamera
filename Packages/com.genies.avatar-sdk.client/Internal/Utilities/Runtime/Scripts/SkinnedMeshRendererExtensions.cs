using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Utilities
{
    public static partial class SkinnedMeshRendererExtensions
    {
        public static void ResetBlendShapeWeights(this SkinnedMeshRenderer renderer)
        {
            if (!renderer.sharedMesh)
            {
                return;
            }

            int blendShapeCount = renderer.sharedMesh.blendShapeCount;
            for (int i = 0; i < blendShapeCount; ++i)
            {
                renderer.SetBlendShapeWeight(i, 0.0f);
            }
        }
        
#region BlendShapeState
        public static BlendShapeState GetBlendShapeState(this SkinnedMeshRenderer renderer, int shapeIndex)
        {
            if (!renderer.sharedMesh || shapeIndex < 0 || shapeIndex >= renderer.sharedMesh.blendShapeCount)
            {
                return default;
            }

            return new BlendShapeState(shapeIndex, renderer.GetBlendShapeWeight(shapeIndex));
        }
        
        public static BlendShapeState GetBlendShapeState(this SkinnedMeshRenderer renderer, string shapeName)
        {
            if (!renderer.sharedMesh || string.IsNullOrEmpty(shapeName))
            {
                return default;
            }

            int shapeIndex = renderer.sharedMesh.GetBlendShapeIndex(shapeName);
            if (shapeIndex < 0)
            {
                return default;
            }

            return new BlendShapeState(shapeIndex, renderer.GetBlendShapeWeight(shapeIndex));
        }
        
        public static List<BlendShapeState> GetBlendShapeStates(this SkinnedMeshRenderer renderer, IEnumerable<int> shapeIndices)
        {
            var shapeStates = new List<BlendShapeState>(renderer.sharedMesh.blendShapeCount);
            GetBlendShapeStates(renderer, shapeIndices, shapeStates);
            return shapeStates;
        }

        public static void GetBlendShapeStates(this SkinnedMeshRenderer renderer, IEnumerable<int> shapeIndices, ICollection<BlendShapeState> results)
        {
            Mesh mesh = renderer.sharedMesh;
            if (shapeIndices is null || results is null || !mesh)
            {
                return;
            }

            foreach (int shapeIndex in shapeIndices)
            {
                if (shapeIndex < 0 || shapeIndex >= mesh.blendShapeCount)
                {
                    continue;
                }

                results.Add(new BlendShapeState(shapeIndex, renderer.GetBlendShapeWeight(shapeIndex)));
            }
        }
        
        public static List<BlendShapeState> GetBlendShapeStates(this SkinnedMeshRenderer renderer, IEnumerable<string> shapeNames)
        {
            var shapeStates = new List<BlendShapeState>(renderer.sharedMesh.blendShapeCount);
            GetBlendShapeStates(renderer, shapeNames, shapeStates);
            return shapeStates;
        }

        public static void GetBlendShapeStates(this SkinnedMeshRenderer renderer, IEnumerable<string> shapeNames, ICollection<BlendShapeState> results)
        {
            Mesh mesh = renderer.sharedMesh;
            if (shapeNames is null || results is null || !mesh)
            {
                return;
            }

            foreach (string shapeName in shapeNames)
            {
                int shapeIndex = mesh.GetBlendShapeIndex(shapeName);
                if (shapeIndex < 0)
                {
                    continue;
                }

                results.Add(new BlendShapeState(shapeIndex, renderer.GetBlendShapeWeight(shapeIndex)));
            }
        }

        public static List<BlendShapeState> GetBlendShapeStates(this SkinnedMeshRenderer renderer, Func<string, bool> filter = null)
        {
            var shapeStates = new List<BlendShapeState>(renderer.sharedMesh.blendShapeCount);
            GetBlendShapeStates(renderer, shapeStates, filter);
            return shapeStates;
        }

        public static void GetBlendShapeStates(this SkinnedMeshRenderer renderer, ICollection<BlendShapeState> results, Func<string, bool> filter = null)
        {
            if (results is null || !renderer.sharedMesh)
            {
                return;
            }

            Mesh mesh = renderer.sharedMesh;
            int blendShapeCount = mesh.blendShapeCount;
            filter ??= item => true;

            for (int shapeIndex = 0; shapeIndex < blendShapeCount; ++shapeIndex)
            {
                string blendShapeName = mesh.GetBlendShapeName(shapeIndex);
                if (filter.Invoke(blendShapeName))
                {
                    results.Add(new BlendShapeState(shapeIndex, renderer.GetBlendShapeWeight(shapeIndex)));
                }
            }
        }

        public static void SetBlendShapeState(this SkinnedMeshRenderer renderer, BlendShapeState state)
        {
            renderer.SetBlendShapeWeight(state.Index, state.Weight);
        }
        
        public static void SetBlendShapeStates(this SkinnedMeshRenderer renderer, IEnumerable<BlendShapeState> states)
        {
            if (states is null)
            {
                return;
            }

            foreach (BlendShapeState state in states)
            {
                renderer.SetBlendShapeWeight(state.Index, state.Weight);
            }
        }
#endregion

#region NamedBlendShapeState
        public static NamedBlendShapeState GetNamedBlendShapeState(this SkinnedMeshRenderer renderer, int shapeIndex)
        {
            if (!renderer.sharedMesh || shapeIndex < 0 || shapeIndex >= renderer.sharedMesh.blendShapeCount)
            {
                return default;
            }

            string name = renderer.sharedMesh.GetBlendShapeName(shapeIndex);
            return new NamedBlendShapeState(name, renderer.GetBlendShapeWeight(shapeIndex));
        }
        
        public static NamedBlendShapeState GetNamedBlendShapeState(this SkinnedMeshRenderer renderer, string shapeName)
        {
            if (!renderer.sharedMesh || string.IsNullOrEmpty(shapeName))
            {
                return default;
            }

            int shapeIndex = renderer.sharedMesh.GetBlendShapeIndex(shapeName);
            if (shapeIndex < 0)
            {
                return default;
            }

            return new NamedBlendShapeState(shapeName, renderer.GetBlendShapeWeight(shapeIndex));
        }
        
        public static List<NamedBlendShapeState> GetNamedBlendShapeStates(this SkinnedMeshRenderer renderer, IEnumerable<int> shapeIndices)
        {
            var shapeStates = new List<NamedBlendShapeState>(renderer.sharedMesh.blendShapeCount);
            GetNamedBlendShapeStates(renderer, shapeIndices, shapeStates);
            return shapeStates;
        }

        public static void GetNamedBlendShapeStates(this SkinnedMeshRenderer renderer, IEnumerable<int> shapeIndices, ICollection<NamedBlendShapeState> results)
        {
            Mesh mesh = renderer.sharedMesh;
            if (shapeIndices is null || results is null || !mesh)
            {
                return;
            }

            foreach (int shapeIndex in shapeIndices)
            {
                if (shapeIndex < 0 || shapeIndex >= mesh.blendShapeCount)
                {
                    continue;
                }

                string shapeName = mesh.GetBlendShapeName(shapeIndex);
                results.Add(new NamedBlendShapeState(shapeName, renderer.GetBlendShapeWeight(shapeIndex)));
            }
        }
        
        public static List<NamedBlendShapeState> GetNamedBlendShapeStates(this SkinnedMeshRenderer renderer, IEnumerable<string> shapeNames)
        {
            var shapeStates = new List<NamedBlendShapeState>(renderer.sharedMesh.blendShapeCount);
            GetNamedBlendShapeStates(renderer, shapeNames, shapeStates);
            return shapeStates;
        }

        public static void GetNamedBlendShapeStates(this SkinnedMeshRenderer renderer, IEnumerable<string> shapeNames, ICollection<NamedBlendShapeState> results)
        {
            Mesh mesh = renderer.sharedMesh;
            if (shapeNames is null || results is null || !mesh)
            {
                return;
            }

            foreach (string shapeName in shapeNames)
            {
                int shapeIndex = mesh.GetBlendShapeIndex(shapeName);
                if (shapeIndex < 0)
                {
                    continue;
                }

                results.Add(new NamedBlendShapeState(shapeName, renderer.GetBlendShapeWeight(shapeIndex)));
            }
        }

        public static List<NamedBlendShapeState> GetNamedBlendShapeStates(this SkinnedMeshRenderer renderer, Func<string, bool> filter = null)
        {
            var shapeStates = new List<NamedBlendShapeState>(renderer.sharedMesh.blendShapeCount);
            GetNamedBlendShapeStates(renderer, shapeStates, filter);
            return shapeStates;
        }

        public static void GetNamedBlendShapeStates(this SkinnedMeshRenderer renderer, ICollection<NamedBlendShapeState> results, Func<string, bool> filter = null)
        {
            if (results is null || !renderer.sharedMesh)
            {
                return;
            }

            Mesh mesh = renderer.sharedMesh;
            int blendShapeCount = mesh.blendShapeCount;
            filter ??= item => true;

            for (int shapeIndex = 0; shapeIndex < blendShapeCount; ++shapeIndex)
            {
                string blendShapeName = mesh.GetBlendShapeName(shapeIndex);
                if (filter.Invoke(blendShapeName))
                {
                    results.Add(new NamedBlendShapeState(blendShapeName, renderer.GetBlendShapeWeight(shapeIndex)));
                }
            }
        }

        public static void SetNamedBlendShapeState(this SkinnedMeshRenderer renderer, NamedBlendShapeState state)
        {
            if (!renderer.sharedMesh || string.IsNullOrEmpty(state.Name))
            {
                return;
            }

            int shapeIndex = renderer.sharedMesh.GetBlendShapeIndex(state.Name);
            if (shapeIndex < 0)
            {
                return;
            }

            renderer.SetBlendShapeWeight(shapeIndex, state.Weight);
        }
        
        public static void SetNamedBlendShapeStates(this SkinnedMeshRenderer renderer, IEnumerable<NamedBlendShapeState> states)
        {
            Mesh sharedMesh = renderer.sharedMesh;
            if (states is null || !sharedMesh)
            {
                return;
            }

            foreach (NamedBlendShapeState state in states)
            {
                int shapeIndex = renderer.sharedMesh.GetBlendShapeIndex(state.Name);
                if (shapeIndex < 0)
                {
                    continue;
                }

                renderer.SetBlendShapeWeight(shapeIndex, state.Weight);
            }
        }
#endregion
    }
}
