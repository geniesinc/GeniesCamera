using System;
using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;
using Genies.Assets.Services;

namespace Genies.Avatars
{
    /// <summary>
    /// Used to control the tattoo configuration from a specific slot index on a given <see cref="MegaSkinGenieMaterial"/> instance.
    /// Any configuration applied will set the given <see cref="MegaSkinGenieMaterial"/> instance dirty. If the material is added to
    /// an <see cref="IEditableGenie"/> instance, you can expect any configuration changes to be applied once the
    /// <see cref="IEditableGenie"/> is rebuilt.
    /// <br/><br/>
    /// Through this controller you can control what tattoo asset is equipped to this slot and what position, rotation and scale the
    /// texture has on the material.
    /// <br/><br/>
    /// You can use a <see cref="TattooController"/> to automatically initialize all the available tattoo slot controllers for
    /// a material and handle the <see cref="IEditableGenie"/> instance for you.
    /// </summary>
    public sealed class TattooSlotController
    {
        public int SlotIndex { get; }
        public bool IsEquipped => AssetId is not null;
        public string AssetId { get; private set; }
        public float PositionX { get => _positionX; set => SetFloatProperty(ref _positionX, value, _tattooPosXId); }
        public float PositionY { get => _positionY; set => SetFloatProperty(ref _positionY, value, _tattooPosYId); }
        public float Rotation  { get => _rotation;  set => SetFloatProperty(ref _rotation, value, _tattooRotationId); }
        public float Scale     { get => _scale;     set => SetFloatProperty(ref _scale, value, _tattooScaleId); }

        public Action Updated;

        // dependencies
        private readonly MegaSkinGenieMaterial _skinMaterial;
        private readonly IAssetLoader<Texture2DAsset> _tattooLoader;

        // state
        private float _positionX;
        private float _positionY;
        private float _rotation;
        private float _scale;

        // shader property IDs
        private readonly int _tattooPosXId;
        private readonly int _tattooPosYId;
        private readonly int _tattooRotationId;
        private readonly int _tattooScaleId;

        public TattooSlotController(int slotIndex, MegaSkinGenieMaterial skinMaterial, IAssetLoader<Texture2DAsset> tattooLoader)
        {
            SlotIndex = slotIndex;
            _skinMaterial = skinMaterial;
            _tattooLoader = tattooLoader;

            // initialize slot property Ids
            _tattooPosXId     = Shader.PropertyToID($"_Tattoo{slotIndex + 1}PosX");
            _tattooPosYId     = Shader.PropertyToID($"_Tattoo{slotIndex + 1}PosY");
            _tattooRotationId = Shader.PropertyToID($"_Tattoo{slotIndex + 1}Rotation");
            _tattooScaleId    = Shader.PropertyToID($"_Tattoo{slotIndex + 1}Scale");

            // update skin material to reflect current state
            _skinMaterial.ClearTattoo(slotIndex);
            _skinMaterial.NonBakedMaterial.SetFloat(_tattooPosXId, _positionX);
            _skinMaterial.NonBakedMaterial.SetFloat(_tattooPosYId, _positionY);
            _skinMaterial.NonBakedMaterial.SetFloat(_tattooRotationId, _rotation);
            _skinMaterial.NonBakedMaterial.SetFloat(_tattooScaleId, _scale);
            _skinMaterial.NotifyUpdate();
        }

        /// <summary>
        /// Loads and equips the given tattoo ID.
        /// </summary>
        public async UniTask LoadAndEquipTattooAsync(string assetId)
        {
            if (assetId == AssetId)
            {
                return;
            }

            Ref<Texture2DAsset> assetRef = await _tattooLoader.LoadAsync(assetId);
            EquipTattoo(assetRef);
        }

        /// <summary>
        /// Equips the given tattoo asset.
        /// </summary>
        public void EquipTattoo(Ref<Texture2DAsset> assetRef)
        {
            if (!assetRef.IsAlive || assetRef.Item?.Id is null || AssetId == assetRef.Item.Id)
            {
                assetRef.Dispose();
                return;
            }

            AssetId = assetRef.Item.Id;
            _skinMaterial.EquipTattoo(SlotIndex, assetRef.Item.Texture);
            _skinMaterial.NotifyUpdate();
            assetRef.Dispose();

            Updated?.Invoke();
        }

        public void ClearTattoo()
        {
            if (!IsEquipped)
            {
                return;
            }

            AssetId = null;
            _skinMaterial.ClearTattoo(SlotIndex);
            _skinMaterial.NotifyUpdate();

            Updated?.Invoke();
        }

        private void SetFloatProperty(ref float field, float value, int propertyId)
        {
            if (value == field || !_skinMaterial.NonBakedMaterial)
            {
                return;
            }

            field = value;

            _skinMaterial.NonBakedMaterial.SetFloat(propertyId, value);
            _skinMaterial.NotifyUpdate();

            Updated?.Invoke();
        }
    }
}
