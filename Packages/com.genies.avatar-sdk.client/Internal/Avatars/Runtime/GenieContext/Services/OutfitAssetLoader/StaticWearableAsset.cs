using UMA;
using UMA.CharacterSystem;

namespace Genies.Avatars
{
    public sealed class StaticWearableAsset : IAsset
    {
        public const string OutfitAssetType = "static";

        public string Id { get; }
        public string Lod { get; }
        public string Subcategory { get; }
        public OutfitCollisionData CollisionData { get; }
        public UMAWardrobeRecipe Recipe { get; }
        public SlotDataAsset[] Slots { get; }
        public OverlayDataAsset[] Overlays { get; }
        public IGenieComponentCreator[] ComponentCreators { get; }

        public string Slot => Recipe.wardrobeSlot;

        public StaticWearableAsset(
            string id,
            string lod,
            string subcategory,
            OutfitCollisionData collisionData,
            UMAWardrobeRecipe recipe,
            SlotDataAsset[] slots,
            OverlayDataAsset[] overlays,
            IGenieComponentCreator[] componentCreators)
        {
            Id = id;
            Lod = lod;
            Subcategory = subcategory;
            CollisionData = collisionData;
            Recipe = recipe;
            Slots = slots;
            Overlays = overlays;
            ComponentCreators = componentCreators;
        }
    }
}
