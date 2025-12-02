using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Genies.Refs
{
    /// <summary>
    /// Contains static factory methods to create resource handles. Handle creation is internal only since
    /// they are expected to have at least one reference to them when created.
    /// </summary>
    internal static class CreateHandle
    {
        public static Handle<T> From<T>(IResource<T> resource)
#if UNITY_EDITOR || DEVELOPMENT_BUILD || QA_BUILD
            => new Handle<T>(GenerationalResourceHandle<T>.New(resource, new StackTrace(true)));
#else
            => new Handle<T>(GenerationalResourceHandle<T>.New(resource));
#endif

        public static Handle<T> FromAny<T>(T asset)
            => From(AnyResource<T>.New(asset));

        public static Handle<T> FromAny<T>(T asset, Action<T> disposeCallback)
            => From(AnyResource<T>.New(asset, disposeCallback));

        public static Handle<T> FromUnityObject<T>(T asset) where T : Object
            => From(UnityResource<T>.New(asset, false));

        public static Handle<T> FromUnityResource<T>(T asset) where T : Object
            => From(UnityResource<T>.New(asset, true));

        public static Handle<T> FromAddressable<T>(AsyncOperationHandle<T> operationHandle)
            => From(AddressableResource<T>.New(operationHandle));

        public static Handle<T> FromDisposable<T>(T disposable) where T : IDisposable
            => From(DisposableResource<T>.New(disposable));

        public static Handle<T> FromDependentResource<T>(Ref<T> reference, params Ref[] dependencies)
            => From(DependentResource<T>.New(reference, dependencies));

        public static Handle<T> FromDependentResource<T>(Ref<T> reference, IEnumerable<Ref> dependencies)
            => From(DependentResource<T>.New(reference, dependencies));

        public static Handle<T> FromDependentResource<T, TOther>(Ref<T> reference, params Ref<TOther>[] dependencies)
            => From(DependentResource<T>.New(reference, dependencies.Select(dependency => (Ref)dependency)));

        public static Handle<T> FromDependentResource<T, TOther>(Ref<T> reference, IEnumerable<Ref<TOther>> dependencies)
            => From(DependentResource<T>.New(reference, dependencies.Select(dependency => (Ref)dependency)));

        public static Handle<T> FromDependentResource<T>(T asset, params Ref[] dependencies)
        {
            var resourceRef = CreateRef.FromAny(asset);
            return From(DependentResource<T>.New(resourceRef, dependencies));
        }

        public static Handle<T> FromDependentResource<T>(T asset, IEnumerable<Ref> dependencies)
        {
            var resourceRef = CreateRef.FromAny(asset);
            return From(DependentResource<T>.New(resourceRef, dependencies));
        }

        public static Handle<T> FromDependentResource<T, TOther>(T asset, params Ref<TOther>[] dependencies)
        {
            var resourceRef = CreateRef.FromAny(asset);
            return From(DependentResource<T>.New(resourceRef, dependencies.Select(dependency => (Ref)dependency)));
        }

        public static Handle<T> FromDependentResource<T, TOther>(T asset, IEnumerable<Ref<TOther>> dependencies)
        {
            var resourceRef = CreateRef.FromAny(asset);
            return From(DependentResource<T>.New(resourceRef, dependencies.Select(dependency => (Ref)dependency)));
        }
    }
}
