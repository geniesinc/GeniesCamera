using System;
namespace Genies.Avatars
{
    public static partial class GenieGltfImporter
    {
        [Serializable]
        public sealed class Settings
        {
            public bool multithreadedImport        = true;
            public bool keepCPUCopyOfMeshes        = false;
            public bool keepCPUCopyOfTextures      = false;
            public bool generateMipMapsForTextures = true;
        }
    }
}
