using Genies.Avatars;
using Genies.Models;

namespace Genies.Avatars.Context
{
    public static class CollisionDataExtensions
    {
        public static OutfitCollisionData ToOutfitCollisionData(this CollisionData collisionData)
        {
            if (collisionData is null)
            {
                return default;
            }

            return new OutfitCollisionData
            {
                Layer = collisionData.Layer,
                Type = collisionData.Type.ToOutfitCollisionType(),
                Mode = collisionData.Mode.ToOutfitCollisionMode(),
                HatHairMode = collisionData.HatMode.ToOutfitHatHairMode(),
            };
        }

        public static OutfitCollisionType ToOutfitCollisionType(this CollisionType collisionType)
        {
            return collisionType switch
            {
                CollisionType.open => OutfitCollisionType.Open,
                CollisionType.closed => OutfitCollisionType.Closed,
                _ => OutfitCollisionType.Closed,
            };
        }

        public static OutfitCollisionMode ToOutfitCollisionMode(this CollisionMode collisionMode)
        {
            return collisionMode switch
            {
                CollisionMode.none => OutfitCollisionMode.None,
                CollisionMode.blend => OutfitCollisionMode.Blend,
                CollisionMode.simulated => OutfitCollisionMode.Simulated,
                _ => OutfitCollisionMode.None,
            };
        }

        public static OutfitHatHairMode ToOutfitHatHairMode(this HatHairBehavior collisionMode)
        {
            return collisionMode switch
            {
                HatHairBehavior.none => OutfitHatHairMode.None,
                HatHairBehavior.blendshape => OutfitHatHairMode.Blendshape,
                HatHairBehavior.fallback => OutfitHatHairMode.Fallback,
                _ => OutfitHatHairMode.None,
            };
        }
    }
}
