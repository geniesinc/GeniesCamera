using System.Collections.Generic;
using Genies.Avatars;
using Genies.Components.Dynamics;
using Genies.Utilities;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Genies.Dynamics
{
    /// <summary>
    /// Dynamics refers to all physics simulation based motion that is added to an avatar at runtime.
    /// This is secondary motion that is added on top of hand authored animation keyframes.
    ///
    /// <see cref="DynamicsStructure"/> for implementation of physics simulation.
    /// </summary>
    [SerializableAs(typeof(GenieComponent), "dynamics-animator")]
    public class DynamicsAnimator : GenieComponent
    {
        public override string Name => _recipeRef.Object.StructureName;

        /// <summary>
        /// Disable this component to reset dynamic joints to their original state and stop dynamics behaviour. You can
        /// re-enable it at any moment.
        /// </summary>
        public bool Enabled
        {
            get
            {
                if (_behaviour)
                {
                    _enabled = _behaviour.enabled;
                }

                return _enabled;
            }
            set
            {
                _enabled = value;
                if (_behaviour)
                {
                    _behaviour.enabled = value;
                }
            }
        }

        private readonly UnityObjectRef<DynamicsRecipe> _recipeRef;
        private DynamicsStructure _behaviour;
        private bool _enabled = true;

        public DynamicsAnimator(DynamicsRecipe recipe)
        {
            _recipeRef = new UnityObjectRef<DynamicsRecipe>(recipe, UnityObjectRef.DisposalBehaviour.DontDestroy);
        }

        public DynamicsAnimator(UnityObjectRef<DynamicsRecipe> recipeRef)
        {
            _recipeRef = recipeRef;
        }

        public override GenieComponent Copy()
        {
            return new DynamicsAnimator(_recipeRef);
        }

        protected override bool TryInitialize()
        {
            // avoid adding two dynamic animator components with the same config
            List<DynamicsAnimator> dynamicAnimators = Genie.Components.GetAll<DynamicsAnimator>();
            foreach (DynamicsAnimator component in dynamicAnimators)
            {
                // the name is the structure name of the recipe, we assume that different recipes will have a different structure name
                if (component.Name != Name)
                {
                    continue;
                }

                Debug.LogError($"[{nameof(DynamicsAnimator)}] this genie already contains a {nameof(DynamicsAnimator)} component with the same config: {Name}");
                return false;
            }

            // subscribe to genie, so we rebuild the behaviour every time the root is rebuilt
            Genie.RootRebuilt += RebuildBehaviour;
            RebuildBehaviour();

            return true;
        }

        protected override void OnRemoved()
        {
            Genie.RootRebuilt -= RebuildBehaviour;
            DestroyBehaviour();
        }

        private void RebuildBehaviour()
        {
            // destroy previous behaviour if any
            DestroyBehaviour();

            var dynamicsGo = new GameObject(_recipeRef.Object.StructureName);
            dynamicsGo.transform.SetParent(Genie.Root.transform, worldPositionStays: false);
            _behaviour = dynamicsGo.AddComponent<DynamicsStructure>();
            _behaviour.BuildFromRecipe(_recipeRef.Object, Genie.SkeletonRoot);
            _behaviour.enabled = _enabled;
        }

        private void DestroyBehaviour()
        {
            if (!_behaviour)
            {
                return;
            }

            // this ensures no issues with possible edge cases where RebuildBehaviour could be called multiple times
            _behaviour.ResetParticlesToHomeTransforms();

            foreach (var particle in _behaviour.Particles)
            {
                if (particle)
                {
                    Object.DestroyImmediate(particle);
                }
            }

            foreach (var link in _behaviour.Links)
            {
                if (link)
                {
                    Object.DestroyImmediate(link);
                }
            }

            foreach (var collider in _behaviour.Colliders)
            {
                if (collider)
                {
                    Object.DestroyImmediate(collider);
                }
            }

            Object.DestroyImmediate(_behaviour.gameObject);
            _behaviour = null;
        }

        public JToken Serialize()
        {
            return JScriptableObject.FromObject(_recipeRef.Object);
        }

        public static GenieComponent Deserialize(JToken token)
        {
            if (token is not JObject objToken)
            {
                return null;
            }

            if (objToken is not JScriptableObject soToken)
            {
                soToken = new JScriptableObject(objToken);
            }

            UnityObjectRef<DynamicsRecipe> recipeRef = soToken.ToScriptableObject<DynamicsRecipe>();
            return new DynamicsAnimator(recipeRef);
        }
    }
}
