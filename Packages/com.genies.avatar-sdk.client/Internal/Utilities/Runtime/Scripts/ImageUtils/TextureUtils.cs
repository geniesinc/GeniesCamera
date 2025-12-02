using System;
using System.IO;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.Refs;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace Genies.Utilities
{
    public static class TextureUtils
    {
        public static Rect GetPanAndZoomNormalizedRect(ref Vector2 pan, float zoom, Texture2D originalTexture)
        {
            int width  = originalTexture.width;
            int height = originalTexture.height;

            var (pos, size) = GetPanAndZoom(ref pan, zoom, originalTexture);
            return new Rect(pos.x * 1f / width, pos.y * 1f / height, size.x * 1f / width, size.y * 1f / height);
        }

        public static Texture2D ApplyPanAndZoomToTexture(ref Vector2 pan, float zoom, Texture2D originalTexture)
        {
            var rect = GetPanAndZoom(ref pan, zoom, originalTexture);
            return GenerateAndSetValidSubTexture(rect.pos.x, rect.pos.y, rect.size.x, rect.size.y, originalTexture);
        }

        private static(Vector2Int pos, Vector2Int size) GetPanAndZoom(ref Vector2 pan, float zoom, Texture2D originalTexture)
        {
            int width  = originalTexture.width;
            int height = originalTexture.height;

            //calculate potential new texture resolution
            int x = Mathf.CeilToInt(width * zoom);
            int y = Mathf.CeilToInt(height * zoom);

            //calculate starting pixel coordinates
            int startX = (int)(pan.x - x);
            int startY = (int)(pan.y - y);
            int endX   = startX + x;
            int endY   = startY + y;

            //clamp startX adjust pan based on new zoom
            if (startX < 0)
            {
                pan.x -= startX;
                startX = 0;
            }
            else if (endX >= width)
            {
                pan.x -= width - endX;
            }

            if (startY < 0)
            {
                pan.y -= startY;
                startY = 0;
            }
            else if (endY >= height)
            {
                pan.y -= height - endY;
            }

            return (pos: new Vector2Int(startX, startY), size: new Vector2Int(x, y));
        }

        private static Texture2D GenerateAndSetValidSubTexture(int startX, int startY, int width, int height, Texture2D originalTexture)
        {
            //clamp width and height of new texture so that it stays within bounds of our original texture
            width = Mathf.Clamp(width,   1, originalTexture.width - startX);
            height = Mathf.Clamp(height, 1, originalTexture.height - startY);

            width = Mathf.Clamp(width,   1, width);
            height = Mathf.Clamp(height, 1, height);

            return DuplicateTexture(originalTexture, isSquare: true, x: startX, y: startY, width: width, height: height);
        }

        public static Ref<Texture2D> DuplicateTexture(Texture2D source, int targetWidth, int targetHeight)
        {
            Texture2D rescaledTex = new Texture2D(targetWidth, targetHeight, source.format, true);
            Color[] pixelArray = rescaledTex.GetPixels(0);
            float multiplierX = 1.0f / targetWidth;
            float multiplierY = 1.0f / targetHeight;
            for (int px = 0; px < pixelArray.Length; px++)
            {
                pixelArray[px] = source.GetPixelBilinear(multiplierX * ((float)px % targetWidth), multiplierY * ((float)Mathf.Floor(px / targetWidth)));
            }
            rescaledTex.SetPixels(pixelArray, 0);
            rescaledTex.Apply();
            return CreateRef.FromUnityObject(rescaledTex);
        }

        public static Texture2D DuplicateTexture(Texture2D source, bool isSquare = false, float x = 0, float y = 0, int width = -1, int height = -1)
        {
            RenderTexture renderTex = RenderTexture.GetTemporary(
                                                                 source.width,
                                                                 source.height,
                                                                 0,
                                                                 RenderTextureFormat.Default,
                                                                 RenderTextureReadWrite.sRGB
                                                                );


            width = width == -1 ? renderTex.width : width;
            height = height == -1 ? renderTex.height : height;

            if (isSquare)
            {
                //get the shortest side
                int side = Math.Min(width, height);
                width = side;
                height = side;
            }

            Graphics.Blit(source, renderTex);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTex;
            var readableText = new Texture2D(width, height);
            var rect         = new Rect(x, y, width, height);

            Debug.Log($"rect: {rect} width: {renderTex.width} h:{renderTex.height}");
            readableText.ReadPixels(rect, 0, 0);
            readableText.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTex);
            return readableText;
        }

        public static Texture2D TextureResizeAndZoomRect(Texture2D original, Rect wantedRect, int newWidth, int newHeight, int alphaBorder)
        {
            Texture2D newTex = new Texture2D(newWidth, newHeight);

            for (int i = 0; i < newWidth; i++)
            {
                for (int j = 0; j < newHeight; j++)
                {
                    // Prevent bleeding by setting alphaBorder pixels to clear
                    if (i < alphaBorder || i >= newWidth - alphaBorder || j < alphaBorder || j >= newHeight - alphaBorder)
                    {
                        newTex.SetPixel(i, j, Color.clear);
                        continue;
                    }

                    float u = wantedRect.x + (i / (float)newWidth) * wantedRect.width;
                    float v = wantedRect.y + (j / (float)newHeight) * wantedRect.height;

                    // Calculate source pixel coordinates
                    int srcX = (int)(u * original.width);
                    int srcY = (int)(v * original.height);

                    srcX = Mathf.Clamp(srcX, 0, original.width - 1);
                    srcY = Mathf.Clamp(srcY, 0, original.height - 1);

                    Color originalColor = original.GetPixel(srcX, srcY);

                    newTex.SetPixel(i, j, originalColor);
                }
            }

            newTex.Apply();

            return newTex;
        }

        public static Ref<Texture2D> LoadTextureFromFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    var iconBytes = File.ReadAllBytes(filePath);
                    var texture = new Texture2D(2, 2);
                    Ref<Texture2D> textureRef = CreateRef.FromUnityObject(texture);
                    texture.LoadImage(iconBytes, false);
                    texture.Apply();
                    return textureRef;
                }
                CrashReporter.LogWarning($"Missing filepath: {filePath}");
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
            }

            return default;
        }

        public static void UnloadAllUnnamedTextures()
        {
            var textures = Resources.FindObjectsOfTypeAll<Texture2D>();

            foreach (var texture in textures)
            {
                if(string.IsNullOrEmpty(texture.name))
                {
                    Object.DestroyImmediate(texture);
                }
            }
        }

        public static async UniTask PreloadAndSaveTextureFromUrl(string url, string filePath)
        {
            try
            {
                UnityWebRequest request = UnityWebRequest.Get(url);
                await request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.ConnectionError)
                {
                    CrashReporter.Log($"Looks like the texture URL {url} is incorrect! Error: {request.error}", LogSeverity.Error);
                }
                else
                {
                    File.WriteAllBytes(filePath, request.downloadHandler.data);
                }
            }
            catch(Exception e)
            {
                CrashReporter.LogError(e);
            }
        }
    }
}
