using System;

namespace Genies.Refs
{
    /// <summary>
    /// Represents a generational handle to a resource object.
    /// </summary>
    internal interface IGenerationalResourceHandle : IGenerational, IDisposable
    {
        object Resource { get; }
        int ReferenceCount { get; }
        
        void AddReference();
        void RemoveReference();
        Ref NewReference();
    }
    
    /// <summary>
    /// Represents a generational handle to a resource object.
    /// </summary>
    internal interface IGenerationalResourceHandle<TResource> : IGenerationalResourceHandle
    {
        new TResource Resource { get; }
        
        event Action<TResource> Releasing;
        
        new Ref<TResource> NewReference();
    }
}