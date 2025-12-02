using System;

namespace Genies.Refs
{
    /// <summary>
    /// Represents a disposable resource. You can implement this to create your own references/handles for your resources by
    /// using the CreateRef.From() and CreateHandle.From() static methods.
    /// </summary>
    public interface IResource<out TResource> : IDisposable
    {
        TResource Resource { get; }
    }
}