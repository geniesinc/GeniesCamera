namespace Genies.Avatars
{
    public sealed class BlendShapeAsset : IAsset
    {
        public string Id { get; }
        public string Lod { get; }
        public string Slot { get; }
        public DnaEntry[] Dna { get; }
        
        public BlendShapeAsset(string id, string lod, string slot, DnaEntry[] dna)
        {
            Id = id;
            Lod = lod;
            Slot = slot;
            Dna = dna;
        }
    }

    public struct DnaEntry
    {
        public string Name;
        public float Value;
    }
}