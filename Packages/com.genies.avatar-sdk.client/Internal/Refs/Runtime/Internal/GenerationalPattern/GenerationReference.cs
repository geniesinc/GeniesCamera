using System;
using System.Collections.Generic;

namespace Genies.Refs
{
    /// <summary>
    /// References a single generation of a generational object. It stores the generation at the moment
    /// of creation and uses it to determine if the reference is alive. The reference becomes dead
    /// if the object is not alive or its current generation is different from the stored one.
    /// </summary>
    internal readonly struct GenerationReference<T> : IEquatable<GenerationReference<T>>
        where T : class, IGenerational
    {
        public T Obj
            => IsAlive ? _obj : null;
        
        public bool IsAlive
            => _obj is not null && _obj.Generation == _generation && _obj.IsAlive;

        private readonly T _obj;
        private readonly ulong _generation;

        public GenerationReference(T obj)
        {
            if (obj is not { IsAlive: true })
            {
                _obj = null;
                _generation = 0;
                return;
            }
            
            _obj = obj;
            _generation = obj.Generation;
        }
        
        public bool Equals(GenerationReference<T> other)
        {
            return _generation == other._generation && ReferenceEquals(_obj, other._obj);
        }

        public override bool Equals(object obj)
        {
            return obj is GenerationReference<T> generationReference && generationReference.Equals(this);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (EqualityComparer<T>.Default.GetHashCode(_obj) * 397) ^ _generation.GetHashCode();
            }
        }

        public static bool operator ==(GenerationReference<T> left, GenerationReference<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GenerationReference<T> left, GenerationReference<T> right)
        {
            return !left.Equals(right);
        }
    }
}