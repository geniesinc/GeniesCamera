using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Utilities
{
    /// <summary>
    /// Represents a reference to an <see cref="Object"/> that can have dependencies. It can be disposed from any thread
    /// so the object and its dependencies are destroyed on the main thread. It is also automatically disposed if the
    /// reference is lost (if destructed by the garbage collector). It is a safe way to handle assets created at runtime
    /// and ensuring they are released when no longer in use.
    /// <br/><br/>
    /// Please note that <see cref="Object"/> types are never garbage collected (i.e.: a <see cref="ScriptableObject"/>,
    /// a <see cref="MonoBehaviour"/>...) since Unity keeps internal references to them. This means that automatic
    /// disposal is not available if you keep a <see cref="UnityObjectRef"/> within any of those types.
    /// </summary>
    public class UnityObjectRef : IDisposable
    {
        public Object Object          { get; private set; }
        public int    DependencyCount => _dependencies.Length;

        private readonly DisposalBehaviour _behaviour;
        private readonly UnityObjectRef[] _dependencies;

        public UnityObjectRef(Object obj, DisposalBehaviour behaviour = DisposalBehaviour.Destroy)
        {
            Object = obj;
            _behaviour = behaviour;
            _dependencies = Array.Empty<UnityObjectRef>();
        }

        public UnityObjectRef(Object obj, IEnumerable<UnityObjectRef> dependencies, DisposalBehaviour behaviour = DisposalBehaviour.Destroy)
        {
            Object = obj;
            _behaviour = behaviour;
            _dependencies = dependencies is null ? Array.Empty<UnityObjectRef>() : dependencies.ToArray();
        }

        public UnityObjectRef(Object obj, IEnumerable<Object> dependencies, DisposalBehaviour behaviour = DisposalBehaviour.Destroy)
        {
            Object = obj;
            _behaviour = behaviour;

            if (dependencies is null)
            {
                _dependencies = Array.Empty<UnityObjectRef>();
                return;
            }

            var dependencyRefs = new List<UnityObjectRef>();
            foreach (Object dependency in dependencies)
            {
                dependencyRefs.Add(new UnityObjectRef(dependency));
            }

            _dependencies = dependencyRefs.ToArray();
        }

        public UnityObjectRef GetDependency(int index)
        {
            return _dependencies[index];
        }

        public void Dispose()
        {
            Destroy().Forget();
        }

        ~UnityObjectRef()
        {
            Dispose();
        }

        protected virtual async UniTaskVoid Destroy()
        {
            await UniTask.SwitchToMainThread();

            foreach (UnityObjectRef dependency in _dependencies)
            {
                dependency?.Dispose();
            }

            if (!Object)
            {
                Object = null;
                return;
            }

#if UNITY_EDITOR
            // when in the editor, check if the asset is a project asset, in that case avoid destroying it
            if (UnityEditor.AssetDatabase.IsMainAsset(Object)
                || UnityEditor.AssetDatabase.IsSubAsset(Object)
                || UnityEditor.AssetDatabase.IsNativeAsset(Object)
                || UnityEditor.AssetDatabase.IsForeignAsset(Object)
            ) {
                return;
            }

            // force DestroyImmediate if on edit mode
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(Object);
                Object = null;
                return;
            }
#endif

            switch (_behaviour)
            {
                case DisposalBehaviour.Destroy:
                    Object.Destroy(Object);
                    break;
                case DisposalBehaviour.DestroyImmediate:
                    Object.DestroyImmediate(Object);
                    break;
            }

            Object = null;
        }

        public enum DisposalBehaviour
        {
            /// <summary>
            /// Uses Object.Destroy().
            /// </summary>
            Destroy = 0,

            /// <summary>
            /// Uses Object.DestroyImmediate().
            /// </summary>
            DestroyImmediate = 1,

            /// <summary>
            /// Skipts destroying the object.
            /// </summary>
            DontDestroy = 2,
        }
    }

    /// <summary>
    /// Generically typed version of <see cref="UnityObjectRef"/>.
    /// </summary>
    public class UnityObjectRef<T> : UnityObjectRef
        where T : Object
    {
        public new T Object { get; private set; }

        public UnityObjectRef(T obj, DisposalBehaviour behaviour = DisposalBehaviour.Destroy)
            : base(obj, behaviour)
        {
            Object = obj;
        }

        public UnityObjectRef(T obj, IEnumerable<UnityObjectRef> dependencies, DisposalBehaviour behaviour = DisposalBehaviour.Destroy)
            : base(obj, dependencies, behaviour)
        {
            Object = obj;
        }

        public UnityObjectRef(T obj, IEnumerable<Object> dependencies, DisposalBehaviour behaviour = DisposalBehaviour.Destroy)
            : base(obj, dependencies, behaviour)
        {
            Object = obj;
        }

        protected override UniTaskVoid Destroy()
        {
            Object = null;
            return base.Destroy();
        }
    }
}
