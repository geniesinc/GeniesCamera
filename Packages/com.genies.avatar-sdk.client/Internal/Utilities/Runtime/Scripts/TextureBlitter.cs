using System;
using UnityEngine;

namespace Genies.Utilities
{
    /// <summary>
    /// Utility for efficient copying of any texture into a given render texture, optionally defining the source and
    /// target rects. Any pixels outside the target rect within the render texture will keep their initial colors, which
    /// is useful for things like generating texture atlases.
    /// <br/><br/>
    /// Usage example: if we have a 64x64 source texture and a 256x256 target render texture, the following blit call
    /// would render the top-right quarter of the source texture in the top-right quarter of the target texture:
    /// <code>
    /// BlitIntoUvRect(source, target,
    ///     sourceXMin: 32, sourceYMin: 32, sourceXMax: 64, sourceYMax: 64,
    ///     targetXMin: 128, targetYMin: 128, targetXMax: 256, targetYMax: 256);
    /// </code>
    /// </summary>
    public static class TextureBlitter
    {
        private const string TextureBlitShaderName = "Genies/Utils/Texture Blit";
        
        private static Material _textureBlitMaterial;

#region TextureBlit
        public static void BlitIntoUvRect(Texture source, RenderTexture target, RectInt targetRect)
        {
            var sourceRect = new RectInt(0, 0, source.width, source.height);
            BlitIntoUvRect(source, target, sourceRect, targetRect);
        }

        public static void BlitIntoUvRect(Texture source, RenderTexture target, RectInt sourceRect, RectInt targetRect)
        {
            int sourceXMax = sourceRect.x + sourceRect.width;
            int sourceYMax = sourceRect.y + sourceRect.height;
            int targetXMax = targetRect.x + targetRect.width;
            int targetYMax = targetRect.y + targetRect.height;
            
            BlitIntoUvRect(source, target,
                sourceRect.x, sourceRect.y, sourceXMax, sourceYMax,
                targetRect.x, targetRect.y, targetXMax, targetYMax);
        }
        
        public static void BlitIntoUvRect(Texture source, RenderTexture target, Vector2Int targetMin, Vector2Int targetMax)
        {
            BlitIntoUvRect(source, target,
                0, 0, source.width, source.height,
                targetMin.x, targetMin.y, targetMax.x, targetMax.y);
        }

        public static void BlitIntoUvRect(Texture source, RenderTexture target,
            Vector2Int sourceMin, Vector2Int sourceMax,
            Vector2Int targetMin, Vector2Int targetMax
        ) {
            BlitIntoUvRect(source, target,
                sourceMin.x, sourceMin.y, sourceMax.x, sourceMax.y,
                targetMin.x, targetMin.y, targetMax.x, targetMax.y);
        }
        
        public static void BlitIntoUvRect(Texture source, RenderTexture target,
            int targetXMin, int targetYMin, int targetXMax, int targetYMax
        ) {
            BlitIntoUvRect(source, target,
                0, 0, source.width, source.height,
                targetXMin, targetYMin, targetXMax, targetYMax);
        }
        
        public static void BlitIntoUvRect(Texture source, RenderTexture target,
            int sourceXMin, int sourceYMin, int sourceXMax, int sourceYMax,
            int targetXMin, int targetYMin, int targetXMax, int targetYMax
        ) {
            EnsureTextureBlitMaterial();
            
            _textureBlitMaterial.mainTexture = source;
            _textureBlitMaterial.SetPass(0);
            
            float normSourceXMin = sourceXMin / (float)source.width;
            float normSourceYMin = sourceYMin / (float)source.height;
            float normSourceXMax = sourceXMax / (float)source.width;
            float normSourceYMax = sourceYMax / (float)source.height;
            
            ExecuteRenderPass(target,
                normSourceXMin, normSourceYMin, normSourceXMax, normSourceYMax,
                targetXMin, targetYMin, targetXMax, targetYMax);
        }
#endregion

#region ColorBlit
        public static void BlitIntoUvRect(Color color, RenderTexture target, RectInt targetRect)
        {
            BlitIntoUvRect(color, target, targetRect.min, targetRect.max);
        }

        public static void BlitIntoUvRect(Color color, RenderTexture target, Vector2Int targetMin, Vector2Int targetMax)
        {
            BlitIntoUvRect(color, target, targetMin.x, targetMin.y, targetMax.x, targetMax.y);
        }

        public static void BlitIntoUvRect(Color color, RenderTexture target,
            int targetXMin, int targetYMin, int targetXMax, int targetYMax
        ) {
            EnsureTextureBlitMaterial();
            
            _textureBlitMaterial.color = color;
            _textureBlitMaterial.mainTexture = null;
            _textureBlitMaterial.SetPass(0);
            
            ExecuteRenderPass(target,
                0.0f, 0.0f, 1.0f, 1.0f,
                targetXMin, targetYMin, targetXMax, targetYMax);
        }
#endregion
        
        private static void ExecuteRenderPass(
            RenderTexture target,
            float normSourceXMin, float normSourceYMin, float normSourceXMax, float normSourceYMax,
            int targetXMin, int targetYMin, int targetXMax, int targetYMax
        ) {
            // initialize texture coordinate vectors
            var texCoord0 = new Vector3(normSourceXMin, normSourceYMin, 0.0f);
            var texCoord1 = new Vector3(normSourceXMin, normSourceYMax, 0.0f);
            var texCoord2 = new Vector3(normSourceXMax, normSourceYMax, 0.0f);
            var texCoord3 = new Vector3(normSourceXMax, normSourceYMin, 0.0f);
            
            //  initialize rect vertices
            var vertex0 = new Vector3(targetXMin, targetYMin, 0.0f);
            var vertex1 = new Vector3(targetXMin, targetYMax, 0.0f);
            var vertex2 = new Vector3(targetXMax, targetYMax, 0.0f);
            var vertex3 = new Vector3(targetXMax, targetYMin, 0.0f);
            
            RenderTexture previousRt = RenderTexture.active;
            RenderTexture.active = target;
            
            GL.PushMatrix();
            {
                GL.LoadPixelMatrix(0.0f, target.width, 0.0f, target.height);
                
                GL.Begin(GL.QUADS);
                {
                    GL.TexCoord(texCoord0); GL.Vertex(vertex0);
                    GL.TexCoord(texCoord1); GL.Vertex(vertex1);
                    GL.TexCoord(texCoord2); GL.Vertex(vertex2);
                    GL.TexCoord(texCoord3); GL.Vertex(vertex3);
                }
                GL.End();
            }
            GL.PopMatrix();
            
            RenderTexture.active = previousRt;
            _textureBlitMaterial.color = Color.white;
            _textureBlitMaterial.mainTexture = null;
        }
        
        private static void EnsureTextureBlitMaterial()
        {
            if (_textureBlitMaterial)
            {
                return;
            }

            var shader = Shader.Find(TextureBlitShaderName);
            if (!shader)
            {
                throw new Exception($"[{nameof(TextureBlitter)}] couldn't load texture blit shader from name: {TextureBlitShaderName}. Make sure the shader is included in the build");
            }

            _textureBlitMaterial = new Material(shader);
        }
    }
}
