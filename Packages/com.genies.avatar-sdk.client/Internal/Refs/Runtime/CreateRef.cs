using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Genies.Refs
{
    /// <summary>
    /// Contains static factory methods to create resource references.
    /// </summary>
    public static class CreateRef
    {
        public static Ref<T> FromHandle<T>(Handle<T> handle)
#if UNITY_EDITOR || DEVELOPMENT_BUILD || QA_BUILD
            => new Ref<T>(GenerationalResourceReference<T>.New(handle, new StackTrace(true)));
#else
            => new Ref<T>(GenerationalResourceReference<T>.New(handle));
#endif
        
        public static Ref<T> From<T>(IResource<T> resource)
            => FromHandle(CreateHandle.From(resource));
        
        public static Ref<T> FromAny<T>(T asset)
            => FromHandle(CreateHandle.FromAny(asset));

        public static Ref<T> FromAny<T>(T asset, Action<T> disposeCallback)
            => FromHandle(CreateHandle.FromAny(asset, disposeCallback));
        
        public static Ref<T> FromUnityObject<T>(T asset) where T : Object
            => FromHandle(CreateHandle.FromUnityObject(asset));
        
        public static Ref<T> FromUnityResource<T>(T asset) where T : Object
            => FromHandle(CreateHandle.FromUnityResource(asset));
        
        public static Ref<T> FromAddressable<T>(AsyncOperationHandle<T> operationHandle)
            => FromHandle(CreateHandle.FromAddressable(operationHandle));
        
        public static Ref<T> FromDisposable<T>(T disposable) where T : IDisposable
            => FromHandle(CreateHandle.FromDisposable(disposable));
        
        public static Ref<T> FromDependentResource<T>(Ref<T> reference, params Ref[] dependencies)
            => FromHandle(CreateHandle.FromDependentResource(reference, dependencies));
        
        public static Ref<T> FromDependentResource<T>(Ref<T> reference, IEnumerable<Ref> dependencies)
            => FromHandle(CreateHandle.FromDependentResource(reference, dependencies));
        
        public static Ref<T> FromDependentResource<T, TOther>(Ref<T> reference, params Ref<TOther>[] dependencies)
            => FromHandle(CreateHandle.FromDependentResource(reference, dependencies));
        
        public static Ref<T> FromDependentResource<T, TOther>(Ref<T> reference, IEnumerable<Ref<TOther>> dependencies)
            => FromHandle(CreateHandle.FromDependentResource(reference, dependencies));
        
        public static Ref<T> FromDependentResource<T>(T asset, params Ref[] dependencies)
            => FromHandle(CreateHandle.FromDependentResource(asset, dependencies));
        
        public static Ref<T> FromDependentResource<T>(T asset, IEnumerable<Ref> dependencies)
            => FromHandle(CreateHandle.FromDependentResource(asset, dependencies));
        
        public static Ref<T> FromDependentResource<T, TOther>(T asset, params Ref<TOther>[] dependencies)
            => FromHandle(CreateHandle.FromDependentResource(asset, dependencies));
        
        public static Ref<T> FromDependentResource<T, TOther>(T asset, IEnumerable<Ref<TOther>> dependencies)
            => FromHandle(CreateHandle.FromDependentResource(asset, dependencies));
    }
}
