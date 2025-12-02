using UnityEngine;
using GUnityGLTF.Loader;

namespace GUnityGLTF
{
	public abstract class ImporterFactory : ScriptableObject
	{
		public abstract GLTFSceneImporter CreateSceneImporter(string gltfFileName, ImportOptions options);
	}

	public class DefaultImporterFactory : ImporterFactory
	{
		public override GLTFSceneImporter CreateSceneImporter(string gltfFileName, ImportOptions options)
		{
			return new GLTFSceneImporter(gltfFileName, options);
		}
	}
}
