using System;
using System.Collections.Generic;
using Genies.Avatars;
using UMA;
using UnityEngine;

namespace Genies.Ugc
{
    /// <summary>
    /// Contains all the data and allocated resources from an element required to build an <see cref="OutfitAsset"/>.
    /// It must be disposed when no longer used so the resources are released.
    /// </summary>
    public sealed class OutfitAssetElement : IDisposable
    {
        public string Id { get; }
        public readonly SlotDataAsset[] Slots;
        public readonly OverlayDataAsset[] Overlays;
        public readonly SlotData[] SlotData;
        public readonly IGenieComponentCreator[] ComponentCreators;
        public readonly MegaMaterial MegaMaterial;
        public readonly string MaterialSlot;
        public readonly int RegionCount;
        public readonly Bounds Bounds;
        public readonly List<Vector3> Vertices;

        private readonly IDisposable _dependencies;

        public OutfitAssetElement(string id, SlotDataAsset[] slots, OverlayDataAsset[] overlays, SlotData[] slotData,
            IGenieComponentCreator[] componentCreators, MegaMaterial megaMaterial, string materialSlot, int regionCount,
            Bounds bounds, List<Vector3> vertices, IDisposable dependencies = null)
        {
            Id = id;
            Slots = slots;
            Overlays = overlays;
            SlotData = slotData;
            ComponentCreators = componentCreators;
            MegaMaterial = megaMaterial;
            MaterialSlot = materialSlot;
            RegionCount = regionCount;
            Bounds = bounds;
            Vertices = vertices;

            _dependencies = dependencies;
        }

        public void Dispose()
        {
            _dependencies?.Dispose();
        }
    }
}
