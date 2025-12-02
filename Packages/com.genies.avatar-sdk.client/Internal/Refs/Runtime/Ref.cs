using System;

namespace Genies.Refs
{
    /// <summary>
    /// Represents a single unique reference to a resource. References to the same resource point to the same <see cref="Handle"/>, which tracks
    /// the count of alive references. The handle gets automatically released and the resource disposed when all references to it are disposed.
    /// </summary>
    public readonly struct Ref : IDisposable, IEquatable<Ref>
    {
        public object Item
            => _referenceGeneration.Obj?.Item;
        
        public Handle Handle
            => _referenceGeneration.Obj?.Handle ?? default;
        
        public bool IsAlive
            => _referenceGeneration.IsAlive;

        private readonly GenerationReference<IGenerationalResourceReference> _referenceGeneration;

        internal Ref(IGenerationalResourceReference reference)
            => _referenceGeneration = new GenerationReference<IGenerationalResourceReference>(reference);

        public Ref New()
            => new Ref(_referenceGeneration.Obj?.New());

        public void Dispose()
            => _referenceGeneration.Obj?.Dispose();

        public bool TryCast<T>(out Ref<T> reference)
        {
            if (_referenceGeneration.IsAlive && _referenceGeneration.Obj is IGenerationalResourceReference<T> validObj)
            {
                reference = new Ref<T>(validObj);
                return true;
            }
            
            reference = default;
            return false;
        }
        
        public bool Equals(Ref other)
        {
            return _referenceGeneration.Equals(other._referenceGeneration);
        }

        public override bool Equals(object obj)
        {
            return obj is Ref reference && reference.Equals(this);
        }

        public override int GetHashCode()
        {
            return _referenceGeneration.GetHashCode();
        }

        public static bool operator ==(Ref left, Ref right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Ref left, Ref right)
        {
            return !left.Equals(right);
        }
    }
    
    /// <summary>
    /// Represents a single unique reference to a resource. References to the same resource point to the same <see cref="Handle"/>, which tracks
    /// the count of alive references. The handle gets automatically released and the resource disposed when all references to it are disposed.
    /// </summary>
    public readonly struct Ref<T> : IDisposable, IEquatable<Ref<T>>
    {
        public T Item
            => _referenceGeneration.Obj is null ? default : _referenceGeneration.Obj.Item;
        
        public Handle<T> Handle
            => _referenceGeneration.Obj?.Handle ?? default;
        
        public bool IsAlive
            => _referenceGeneration.IsAlive;

        private readonly GenerationReference<IGenerationalResourceReference<T>> _referenceGeneration;

        internal Ref(IGenerationalResourceReference<T> reference)
            => _referenceGeneration = new GenerationReference<IGenerationalResourceReference<T>>(reference);

        public Ref<T> New()
            => new Ref<T>(_referenceGeneration.Obj?.New());

        public void Dispose()
            => _referenceGeneration.Obj?.Dispose();
        
        public bool Equals(Ref<T> other)
        {
            return _referenceGeneration.Equals(other._referenceGeneration);
        }

        public override bool Equals(object obj)
        {
            return obj is Ref<T> reference && reference.Equals(this);
        }

        public override int GetHashCode()
        {
            return _referenceGeneration.GetHashCode();
        }

        public static bool operator ==(Ref<T> left, Ref<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Ref<T> left, Ref<T> right)
        {
            return !left.Equals(right);
        }

        public static implicit operator Ref(Ref<T> reference)
        {
            return new Ref(reference._referenceGeneration.Obj);
        }
    }
}