
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Genies
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal static class AvatarPngCapture
#else
    public static class AvatarPngCapture
#endif
    {
        /// <summary>
        /// Captures a PNG of the given avatar GameObject.
        /// Returns the encoded PNG bytes; optionally writes to savePath if provided.
        /// </summary>
        /// <param name="avatarRoot">Root GameObject of the avatar.</param>
        /// <param name="width">Output width in pixels.</param>
        /// <param name="height">Output height in pixels.</param>
        /// <param name="transparentBackground">If true, background alpha is 0.</param>
        /// <param name="msaa">MSAA level for the RenderTexture (1,2,4,8).</param>
        /// <param name="orthographic">If true, uses ortho camera (good for portraits/icons).</param>
        /// <param name="savePath">If non-empty, writes PNG to disk and creates directories as needed.</param>
        /// <returns>PNG bytes (use for uploads). Also written to file if savePath provided.</returns>
        public static byte[] CapturePNG(
            GameObject avatarRoot,
            int width,
            int height,
            string savePath = null,
            bool transparentBackground = true,
            int msaa = 4,
            bool orthographic = false
        )
        {
            if (avatarRoot == null)
            {
                throw new ArgumentNullException(nameof(avatarRoot));
            }

            // 1) Gather renderers and compute bounds
            var renderers = avatarRoot.GetComponentsInChildren<Renderer>(includeInactive: true);
            if (renderers == null || renderers.Length == 0)
            {
                throw new InvalidOperationException("Avatar has no renderers to capture.");
            }

            var bounds = new Bounds(renderers[0].bounds.center, Vector3.zero);
            foreach (var r in renderers)
            {
                // Skip non-visible renderers if desired
                if (r.enabled && r.gameObject.activeInHierarchy)
                {
                    bounds.Encapsulate(r.bounds);
                }
            }

            // 2) Create a dedicated camera + light
            var goCam = new GameObject("AvatarCaptureCamera", typeof(Camera));
            var cam = goCam.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor =
                transparentBackground ? new Color(0, 0, 0, 0) : Color.clear; // alpha respected by ARGB RT
            cam.allowHDR = false;
            cam.allowMSAA = msaa > 1;
            cam.orthographic = orthographic;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 1000f;
            cam.fieldOfView = 25f; // tight FoV for portraits

            // Optional: a simple neutral key light
            var goLight = new GameObject("AvatarCaptureLight", typeof(Light));
            var light = goLight.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(35f, 135f, 0f);

            // 3) Isolate the avatar using a temp layer
            int captureLayer = LayerMask.NameToLayer("AvatarCapture");
            if (captureLayer < 0)
            {
                // If the layer doesn't exist in Project Settings, we fall back to default (not fatal).
                // You can add the layer "AvatarCapture" in Tags & Layers for perfect isolation.
                captureLayer = 0;
            }

            var originalLayers = new Dictionary<Transform, int>(64);
            foreach (var t in avatarRoot.GetComponentsInChildren<Transform>(true))
            {
                originalLayers[t] = t.gameObject.layer;
                t.gameObject.layer = captureLayer;
            }

            cam.cullingMask = (1 << captureLayer);

            // 4) Position camera to frame bounds
            var center = bounds.center;
            var extents = bounds.extents;
            float radius = extents.magnitude; // rough sphere radius
            float aspect = (float)width / Mathf.Max(1, height);

            if (orthographic)
            {
                float sizeY = Mathf.Max(extents.y, extents.x / aspect);
                cam.orthographicSize = sizeY * 1.15f; // margin
                cam.transform.position = center + new Vector3(0, 0.15f * radius, radius * 3f);
            }
            else
            {
                // distance so the bounding sphere fits in FOV
                float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
                float dist = (radius / Mathf.Sin(fovRad * 0.5f)) * 1.05f; // small margin
                cam.transform.position = center + new Vector3(0, 0.15f * radius, dist);
            }

            cam.transform.LookAt(center);

            // Align light to camera for flattering frontal light
            light.transform.rotation = cam.transform.rotation * Quaternion.Euler(20f, -35f, 0f);

            // 5) Render to RT
            var desc = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24)
            {
                msaaSamples = Mathf.Clamp(msaa, 1, 8),
                useMipMap = false,
                autoGenerateMips = false,
                sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear)
            };

            var rt = new RenderTexture(desc);
            rt.Create();

            var prevActive = RenderTexture.active;
            var prevTarget = cam.targetTexture;

            try
            {
                cam.targetTexture = rt;

                // If you need post effects, ensure your pipeline allows alpha output (URP/HDRP setting).
#if UNITY_2018_2_OR_NEWER
                cam.Render();
#else
            cam.Render();
#endif
                RenderTexture.active = rt;

                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply(false, false);

                byte[] png = tex.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(tex);

                if (!string.IsNullOrEmpty(savePath))
                {
                    var dir = Path.GetDirectoryName(savePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    File.WriteAllBytes(savePath, png);
                }

                return png;
            }
            finally
            {
                // Cleanup & restore
                cam.targetTexture = prevTarget;
                RenderTexture.active = prevActive;

                if (rt != null)
                {
                    rt.Release();
                    UnityEngine.Object.DestroyImmediate(rt);
                }

                UnityEngine.Object.DestroyImmediate(goCam);
                UnityEngine.Object.DestroyImmediate(goLight);

                foreach (var kvp in originalLayers)
                {
                    kvp.Key.gameObject.layer = kvp.Value;
                }
            }
        }

        public static byte[] CaptureHeadshotPNG(
            GameObject avatarRoot,
            Transform headAnchor,
            int width,
            int height,
            string savePath, // write to file if non-null/non-empty
            bool transparentBackground = true,
            int msaa = 4,
            float fieldOfView = 25f,
            float headRadiusMeters = 0.25f, // approx radius around head
            float forwardDistance = 1.0f, // camera distance from head center
            Vector3 cameraUpOffset = default // small vertical offset for flattering angle
        )
        {
            if (avatarRoot == null)
            {
                throw new ArgumentNullException(nameof(avatarRoot));
            }

            if (headAnchor == null)
            {
                throw new ArgumentNullException(nameof(headAnchor));
            }

            if (cameraUpOffset == default)
            {
                cameraUpOffset = new Vector3(0f, 0.05f, 0f);
            }

            // --- Camera & light ---
            var goCam = new GameObject("AvatarHeadshotCamera", typeof(Camera));
            var cam = goCam.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = transparentBackground ? new Color(0, 0, 0, 0) : Color.black;
            cam.allowHDR = false;
            cam.allowMSAA = msaa > 1;
            cam.orthographic = false;
            cam.nearClipPlane = 0.01f;
            cam.farClipPlane = 1000f;
            cam.fieldOfView = fieldOfView;

            var goLight = new GameObject("AvatarHeadshotLight", typeof(Light));
            var light = goLight.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;

            // --- Optional isolation on a dedicated layer (if present) ---
            int captureLayer = LayerMask.NameToLayer("AvatarCapture");
            if (captureLayer < 0)
            {
                captureLayer = 0; // fallback: Default
            }

            var originalLayers = new Dictionary<Transform, int>(64);
            foreach (var t in avatarRoot.GetComponentsInChildren<Transform>(true))
            {
                originalLayers[t] = t.gameObject.layer;
                t.gameObject.layer = captureLayer;
            }

            cam.cullingMask = (1 << captureLayer);

            // --- Frame the head ---
            var center = headAnchor.position;
            // Place camera in front of head (along head forward), slightly above for a nice look
            cam.transform.position = center + (headAnchor.forward * forwardDistance) + cameraUpOffset;
            cam.transform.LookAt(center);
            light.transform.rotation = cam.transform.rotation * Quaternion.Euler(20f, -35f, 0f);

            // Compute a distance so the head radius fits in FOV with a small margin
            float margin = 1.10f;
            float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
            float neededDist = (headRadiusMeters / Mathf.Sin(fovRad * 0.5f)) * margin;
            cam.transform.position = center + (cam.transform.position - center).normalized * neededDist;

            // --- Render to RT ---
            RenderTexture rt;
#if UNITY_2019_1_OR_NEWER
            var desc = new RenderTextureDescriptor(width, height, RenderTextureFormat.ARGB32, 24)
            {
                msaaSamples = Mathf.Clamp(msaa, 1, 8),
                useMipMap = false,
                autoGenerateMips = false,
                // Unity will pick correct sRGB/linear handling; setting sRGB via descriptor is okay if available:
                sRGB = (QualitySettings.activeColorSpace == ColorSpace.Linear)
            };
            rt = new RenderTexture(desc);
#else
    rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
    {
        antiAliasing = Mathf.Clamp(msaa, 1, 8),
        useMipMap = false,
        autoGenerateMips = false
    };
#endif
            rt.Create();

            var prevActive = RenderTexture.active;
            var prevTarget = cam.targetTexture;

            try
            {
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply(false, false);

                byte[] png = tex.EncodeToPNG();
                UnityEngine.Object.DestroyImmediate(tex);

                if (!string.IsNullOrEmpty(savePath))
                {
                    var dir = System.IO.Path.GetDirectoryName(savePath);
                    if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                    {
                        System.IO.Directory.CreateDirectory(dir);
                    }

                    System.IO.File.WriteAllBytes(savePath, png);
                }

                return png;
            }
            finally
            {
                cam.targetTexture = prevTarget;
                RenderTexture.active = prevActive;

                if (rt != null)
                {
                    rt.Release();
                    UnityEngine.Object.DestroyImmediate(rt);
                }

                UnityEngine.Object.DestroyImmediate(goCam);
                UnityEngine.Object.DestroyImmediate(goLight);

                foreach (var kv in originalLayers)
                {
                    kv.Key.gameObject.layer = kv.Value;
                }
            }
        }
    }
}
