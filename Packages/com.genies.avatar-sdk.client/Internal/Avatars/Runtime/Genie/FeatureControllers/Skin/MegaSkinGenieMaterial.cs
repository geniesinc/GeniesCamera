using System;
using Genies.Assets.Services;
using Genies.Shaders;
using Genies.Refs;
using Genies.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Avatars
{
    /// <summary>
    /// Genie material implementation for the MegaSkin shader that can be updated externally.
    /// </summary>
    public sealed class MegaSkinGenieMaterial : IGenieMaterial, IDisposable
    {
        private static readonly int _tattoosTexturePropertyId = Shader.PropertyToID("_Tattoos");
        private const string _megaSkinTattooSettingsPath = "DefaultMegaSkinTattooSettings";

        public string SlotId { get; }
        public string Lod { get; }
        public int TattooSlotCount { get; }
        public bool IsBaked { get; private set; }
        public Material Material => IsBaked && _bakedMaterialRef.IsAlive ? _bakedMaterialRef.Item : NonBakedMaterial;
        public Material NonBakedMaterial { get; private set; }

        public event Action Updated;

        // state
        private readonly RenderTexture _tattoosTexture;
        private readonly RenderTexture _clearTexture;
        private readonly Material _blitMaterial;
        private readonly MaterialBaker _baker;
        private Ref<Material> _bakedMaterialRef;

        public MegaSkinGenieMaterial(string slot, string lod, MegaSkinTattooSettings tattooSettings = null)
        {
            if (!tattooSettings)
            {
                tattooSettings = Resources.Load<MegaSkinTattooSettings>(_megaSkinTattooSettingsPath);
            }

            if (!tattooSettings)
            {
                Debug.LogError($"[{nameof(MegaSkinGenieMaterial)}] no tattoo settings were provided and couldn't load default ones from path: {_megaSkinTattooSettingsPath}");
                return;
            }

            NonBakedMaterial = GeniesShaders.MegaSkin.NewMaterial();
            SlotId = slot;
            Lod = lod;
            TattooSlotCount = tattooSettings.slots;

            // create a render texture array that will contain all tattoo slots
            _tattoosTexture = tattooSettings.CreateTattoosRenderTexture();
            _tattoosTexture.WriteColor(Color.clear);

            // set the texture to the tattoos slot on the material
            NonBakedMaterial.SetTexture(_tattoosTexturePropertyId, _tattoosTexture);

            // create a clear texture
            TextureConfig config = TextureConfig.Default;
            config.Width = config.Height = 16;
            config.FilterMode = FilterMode.Point;
            _clearTexture = config.CreateRenderTexture();
            _clearTexture.WriteColor(Color.clear);

            _blitMaterial = new Material(Shader.Find("Hidden/BlitCopy"));
            
            // get the proper MaterialBaker for the current LOD
            _baker = lod switch
            {
                AssetLod.Low => GeniesShaders.LqMegaSimpleBaker,
                AssetLod.Medium => GeniesShaders.MqMegaSimpleBaker,
                AssetLod.High => GeniesShaders.HqMegaSimpleBaker,
                _ => GeniesShaders.MqMegaSimpleBaker,
            };
        }

        public void OnApplyingMaterial(Material previousMaterial) { }

        /// <summary>
        /// Equips the given texture as a tattoo in the given slot. Once the tattoo is equipped the texture can be destroyed.
        /// </summary>
        public void EquipTattoo(int slotIndex, Texture tattoo)
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
            NotifyUpdate();
        }

        public void ClearTattoo(int slotIndex)
        {
            if (!_tattoosTexture)
            {
                return;
            }

            BlitIntoTattoosTexture(_clearTexture, slotIndex);
            NotifyUpdate();
        }

        public void NotifyUpdate()
        {
            // if it was baked, then we dispose the baked material and return to the non baked instance[
            IsBaked = false;
            _bakedMaterialRef.Dispose();
            Updated?.Invoke();
        }

        public void Bake()
        {
            // if we have no baking config or no changes where applied and we are currently baked, do nothing
            if (IsBaked)
            {
                return;
            }

            // try to bake the material
            _bakedMaterialRef = _baker.Bake(NonBakedMaterial);
            if (!_bakedMaterialRef.IsAlive)
            {
                return;
            }

            IsBaked = true;
            Updated?.Invoke();
        }

        public void Dispose()
        {
            if (NonBakedMaterial)
            {
                Object.Destroy(NonBakedMaterial);
            }

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

            _bakedMaterialRef.Dispose();
            NonBakedMaterial = null;
            IsBaked = false;
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
