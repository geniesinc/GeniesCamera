using System.Text.RegularExpressions;
using Unity.Collections;
using UnityEngine;

namespace Genies.Avatars
{
    public static class UnifiedDnaDeformsHelper
    {
        public static MeshDeformationDescriptor GetDeformationDescriptor(string dnaKey, Mesh mesh, Allocator allocator, bool generateEmptyBindposes = true)
        {
            var descriptor = new MeshDeformationDescriptor
            {
                MortphTargetWeights = new NativeArray<float>(mesh.blendShapeCount, allocator, NativeArrayOptions.UninitializedMemory),
            };
            
            if (generateEmptyBindposes)
            {
                descriptor.TargetBindposes = new NativeArray<Matrix4x4>(0, allocator, NativeArrayOptions.UninitializedMemory);
            }

            var regex = new Regex($@"^.*(\.|_){dnaKey}$|^{dnaKey}$");
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string name = mesh.GetBlendShapeName(i);
                if (regex.IsMatch(name))
                {
                    descriptor.MortphTargetWeights[i] = 1.0f;
                }
                else
                {
                    descriptor.MortphTargetWeights[i] = 0.0f;
                }
            }
            
            return descriptor;
        }
    }
}