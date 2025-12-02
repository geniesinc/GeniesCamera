using System;

namespace Genies.Avatars
{
    /// <summary>
    /// Information about an outfit item.
    /// </summary>
    public struct OutfitAssetMetadata : IEquatable<OutfitAssetMetadata>
    {
        private const float UgcwWearableExpirationSeconds = 5.0f;

        public bool IsValid => !string.IsNullOrEmpty(Id);

        public string Id;
        public string Slot;
        public string Subcategory;
        public string Species;
        public string Type;
        public OutfitCollisionData CollisionData; // TODO move CollisionData to this package and it also should be a struct

        public readonly string AssetIndexKey;

        private readonly DateTime _creationTimeStamp;
        private object _ugcWearable;

        public OutfitAssetMetadata(string assetIndexKey, object ugcWearable = null)
        {
            Id = Slot = Subcategory = Species = null;
            Type = default;
            CollisionData = new OutfitCollisionData();
            _ugcWearable = ugcWearable;
            _creationTimeStamp = DateTime.Now;

            AssetIndexKey = assetIndexKey;
        }

        // not a very elegant solution but we can avoid fetching from the wearable API twice when fetching UGCW items for the first time
        public object GetUgcwWearable()
        {
            TimeSpan timeSinceItemWasCreated = DateTime.Now - _creationTimeStamp;
            if (timeSinceItemWasCreated.TotalSeconds < UgcwWearableExpirationSeconds)
            {
                return _ugcWearable;
            }

            _ugcWearable = null;
            return null;
        }

        /**
         * Since this object represents static information about static outfit items, the same Id should always contain the same info
         * and hence the this object should be treated as equal when having the same Id. It would be an implementation error to have
         * two OutfitItemInfo objects with same Id but different information.
         *
         * By implementing the IEquatable interface we achieve 0 heap allocations in generic collections such as HashSet and Dictionary.
         */
        public bool Equals(OutfitAssetMetadata other)
            => other.Id == Id;

        public override int GetHashCode()
            => Id?.GetHashCode() ?? 0;

        public override bool Equals(object obj)
            => obj is OutfitAssetMetadata item && item.Id == Id;

        public static bool operator ==(OutfitAssetMetadata left, OutfitAssetMetadata right)
            => left.Id == right.Id;

        public static bool operator !=(OutfitAssetMetadata left, OutfitAssetMetadata right)
            => left.Id != right.Id;
    }
}
