using System;
using System.Collections.Generic;
using System.Linq;
using UMA;

namespace Genies.Avatars
{
    /// <summary>
    /// Contains all the resources for setting a genie species to an UMA avatar.
    /// </summary>
    public sealed class SpeciesAsset : IAsset, IDisposable
    {
        public string Id { get; }
        public string GenieType { get; }
        public string Lod { get; }
        public bool IsDisposed { get; private set; }

        public readonly RaceData Race;
        public readonly DynamicUMADnaAsset Dna;
        public readonly SlotDataAsset[] Slots;
        public readonly OverlayDataAsset[] Overlays;
        public readonly IGenieComponentCreator[] ComponentCreators;
        public readonly Dictionary<string, string> MappedUmaIdentifiers;

        private readonly IDisposable _dependencies;
        private readonly IndexedAssets _indexedAssets;

        public SpeciesAsset(string id, string genieType, string lod, RaceData race, DynamicUMADnaAsset dna, IEnumerable<SlotDataAsset> slots,
            IEnumerable<OverlayDataAsset> overlays, IEnumerable<IGenieComponentCreator> componentCreators,
            IDictionary<string, string> mappedUmaIdentifiers, IDisposable dependencies)
        {
            GenieType = genieType;
            Id = id;
            Lod = lod;
            Race = race;
            Dna = dna;
            Slots = slots.ToArray();
            Overlays = overlays.ToArray();
            ComponentCreators = componentCreators.ToArray();
            MappedUmaIdentifiers = new Dictionary<string, string>(mappedUmaIdentifiers);

            _dependencies = dependencies;
            _indexedAssets = new IndexedAssets();

            IndexAssets();
        }

        private void IndexAssets()
        {
            if (GenieType == GenieTypeName.NonUma)
            {
                return;
            }

            _indexedAssets.Index(Race);
            _indexedAssets.Index(Dna);
            _indexedAssets.Index(Race.TPose);
            _indexedAssets.Index(Race.baseRaceRecipe);
            _indexedAssets.Index(Slots);
            _indexedAssets.Index(Overlays);
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            _indexedAssets.ReleaseAll();
            _dependencies?.Dispose();
            IsDisposed = true;
        }
    }
}
