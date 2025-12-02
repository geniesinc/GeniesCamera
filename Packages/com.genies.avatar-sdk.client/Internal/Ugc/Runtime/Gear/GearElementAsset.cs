using Genies.Avatars;

namespace Genies.Ugc
{
    /// <summary>
    /// Contains a Gear element asset.
    /// </summary>
    public sealed class GearElementAsset : IAsset
    {
        public string Id  { get; }
        public string Lod { get; }

        public readonly GearSubElement[]         SubElements;
        public readonly IGenieComponentCreator[] ComponentCreators;

        public GearElementAsset(
            string id,
            string lod,
            GearSubElement[] subElements,
            IGenieComponentCreator[] componentCreators)
        {
            Id = id;
            Lod = lod;
            SubElements = subElements;
            ComponentCreators = componentCreators;
        }
    }
}
