using System;
using System.Collections.Generic;
using System.Linq;
using Genies.Assets.Services;

namespace Genies.Avatars
{
    /// <summary>
    /// Used to control all the available tattoo slots from a <see cref="MegaSkinGenieMaterial"/> instance.
    /// You can configure each tattoo by fetching a <see cref="TattooSlotController"/> from the
    /// <see cref="SlotControllers"/> list. Any changes performed on the tattoo slots will set the skin
    /// material dirty.
    /// <br/><br/>
    /// If you want to have a set of predefined slots with a fixed tattoo transformation for each slot,
    /// then use the <see cref="TattooPresetController"/> instead.
    /// </summary>
    public sealed class TattooController : ITattooController, IDisposable
    {
        public IReadOnlyList<TattooSlotController> SlotControllers { get; }

        public event Action Updated;

        // state
        private readonly List<TattooSlotController> _slotControllers;

        public TattooController(MegaSkinGenieMaterial skinMaterial, IAssetLoader<Texture2DAsset> tattooLoader)
        {
            _slotControllers = Enumerable.Range(0, skinMaterial.TattooSlotCount).Select(index => new TattooSlotController(index, skinMaterial, tattooLoader)).ToList();
            SlotControllers = _slotControllers.AsReadOnly();

            foreach (TattooSlotController slotController in _slotControllers)
            {
                slotController.Updated += OnTattooSlotUpdated;
            }
        }

        public void Dispose()
        {
            Updated = null;

            foreach (TattooSlotController slotController in _slotControllers)
            {
                slotController.Updated -= OnTattooSlotUpdated;
                slotController.ClearTattoo();
            }

            _slotControllers.Clear();
        }

        private void OnTattooSlotUpdated()
        {
            Updated?.Invoke();
        }
    }
}
