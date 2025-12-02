namespace Genies.Avatars
{
    public struct OutfitCollisionData
    {
        public int Layer;
        public OutfitCollisionType Type;
        public OutfitCollisionMode Mode;
        public OutfitHatHairMode HatHairMode;
    }

    public enum OutfitCollisionType
    {
        Open,
        Closed,
    }

    public enum OutfitCollisionMode
    {
        None,
        Blend,
        Simulated,
    }

    public enum OutfitHatHairMode
    {
        None,
        Blendshape,
        Fallback,
    }
}
