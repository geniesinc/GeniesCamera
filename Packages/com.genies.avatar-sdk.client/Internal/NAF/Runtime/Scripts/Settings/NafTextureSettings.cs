using System;
using System.Collections.Generic;
using GnWrappers;
using UnityEngine;
using UnityEngine.Rendering;

namespace Genies.Naf
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "TextureSettings", menuName = "Genies/NAF/Texture Settings")]
#endif
    public sealed class NafTextureSettings : ScriptableObject
    {
        [Tooltip("Depending on your shaders, you may want to use two-component normal maps (XY/RG) instead of three-component ones (XYZ/RGB). Enable this so any loaded normal map textures are loaded in the 2-component variant. Normal map textures that doesn't have the proper flavor variant will not load.")]
        public NormalMapType normalMapType = NormalMapType.Auto;

        [Tooltip("If a texture matches ETC1 as the preferred format but contains an alpha channel, then enable this setting so the system will try to find a lower preference format that supports alpha channel first. ETC1 is the only supported graphics format that doesn't support an alpha channel.")]
        public bool ignoreETC1IfTextureHasAlpha = true;

        [Tooltip("If true, when loading a texture, the system will include the texture source (if available) in the loaded texture, which can be used to load the original texture data (like a PNG, JPEG or KTX2 file). This is useful if you want to save the texture later on, or if you want to export to glTF. Depending on the source, enabling this may increase the CPU memory usage.")]
        public bool includeTextureSources = false;

        public List<Preference> preferences;

        public void Write(TextureSettings settings)
        {
            settings.useTwoComponentNormalMaps = normalMapType switch
            {
                NormalMapType.Auto           => !GraphicsSettings.HasShaderDefine(BuiltinShaderDefine.UNITY_NO_DXT5nm),
                NormalMapType.TwoComponent   => true,
                NormalMapType.ThreeComponent => false,
                _ => throw new ArgumentOutOfRangeException()
            };

            settings.ignoreETC1IfTextureHasAlpha = ignoreETC1IfTextureHasAlpha;
            settings.includeTextureSources       = includeTextureSources;

            settings.preferences.Clear();
            foreach (Preference preference in preferences)
            {
                using var item = new TextureSettingsPreference();
                item.format = preference.format;
                item.hqTranscoding = preference.hqTranscoding;
                if (preference.transcodingFallbacks != null)
                {
                    foreach (TranscodableTextureFormat format in preference.transcodingFallbacks)
                    {
                        item.transcodingFallbacks.Add(format);
                    }
                }

                settings.preferences.Add(item);
            }

            settings.Write();
        }

        public void Read(TextureSettings settings)
        {
            settings.Read();

            normalMapType = settings.useTwoComponentNormalMaps ? NormalMapType.TwoComponent : NormalMapType.ThreeComponent;

            ignoreETC1IfTextureHasAlpha = settings.ignoreETC1IfTextureHasAlpha;
            includeTextureSources       = settings.includeTextureSources;

            preferences.Clear();
            foreach (TextureSettingsPreference item in settings.preferences)
            {
                var preference = new Preference
                {
                    format               = item.format,
                    hqTranscoding        = item.hqTranscoding,
                    transcodingFallbacks = new List<TranscodableTextureFormat>(item.transcodingFallbacks)
                };
                preferences.Add(preference);
            }
        }

        public enum NormalMapType
        {
            Auto           = 0,
            TwoComponent   = 1,
            ThreeComponent = 2,
        }

        [Serializable]
        public struct Preference
        {
            public GnWrappers.TextureFormat format;

            [Tooltip("Fallback formats to try to transcode to the preferred format.")]
            public List<TranscodableTextureFormat> transcodingFallbacks;

            [Tooltip("When falling back to a transcoding format, requests high quality transcoding if available.")]
            public bool hqTranscoding;
        }

#if UNITY_EDITOR
        [ContextMenu("Read from Global Texture Settings")]
        public void ReadFromGlobal()
        {
            using var settings = TextureSettings.Global();
            Read(settings);

            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
        }
#endif
    }
}
