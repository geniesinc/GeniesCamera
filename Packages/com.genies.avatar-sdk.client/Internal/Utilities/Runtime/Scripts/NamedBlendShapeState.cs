namespace Genies.Utilities
{
    public struct NamedBlendShapeState
    {
        public string Name;
        public float Weight;

        public NamedBlendShapeState(string name, float weight)
        {
            Name = name;
            Weight = weight;
        }
    }
}
