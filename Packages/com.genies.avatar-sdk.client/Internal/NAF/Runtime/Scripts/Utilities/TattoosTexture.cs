using System;
using Genies.Avatars;
using Genies.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Naf
{
    public sealed class TattoosTexture : IDisposable
    {
        private static readonly int TattoosTexturePropertyId = Shader.PropertyToID("_Tattoos");
        private const string MegaSkinTattooSettingsPath = "DefaultMegaSkinTattooSettings";

        public int TattooSlotCount { get; }

        // state
        private readonly RenderTexture _tattoosTexture;
        private readonly RenderTexture _clearTexture;
        private readonly Material _blitMaterial;

        public TattoosTexture(MegaSkinTattooSettings tattooSettings = null)
        {
            if (!tattooSettings)
            {
                tattooSettings = Resources.Load<MegaSkinTattooSettings>(MegaSkinTattooSettingsPath);
            }

            if (!tattooSettings)
            {
                Debug.LogError($"[{nameof(TattoosTexture)}] no tattoo settings were provided and couldn't load default ones from path: {MegaSkinTattooSettingsPath}");
                return;
            }

            TattooSlotCount = tattooSettings.slots;

            // create a render texture array that will contain all tattoo slots
            _tattoosTexture = tattooSettings.CreateTattoosRenderTexture();
            _tattoosTexture.WriteColor(Color.clear);

            // create a clear texture
            TextureConfig config = TextureConfig.Default;
            config.Width = config.Height = 16;
            config.FilterMode = FilterMode.Point;
            _clearTexture = config.CreateRenderTexture();
            _clearTexture.WriteColor(Color.clear);

            _blitMaterial = new Material(Shader.Find("Hidden/BlitCopy"));
        }

        public void ApplyTo(Material material)
        {
            material.SetTexture(TattoosTexturePropertyId, _tattoosTexture);
        }

        public void SetTattoo(int slotIndex, Texture tattoo)
        {
            if (!tattoo)
            {
                ClearTattoo(slotIndex);
                return;
            }

            if (!_tattoosTexture)
            {
                return;
            }

            BlitIntoTattoosTexture(tattoo, slotIndex);
        }

        public void ClearTattoo(int slotIndex)
        {
            if (!_tattoosTexture)
            {
                return;
            }

            BlitIntoTattoosTexture(_clearTexture, slotIndex);
        }

        public void ClearAllTattoos()
        {
            for (int i = 0; i < TattooSlotCount; ++i)
            {
                ClearTattoo(i);
            }
        }

        public void Dispose()
        {
            if (_tattoosTexture)
            {
                Object.Destroy(_tattoosTexture);
            }

            if (_clearTexture)
            {
                Object.Destroy(_clearTexture);
            }

            if (_blitMaterial)
            {
                Object.Destroy(_blitMaterial);
            }
        }

        private void BlitIntoTattoosTexture(Texture source, int slotIndex)
        {
            // Unity has a bug with the Graphics.Blit method where it does not work only on iOS so we have to do the blit manually using the GL API
            Graphics.SetRenderTarget(_tattoosTexture, 0, CubemapFace.Unknown, slotIndex);
            _blitMaterial.mainTexture = source;
            _blitMaterial.SetPass(0);
            
            GL.PushMatrix();
            GL.LoadOrtho();

            GL.Begin(GL.QUADS);
            GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(0.0f, 0.0f, 0.0f);
            GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(0.0f, 1.0f, 0.0f);
            GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(1.0f, 1.0f, 0.0f);
            GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(1.0f, 0.0f, 0.0f);
            GL.End();
            
            GL.PopMatrix();
        }
    }
}
