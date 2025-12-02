using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.Avatars
{
    /// <summary>
    /// Serializable settings for LOD compression type
    /// </summary>
    [Serializable]
    public enum LodTextureCompression
    {
        ETC1S, // ETC1S for all textures
        UASTC, // UASTC for all textures
        MIXED  // UASTC for normal maps and ETC1S for everything else
    }

    /// <summary>
    /// Serializable settings to generate LODs
    /// </summary>
    [Serializable]
    public sealed class LodGenerateSettings
    {
        [Tooltip("Whether or not to export each LOD in parallel.")]
        public bool runInParallel = true;

        [Tooltip("List of LODs to generate")]
        public List<LodSettings> lods = new();

        /// <summary>
        /// Stores the root directory of the LODs
        /// </summary>
        [NonSerialized]
        public string lodRoot;
    }

    /// <summary>
    /// Serializable settings for each LOD to be generated
    /// </summary>
    [Serializable]
    public sealed class LodSettings
    {
        [Tooltip("Name for the LOD")]
        public string name;

        [Tooltip("Ratio to the original texture for resizing."), Range(0.1f,1.0f)]
        public float textureRatio;

        [Tooltip("Ratio to the original mesh for decimation."), Range(0.1f, 1.0f)]
        public float meshRatio;

        [Tooltip("Compression type for textures.")]
        public LodTextureCompression compression;

        /// <summary>
        /// Stores the file path of the generated LOD
        /// </summary>
        [NonSerialized]
        public string filePath;

        /// <summary>
        /// Stores the file path of the report for the generated LOD
        /// </summary>
        [NonSerialized]
        public string reportPath;
    }
}
