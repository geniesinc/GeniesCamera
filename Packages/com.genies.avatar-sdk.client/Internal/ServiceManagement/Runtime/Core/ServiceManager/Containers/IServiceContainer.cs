using System;
using System.Collections.Generic;
using UnityEngine;

namespace Genies.ServiceManagement
{
    
    /// <summary>
    /// A service container holds all instances that were resolved, it can have a parent
    /// container and can be owned by a GameObject to control its lifetime/scope. 
    /// </summary>
    internal interface IServiceContainer : IDisposable
    {
        GameObject Owner { get; }
        IServiceContainer ParentContainer { get; }
        object Get(Type serviceType);
        T Get<T>();
        IReadOnlyCollection<object> GetCollection(Type serviceType);
        IReadOnlyCollection<T> GetCollection<T>();
    }
}
