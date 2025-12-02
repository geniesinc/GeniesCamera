using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Genies.Avatars
{
    public class SubSpeciesMetadata : IEquatable<SubSpeciesMetadata>
    {
        
        public bool IsValid => !string.IsNullOrEmpty(Id);
        public string Id;
        
        private readonly DateTime _creationTimeStamp;
        
        public SubSpeciesMetadata(string assetId)
        {
            Id = assetId;
            _creationTimeStamp = DateTime.Now;
        }
        
        public bool Equals(SubSpeciesMetadata other)
            => other.Id == Id;

        public override int GetHashCode()
            => Id?.GetHashCode() ?? 0;

        public override bool Equals(object obj)
            => obj is SubSpeciesMetadata item && item.Id == Id;

        public static bool operator ==(SubSpeciesMetadata left, SubSpeciesMetadata right)
            => left.Id == right.Id;

        public static bool operator !=(SubSpeciesMetadata left, SubSpeciesMetadata right)
            => left.Id != right.Id;
        
    }
}
