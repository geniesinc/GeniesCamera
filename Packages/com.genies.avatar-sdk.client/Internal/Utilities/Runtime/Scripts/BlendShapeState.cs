namespace Genies.Utilities
{
    public struct BlendShapeState
    {
        public int Index;
        public float Weight;

        public BlendShapeState(int index, float weight)
        {
            Index = index;
            Weight = weight;
        }
    }
}
