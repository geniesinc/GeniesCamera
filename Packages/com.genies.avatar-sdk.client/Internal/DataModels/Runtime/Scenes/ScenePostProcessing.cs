using Genies.Utilities;
using Newtonsoft.Json;
using UnityEngine;

namespace Genies.Models
{
    /// <summary>
    /// Models the 'look' scene postprocessing parameters
    /// </summary>
    [System.Serializable]
    public class ScenePostProcessing
    {
        /// <summary>
        /// Enabling flag of using the mobile post-processing package.
        /// </summary>
        public bool UseMobilePpPackageData = true;
        
        //pp settings
        public bool Blur;
        [Range(0, 1)]
        public float BlurAmount = 1f;
        public bool Bloom;
        
        public Color BloomColor = Color.white;
        [Range(0, 5)]
        public float BloomAmount = 1f;
        [Range(0, 1)]
        public float BloomDiffuse = 1f;
        [Range(0, 1)]
        public float BloomThreshold;
        [Range(0, 1)]
        public float BloomSoftness;
        public bool ImageFiltering;
        
        public Color Color = Color.white;
        [Range(0, 1)]
        public float Contrast;
        [Range(-1, 1)]
        public float Brightness;
        [Range(-1, 1)]
        public float Saturation;
        [Range(-1, 1)]
        public float Exposure;
        [Range(-1, 1)]
        public float Gamma;
        [Range(0, 1)]
        public float Sharpness;
        public bool ChromaticAberration;
        public float Offset;
        [Range(-10, 10)]
        public float FishEyeDistortion;
        [Range(0, 1)]
        public float GlitchAmount;
    
        public bool Distortion;
        [Range(0, 1)]
        public float LensDistortion;
    
        public bool Vignette;
        
        public Color VignetteColor = Color.black;
        [Range(0, 1)]
        public float VignetteAmount;
        [Range(0.001f, 1)]
        public float VignetteSoftness = 0.001f;
    }
}