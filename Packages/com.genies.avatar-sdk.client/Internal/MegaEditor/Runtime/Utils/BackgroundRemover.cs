using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Genies.Customization.MegaEditor
{
    public class BackgroundRemover
    {
        private readonly float _contrastBoost = 1.3f;
        private readonly int _maxProcessingTime = 16;

        private Color[] _pixelBuffer;
        private Color[] _resultBuffer;
        private bool[] _edgeBuffer;
        private bool[] _backgroundBuffer;
        private bool[] _visitedBuffer;
        private Queue<int> _floodQueue;
        private float[] _edgeStrength;

        public BackgroundRemover()
        {
            _floodQueue = new Queue<int>(1024);
        }

        public void ProcessTextureAsync(
            MonoBehaviour caller,
            Texture2D inputTexture,
            System.Action<Texture2D> onComplete)
        {
            if (inputTexture == null || caller == null)
            {
                onComplete?.Invoke(null);
                return;
            }

            caller.StartCoroutine(ProcessTextureCoroutine(inputTexture, onComplete));
        }

        private IEnumerator ProcessTextureCoroutine(Texture2D inputTexture, System.Action<Texture2D> onComplete)
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Texture2D readableTexture = MakeTextureReadable(inputTexture);
            if (stopwatch.ElapsedMilliseconds > _maxProcessingTime)
            {
                yield return null;
                stopwatch.Restart();
            }

            Texture2D result = null;
            yield return RemoveGradientBackgroundCoroutine(readableTexture, (tex) => result = tex);
            if (readableTexture != inputTexture)
            {
                Object.DestroyImmediate(readableTexture);
            }

            onComplete?.Invoke(result);
        }

        private Texture2D MakeTextureReadable(Texture2D source)
        {
            try
            {
                source.GetPixels();
                return source;
            }
            catch
            {
                RenderTexture tmp =
                    RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32);
                Graphics.Blit(source, tmp);
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = tmp;
                Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
                readable.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                readable.Apply();
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(tmp);
                return readable;
            }
        }

        private IEnumerator RemoveGradientBackgroundCoroutine(Texture2D original, System.Action<Texture2D> onComplete)
        {
            int pixelCount = original.width * original.height;
            InitializeBuffers(pixelCount);

            Texture2D result = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false);
            _pixelBuffer = original.GetPixels();
            EnhanceContrastPreserveQuality(_pixelBuffer, _contrastBoost);

            yield return DetectEdgesWithSobelCoroutine(_pixelBuffer, _edgeBuffer, _edgeStrength, original.width,
                original.height);
            yield return FloodFillBackgroundImprovedCoroutine(_pixelBuffer, _edgeBuffer, _backgroundBuffer,
                original.width, original.height);
            ApplySoftAlphaByDistance(_pixelBuffer, _backgroundBuffer, _resultBuffer, original.width, original.height);
            result.SetPixels(_resultBuffer);
            result.Apply();
            onComplete?.Invoke(result);
        }

        private void InitializeBuffers(int pixelCount)
        {
            if (_resultBuffer == null || _resultBuffer.Length != pixelCount)
            {
                _resultBuffer = new Color[pixelCount];
                _edgeBuffer = new bool[pixelCount];
                _backgroundBuffer = new bool[pixelCount];
                _visitedBuffer = new bool[pixelCount];
                _edgeStrength = new float[pixelCount];
            }
            else
            {
                System.Array.Clear(_edgeBuffer, 0, pixelCount);
                System.Array.Clear(_backgroundBuffer, 0, pixelCount);
                System.Array.Clear(_visitedBuffer, 0, pixelCount);
                System.Array.Clear(_edgeStrength, 0, pixelCount);
            }
        }

        private void EnhanceContrastPreserveQuality(Color[] pixels, float contrast)
        {
            for (int i = 0; i < pixels.Length; i++)
            {
                ref Color pixel = ref pixels[i];
                Color.RGBToHSV(pixel, out float h, out float s, out float v);
                v = Mathf.Clamp01((v - 0.5f) * contrast + 0.5f);
                s = Mathf.Clamp01(s * 1.1f);
                pixel = Color.HSVToRGB(h, s, v);
                pixel.a = pixels[i].a;
            }
        }

        private IEnumerator DetectEdgesWithSobelCoroutine(
            Color[] pixels,
            bool[] edges,
            float[] edgeStrength,
            int width,
            int height)
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int index = y * width + x;
                    float gx = 0, gy = 0;

                    gx += pixels[(y - 1) * width + (x - 1)].grayscale * -1;
                    gx += pixels[(y - 1) * width + (x + 1)].grayscale * 1;
                    gx += pixels[y * width + (x - 1)].grayscale * -2;
                    gx += pixels[y * width + (x + 1)].grayscale * 2;
                    gx += pixels[(y + 1) * width + (x - 1)].grayscale * -1;
                    gx += pixels[(y + 1) * width + (x + 1)].grayscale * 1;

                    gy += pixels[(y - 1) * width + (x - 1)].grayscale * -1;
                    gy += pixels[(y - 1) * width + x].grayscale * -2;
                    gy += pixels[(y - 1) * width + (x + 1)].grayscale * -1;
                    gy += pixels[(y + 1) * width + (x - 1)].grayscale * 1;
                    gy += pixels[(y + 1) * width + x].grayscale * 2;
                    gy += pixels[(y + 1) * width + (x + 1)].grayscale * 1;

                    float magnitude = Mathf.Sqrt(gx * gx + gy * gy);
                    float dynThreshold = Mathf.Lerp(0.05f, 0.15f, pixels[index].grayscale);
                    edgeStrength[index] = magnitude;
                    edges[index] = magnitude > dynThreshold;
                }

                if (stopwatch.ElapsedMilliseconds > _maxProcessingTime)
                {
                    yield return null;
                    stopwatch.Restart();
                }
            }
        }

        private IEnumerator FloodFillBackgroundImprovedCoroutine(
            Color[] pixels,
            bool[] edges,
            bool[] background,
            int width,
            int height)
        {
            System.Diagnostics.Stopwatch stopwatch = System.Diagnostics.Stopwatch.StartNew();
            _floodQueue.Clear();
            for (int x = 0; x < width; x++)
            {
                _floodQueue.Enqueue(x);
                _floodQueue.Enqueue((height - 1) * width + x);
            }

            for (int y = 1; y < height - 1; y++)
            {
                _floodQueue.Enqueue(y * width);
                _floodQueue.Enqueue(y * width + (width - 1));
            }

            int processedCount = 0;
            while (_floodQueue.Count > 0)
            {
                int index = _floodQueue.Dequeue();
                if (_visitedBuffer[index] || edges[index])
                {
                    continue;
                }

                _visitedBuffer[index] = true;
                background[index] = true;

                int x = index % width;
                int y = index / width;

                CheckAndAddNeighborImproved(x - 1, y, pixels, index, width, height);
                CheckAndAddNeighborImproved(x + 1, y, pixels, index, width, height);
                CheckAndAddNeighborImproved(x, y - 1, pixels, index, width, height);
                CheckAndAddNeighborImproved(x, y + 1, pixels, index, width, height);

                processedCount++;
                if (processedCount > 1000 && stopwatch.ElapsedMilliseconds > _maxProcessingTime)
                {
                    yield return null;
                    stopwatch.Restart();
                    processedCount = 0;
                }
            }
        }

        private void CheckAndAddNeighborImproved(
            int nx,
            int ny,
            Color[] pixels,
            int currentIndex,
            int width,
            int height)
        {
            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
            {
                int neighborIndex = ny * width + nx;
                if (!_visitedBuffer[neighborIndex])
                {
                    Color current = pixels[currentIndex];
                    Color neighbor = pixels[neighborIndex];
                    float dr = (current.r - neighbor.r) * 0.3f;
                    float dg = (current.g - neighbor.g) * 0.59f;
                    float db = (current.b - neighbor.b) * 0.11f;
                    float colorDiff = Mathf.Sqrt(dr * dr + dg * dg + db * db);
                    float brightnessDiff = Mathf.Abs(current.grayscale - neighbor.grayscale);

                    if (colorDiff < 0.15f && brightnessDiff < 0.1f)
                    {
                        _floodQueue.Enqueue(neighborIndex);
                    }
                }
            }
        }

        private void ApplySoftAlphaByDistance(
            Color[] pixels,
            bool[] backgroundMask,
            Color[] result,
            int width,
            int height)
        {
            int pixelCount = width * height;

            // Precompute distance to background for soft edges
            int[] distanceMap = new int[pixelCount];
            for (int i = 0; i < pixelCount; i++)
            {
                distanceMap[i] = backgroundMask[i] ? 0 : int.MaxValue;
            }

            // Simple 2-pass distance transform (city block for perf)
            for (int y = 1; y < height; y++)
            {
                for (int x = 1; x < width; x++)
                {
                    int i = y * width + x;
                    int minDist = distanceMap[i];
                    minDist = Mathf.Min(minDist, distanceMap[i - 1] + 1);
                    minDist = Mathf.Min(minDist, distanceMap[i - width] + 1);
                    distanceMap[i] = minDist;
                }
            }

            for (int y = height - 2; y >= 0; y--)
            {
                for (int x = width - 2; x >= 0; x--)
                {
                    int i = y * width + x;
                    int minDist = distanceMap[i];
                    minDist = Mathf.Min(minDist, distanceMap[i + 1] + 1);
                    minDist = Mathf.Min(minDist, distanceMap[i + width] + 1);
                    distanceMap[i] = minDist;
                }
            }

            float maxSoftEdge = 5f;

            for (int i = 0; i < pixelCount; i++)
            {
                if (backgroundMask[i])
                {
                    result[i] = Color.clear;
                }
                else
                {
                    float alpha = 1.0f;
                    float dist = distanceMap[i];

                    if (dist < maxSoftEdge)
                    {
                        alpha = Mathf.SmoothStep(0.0f, 1.0f, dist / maxSoftEdge);
                    }

                    result[i] = pixels[i];
                    result[i].a = alpha;
                }
            }

            // ---------- Smarter Shadow Cleanup ----------
            int minY = height, maxY = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    if (!backgroundMask[i])
                    {
                        minY = Mathf.Min(minY, y);
                        maxY = Mathf.Max(maxY, y);
                    }
                }
            }

            // After calculating minY / maxY
            int shadowStart = maxY + 2;
            int shadowEnd = Mathf.Min(height - 1, maxY + (int)(height * 0.2f));

            for (int y = shadowStart; y <= shadowEnd; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int i = y * width + x;
                    if (backgroundMask[i])
                    {
                        continue;
                    }

                    float brightness = pixels[i].grayscale;
                    float edgeSoftness = _edgeStrength != null ? _edgeStrength[i] : 0f;

                    // Heuristic: shadows are soft-edged, darker, not at image borders
                    bool likelyShadow = brightness < 0.85f && edgeSoftness < 0.05f;

                    if (likelyShadow)
                    {
                        float verticalFactor = (float)(y - shadowStart) / (shadowEnd - shadowStart);
                        float fade = Mathf.SmoothStep(1.0f, 0.0f, verticalFactor);
                        result[i].a *= fade;
                    }
                }
            }


        }
    }
}

