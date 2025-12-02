using System.Collections.Generic;
using Genies.Utilities;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Genies.Avatars
{
    [SerializableAs(typeof(GenieComponent), "blendshape-animator")]
    public sealed class BlendShapeAnimator : GenieComponent
    {
        public override string Name { get; }
        
        // dependencies
        private readonly UnityObjectRef<BlendShapeAnimatorConfig> _configRef;
        
        // state
        private BlendShapeAnimatorBehaviour _behaviour;

        public BlendShapeAnimator(string name, BlendShapeAnimatorConfig config)
        {
            Name = name;
            _configRef = new UnityObjectRef<BlendShapeAnimatorConfig>(config, UnityObjectRef.DisposalBehaviour.DontDestroy);
        }
        
        public BlendShapeAnimator(string name, UnityObjectRef<BlendShapeAnimatorConfig> configRef)
        {
            Name = name;
            _configRef = configRef;
        }
        
        public void RebuildMappings()
        {
            _behaviour.config = _configRef.Object;
            _behaviour.Renderers.Clear();
            _behaviour.Renderers.AddRange(Genie.Renderers);
            
            // if there is an animation feature manager then use its refreshed parameters so mappings are more precise
            if (Genie.Components.TryGet(out AnimationFeatureManager featureManager))
            {
                _behaviour.RebuildMappings(featureManager.AnimatorParameters);
            }
            else
            {
                _behaviour.RebuildMappings();
            }
        }
        
        public override GenieComponent Copy()
        {
            return new BlendShapeAnimator(Name, _configRef);
        }

        protected override bool TryInitialize()
        {
            // avoid adding two blend shape animator components with the same configs
            List<BlendShapeAnimator> blendShapeAnimators = Genie.Components.GetAll<BlendShapeAnimator>();
            foreach (BlendShapeAnimator component in blendShapeAnimators)
            {
                if (component._configRef == _configRef || component._configRef.Object != _configRef.Object)
                {
                    continue;
                }

                Debug.LogError($"[{nameof(BlendShapeAnimator)}] this genie already contains a {nameof(BlendShapeAnimator)} component with the same config: {_configRef.Object.name}");
                return false;
            }
            
            if (!Genie.Animator)
            {
                Debug.LogError($"[{nameof(BlendShapeAnimator)}] this genie does not have an animator: {Genie.Root.name}");
                return false;
            }
            
            _behaviour = Genie.Animator.gameObject.AddComponent<BlendShapeAnimatorBehaviour>();
            RebuildMappings();
            Genie.RootRebuilt += RebuildMappings;
            return true;
        }

        protected override void OnRemoved()
        {
            if (_behaviour)
            {
                Object.Destroy(_behaviour);
            }

            if (Genie is not null)
            {
                Genie.RootRebuilt -= RebuildMappings;
            }
        }

        protected internal override void OnAnimationFeatureManagerRefreshed()
        {
            RebuildMappings();
        }

        public JToken Serialize()
        {
            if (!_configRef.Object)
            {
                return null;
            }

            return new JObject
            {
                { "name", Name },
                { "config", JScriptableObject.FromObject(_configRef.Object) }
            };
        }

        public static GenieComponent Deserialize(JToken token)
        {
            if (token is not JObject obj)
            {
                return null;
            }

            if (!obj.TryGetValue("config", out JToken configToken) || configToken is not JObject objectConfigToken)
            {
                return null;
            }

            string name = obj.TryGetValue("name", out JToken nameToken) ? nameToken.Value<string>() : string.Empty;
            var soToken = new JScriptableObject(objectConfigToken);
            UnityObjectRef<BlendShapeAnimatorConfig> configRef = soToken.ToScriptableObject<BlendShapeAnimatorConfig>();
            
            return new BlendShapeAnimator(name, configRef);
        }
    }
}