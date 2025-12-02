using System;

namespace Genies.Refs
{
    /// <summary>
    /// Represents a handle that contains a resource. While the handle is alive you can create new references to it
    /// which will increase the <see cref="ReferenceCount"/>. When all references to a resource handle are disposed
    /// the handle will be released and the resource disposed.
    /// </summary>
    public readonly struct Handle : IEquatable<Handle>, IDisposable
    {
        public object Resource => _handleGeneration.Obj?.Resource;

        public int ReferenceCount => _handleGeneration.Obj?.ReferenceCount ?? 0;

        public bool IsAlive => _handleGeneration.IsAlive;

        private readonly GenerationReference<IGenerationalResourceHandle> _handleGeneration;

        internal Handle(IGenerationalResourceHandle handle)
            => _handleGeneration = new GenerationReference<IGenerationalResourceHandle>(handle);
        
        public Ref NewReference()
        {
            return _handleGeneration.Obj?.NewReference() ?? default;
        }
        
        /// <summary>
        /// Releases this handle and disposes the resource even if there is still alive references. It is highly discouraged
        /// to dispose handles manually unless you know what you are doing.
        /// </summary>
        public void Dispose()
        {
            _handleGeneration.Obj?.Dispose();
        }

        public bool TryCast<T>(out Handle<T> handle)
        {
            if (_handleGeneration.IsAlive && _handleGeneration.Obj is IGenerationalResourceHandle<T> validObj)
            {
                handle = new Handle<T>(validObj);
                return true;
            }
            
            handle = default;
            return false;
        }
        
        public bool Equals(Handle other)
        {
            return _handleGeneration.Equals(other._handleGeneration);
        }

        public override bool Equals(object obj)
        {
            return obj is Handle reference && reference.Equals(this);
        }

        public override int GetHashCode()
        {
            return _handleGeneration.GetHashCode();
        }

        public static bool operator ==(Handle left, Handle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Handle left, Handle right)
        {
            return !left.Equals(right);
        }
    }
    
    /// <summary>
    /// Represents a handle that contains a resource. While the handle is alive you can create new references to it
    /// which will increase the <see cref="ReferenceCount"/>. When all references to a resource handle are disposed
    /// the handle will be released and the resource disposed.
    /// </summary>
    public readonly struct Handle<T> : IEquatable<Handle<T>>, IDisposable
    {
        public T Resource
        {
            get
            {
                IGenerationalResourceHandle<T> handle = _handleGeneration.Obj;
                return handle is null ? default : handle.Resource;
            }
        }
        
        public int ReferenceCount => _handleGeneration.Obj?.ReferenceCount ?? 0;

        public bool IsAlive => _handleGeneration.IsAlive;
        
        /// <summary>
        /// Invoked just before the handle is being released and the resource disposed.
        /// </summary>
        public event Action<T> Releasing
        {
            add
            {
                if (_handleGeneration.Obj is not null)
                {
                    _handleGeneration.Obj.Releasing += value;
                }
            }

            remove
            {
                if (_handleGeneration.Obj is not null)
                {
                    _handleGeneration.Obj.Releasing -= value;
                }
            }
        }

        private readonly GenerationReference<IGenerationalResourceHandle<T>> _handleGeneration;

        internal Handle(IGenerationalResourceHandle<T> handle)
            => _handleGeneration = new GenerationReference<IGenerationalResourceHandle<T>>(handle);

        internal void AddReference()
        {
            _handleGeneration.Obj?.AddReference();
        }
        
        internal void RemoveReference()
        {
            _handleGeneration.Obj?.RemoveReference();
        }

        public Ref<T> NewReference()
        {
            return _handleGeneration.Obj?.NewReference() ?? default;
        }
        
        /// <summary>
        /// Releases this handle and disposes the resource even if there is still alive references. It is highly discouraged
        /// to dispose handles manually unless you know what you are doing.
        /// </summary>
        public void Dispose()
        {
            _handleGeneration.Obj?.Dispose();
        }

        public bool Equals(Handle<T> other)
        {
            return _handleGeneration.Equals(other._handleGeneration);
        }

        public override bool Equals(object obj)
        {
            return obj is Handle<T> reference && reference.Equals(this);
        }

        public override int GetHashCode()
        {
            return _handleGeneration.GetHashCode();
        }

        public static bool operator ==(Handle<T> left, Handle<T> right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Handle<T> left, Handle<T> right)
        {
            return !left.Equals(right);
        }
        
        public static implicit operator Handle(Handle<T> handle)
        {
            return new Handle(handle._handleGeneration.Obj);
        }
    }
}