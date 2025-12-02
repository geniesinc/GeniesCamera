using System;
using System.Collections.Generic;
using Genies.Utilities;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Genies.Avatars
{
    /// <summary>
    /// <see cref="IIkrTarget"/> implementation for transform targets.
    /// </summary>
    public sealed class TransformIkrTarget : IIkrTarget
    {
        public string     Key         { get; }
        public float      Weight      => _animator.GetFloat(_weightPropertyId);
        public Vector3    Position    => Transform.position;
        public Quaternion Rotation    => Transform.rotation;
        public bool       HasPosition { get; set; }
        public bool       HasRotation { get; set; }
        public bool       IsFree      { get; }
        public Transform  Transform   { get; }

        private readonly Animator _animator;
        private readonly int      _weightPropertyId;
        private readonly bool     _destroyTransformOnDispose;

        public TransformIkrTarget(string key, Animator animator, Transform transform, int weightPropertyId,
            bool hasPosition, bool hasRotation, bool isFree, bool destroyTransformOnDispose)
        {
            Key = key;
            HasPosition = hasPosition;
            HasRotation = hasRotation;
            IsFree = isFree;
            Transform = transform;

            _animator = animator;
            _weightPropertyId = weightPropertyId;
            _destroyTransformOnDispose = destroyTransformOnDispose;
        }

        public void Dispose()
        {
            // use DestroyImmediate, otherwise when doing animator.Rebind() on the same frame will fail sometimes
            if (_destroyTransformOnDispose && Transform)
            {
                Object.DestroyImmediate(Transform.gameObject);
            }
        }

#region Config
        /// <summary>
        /// Serializable config that can be used to create <see cref="TransformIkrTarget"/>s from.
        /// </summary>
        [Serializable]
        public struct Config
        {
            public string child;
            [Tooltip("Ignored if the target is marked as free")]
            public string parent;
            public string weightProperty;
            [Tooltip("Whether or not to use this target's position for position blending")]
            public bool usePosition;
            [Tooltip("Whether or not to use this target's rotation for position blending")]
            public bool useRotation;
            public bool isFree;

            /// <summary>
            /// Hierarchy path to this target.
            /// </summary>
            public string Path =>
                isFree ? child :
                string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(child) ? null : $"{parent}/{child}";
        }

        public static void GetOrCreateFromConfigs(IEnumerable<Config> configs, Animator animator, AnimatorParameters parameters, ICollection<IIkrTarget> results,
            IDictionary<string, IIkrTarget> targets = null)
        {
            if (results is null)
            {
                throw new NullReferenceException($"[{nameof(TransformIkrTarget)}] IKR target results collection is null");
            }

            foreach (Config config in configs)
            {
                // skip targets that have a weight property that is not in the animator parameters
                if (!parameters.Contains(config.weightProperty))
                {
                    return;
                }

                try
                {
                    TransformIkrTarget target = targets is null ?
                        CreateFromConfig(config, animator) :
                        GetOrCreateFromConfig(config, animator, targets);

                    results.Add(target);
                }
                catch (Exception exception)
                {
                    Debug.LogError(exception);
                }
            }
        }

        public static TransformIkrTarget GetOrCreateFromConfig(Config config, Animator animator, IDictionary<string, IIkrTarget> targets)
        {
            // if the target already exists and it matches the config then we can reuse it
            string key = config.Path;
            if (targets.TryGetValue(key, out IIkrTarget target))
            {
                // check if it is the same target type and matches the config
                if (target is TransformIkrTarget tTarget
                    && tTarget.Key == config.Path
                    && tTarget.IsFree == config.isFree
                    && tTarget.Transform
                    && tTarget.Transform.name == config.child
                    && (tTarget.IsFree || tTarget.Transform.IsChildOf(animator.transform))
                    && tTarget._weightPropertyId == Animator.StringToHash(config.weightProperty)
                ) {
                    // sync the use pos/rot values as they can be changed dynamically
                    tTarget.HasPosition = config.usePosition;
                    tTarget.HasRotation = config.useRotation;
                    return tTarget;
                }

                // dispose the target since it doesn't match the config
                target.Dispose();
            }

            TransformIkrTarget transformTarget = CreateFromConfig(config, animator);
            targets[key] = transformTarget;

            return transformTarget;
        }

        public static TransformIkrTarget CreateFromConfig(Config config, Animator animator)
        {
            // get parent
            Transform parent;
            if (config.isFree)
            {
                parent = null;
            }
            else
            {
                parent = animator.transform.Find(config.parent);
                if (!parent)
                {
                    throw new Exception($"[{nameof(TransformIkrTarget)}] failed to create target: parent not found \"{config.parent}\"");
                }
            }

            // create target transform
            Transform transform = new GameObject(config.child).transform;
            transform.SetParent(config.isFree ? animator.transform : parent, worldPositionStays: false);

            return new TransformIkrTarget
            (
                key:                       config.Path,
                animator:                  animator,
                transform:                 transform,
                weightPropertyId:          Animator.StringToHash(config.weightProperty),
                hasPosition:               config.usePosition,
                hasRotation:               config.useRotation,
                isFree:                    config.isFree,
                destroyTransformOnDispose: true
            );
        }
#endregion
    }
}
