#if UNITY_EDITOR
using Genies.Refs;
using Genies.Utilities;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace Genies.Avatars.Context
{
    /// <summary>
    /// Created with the only purpose of fixing normal maps when targeting the Android platform in the editor.
    /// </summary>
    public static class ContentNormalMapFix
    {
        private const string _ag2RgbShaderName = "Genies/Utils/AG to RGB Normal";
        private static Material _ag2RgbMaterial;

        public static bool IsFixNeeded()
        {
            // we need to fix DXT5/ASTC normal maps when shaders are compiled with no DXT5 support
            return GraphicsSettings.HasShaderDefine(BuiltinShaderDefine.UNITY_NO_DXT5nm);
        }

        public static Ref<Texture> GetFixedNormalMap(Texture texture)
        {
            if (!IsFixNeeded())
                return default;
            
            // if texture is not a normal map in AG format then do nothing
            if (!GraphicsFormatUtility.IsDXTCFormat(texture.graphicsFormat) && !GraphicsFormatUtility.IsASTCFormat(texture.graphicsFormat))
                return default;
            
            if (!TryInitializeMaterial())
                return default;

            RenderTexture fixedTexture;
            switch (texture)
            {
                case Texture2D texture2d:
                    fixedTexture = texture2d.ToRenderTexture();
                    break;
                case RenderTexture renderTexture:
                    fixedTexture = renderTexture.CreateCopy();
                    break;
                default:
                    return default;
            }
            
            fixedTexture.name = $"{texture.name}--fixed";
            Graphics.Blit(texture, fixedTexture, _ag2RgbMaterial);
            
            return CreateRef.FromUnityObject((Texture)fixedTexture);
        }

        private static bool TryInitializeMaterial()
        {
            if (_ag2RgbMaterial)
                return true;
            
            var shader = Shader.Find(_ag2RgbShaderName);
            if (!shader)
            {
                Debug.LogError($"[{nameof(ContentNormalMapFix)}] couldn't load AG to RGB normal shader from name: {_ag2RgbShaderName}");
                return false;
            }

            _ag2RgbMaterial = new Material(shader);
            return true;
        }
    }
}
#endif