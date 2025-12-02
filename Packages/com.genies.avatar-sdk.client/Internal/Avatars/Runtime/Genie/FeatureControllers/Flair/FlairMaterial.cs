using System;
using System.Collections;
using System.Collections.Generic;
using Genies.Shaders;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Avatars
{
    public class FlairMaterial : IGenieMaterial, IDisposable
    {
        public string SlotId { get; }
        public Material Material { get; }
        public event System.Action Updated;

        public FlairMaterial(string slotId)
        {
            SlotId = slotId;
            Material = GeniesShaders.MegaFlair.NewMaterial();
        }

        public void OnApplyingMaterial(Material previousMaterial)
        {
            return;
        }

        public void NotifyUpdate()
        {
            Updated?.Invoke();
        }

        public void Dispose()
        {
            Object.Destroy(Material);
        }
    }
}
