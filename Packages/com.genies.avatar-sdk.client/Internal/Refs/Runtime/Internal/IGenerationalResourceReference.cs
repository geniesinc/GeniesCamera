using System;

namespace Genies.Refs
{
    /// <summary>
    /// Represents a unique reference to a disposable resource. The resource itself will not be disposed until all references to it has been disposed.
    /// </summary>
    internal interface IGenerationalResourceReference : IGenerational, IDisposable
    {
        object Item { get; }
        Handle Handle { get; }
        IGenerationalResourceReference New();
    }
    
    /// <summary>
    /// Represents a unique reference to a disposable resource. The resource itself will not be disposed until all references to it has been disposed.
    /// </summary>
    internal interface IGenerationalResourceReference<TResource> : IGenerationalResourceReference
    {
        new TResource Item { get; }
        new Handle<TResource> Handle { get; }
        new IGenerationalResourceReference<TResource> New();
    }
}