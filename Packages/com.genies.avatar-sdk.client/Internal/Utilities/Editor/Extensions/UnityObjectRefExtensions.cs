using UnityEditor;

namespace Genies.Utilities.Editor.Extensions
{
    public static class UnityObjectRefExtensions
    {
        /**
         * Creates a project asset from the given Unity object reference, at the given path. This function does not call
         * AssetDatabase.SaveAssets or AssetDatabase.Refresh, so you should call them after this function.
         */
        public static void CreateAsset(this UnityObjectRef objectRef, string path)
        {
            AssetDatabase.CreateAsset(objectRef.Object, path);
            AddDependencies(objectRef);
            return;

            // recursively add all dependencies to the asset
            void AddDependencies(UnityObjectRef obj)
            {
                for (int i = 0; i < obj.DependencyCount; ++i)
                {
                    UnityObjectRef dependency = obj.GetDependency(i);
                    AssetDatabase.AddObjectToAsset(dependency.Object, objectRef.Object);
                    AddDependencies(dependency);
                }
            }
        }
    }
}
