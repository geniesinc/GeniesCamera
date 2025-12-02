using System;
using System.Collections.Generic;

namespace Genies.Avatars
{
    public sealed partial class BodyController : IBodyController, IDisposable
    {
        public IReadOnlyList<string> Attributes { get; }
        public BodyAttributesConfig Config
        {
            get => _config;
            set
            {
                if (_config == value)
                {
                    return;
                }

                if (_config)
                {
                    _config.Updated -= OnConfigUpdated;
                }

                _config = value;
                if (_config)
                {
                    _config.Updated += OnConfigUpdated;
                }

                Rebuild();
            }
        }
        
        public event Action Updated = delegate { };
        
        public bool RebuildOnConfigUpdated = true;
        
        // dependencies
        private readonly IEditableGenie _genie;
        
        // state
        private BodyAttributesConfig _config;
        private readonly Dictionary<string, AttributeController> _controllers;
        private readonly Dictionary<string, GenieJointModifier> _jointModifiers;
        private readonly Dictionary<string, BlendShapeHandle> _blendShapeHandles;
        private readonly SkeletonModifier _skeletonModifier;
        private readonly List<string> _attributeNames;
        
        private readonly GenieJointModifier _defaultJointModifier;
        private readonly BlendShapeHandle _defaultBlendShapeHandle;

        public BodyController(IEditableGenie genie, BodyAttributesConfig config = null)
        {
            _genie = genie;
            _genie.RootRebuilt += OnGenieRootRebuilt;
            
            _controllers = new Dictionary<string, AttributeController>();
            _jointModifiers = new Dictionary<string, GenieJointModifier>();
            _blendShapeHandles = new Dictionary<string, BlendShapeHandle>();
            
            if (!genie.Components.TryGet(out _skeletonModifier))
            {
                genie.Components.Add(_skeletonModifier = new SkeletonModifier());
            }

            _attributeNames = new List<string>();
            Attributes = _attributeNames.AsReadOnly();
            
            _defaultJointModifier = new GenieJointModifier(string.Empty);
            _defaultBlendShapeHandle = new BlendShapeHandle(string.Empty, null);
            
            Config = config;
        }
        
        public bool HasAttribute(string name)
        {
            return _controllers.ContainsKey(name);
        }

        public float GetAttributeWeight(string name)
        {
            if (!string.IsNullOrEmpty(name) && _controllers.TryGetValue(name, out AttributeController controller))
            {
                return controller.Weight;
            }

            return 0.0f;
        }

        public void SetAttributeWeight(string name, float weight)
        {
            if (string.IsNullOrEmpty(name) || !_controllers.TryGetValue(name, out AttributeController controller) || controller.Weight == weight)
            {
                return;
            }

            controller.Weight = weight;
            controller.Apply();
            Updated.Invoke();
        }

        public void SetPreset(IReadOnlyDictionary<string, float> preset)
        {
            bool updated = false;
            foreach ((string attribute, float weight) in preset)
            {
                if (string.IsNullOrEmpty(attribute) || !_controllers.TryGetValue(attribute, out AttributeController controller) || controller.Weight == weight)
                {
                    continue;
                }

                controller.Weight = weight;
                controller.Apply();
                updated = true;
            }
            
            if (updated)
            {
                Updated.Invoke();
            }
        }

        public void SetPreset(IEnumerable<BodyAttributeState> preset)
        {
            bool updated = false;
            foreach (BodyAttributeState attributeState in preset)
            {
                if (string.IsNullOrEmpty(attributeState.name) || !_controllers.TryGetValue(attributeState.name, out AttributeController controller) || controller.Weight == attributeState.weight)
                {
                    continue;
                }

                controller.Weight = attributeState.weight;
                controller.Apply();
                updated = true;
            }
            
            if (updated)
            {
                Updated.Invoke();
            }
        }

        public void GetAllAttributeWeights(IDictionary<string, float> results)
        {
            foreach (AttributeController controller in _controllers.Values)
            {
                results[controller.Name] = controller.Weight;
            }
        }

        public Dictionary<string, float> GetAllAttributeWeights()
        {
            var results = new Dictionary<string, float>();
            GetAllAttributeWeights(results);
            return results;
        }

        public void GetAllAttributeStates(ICollection<BodyAttributeState> results)
        {
            foreach (AttributeController controller in _controllers.Values)
            {
                results.Add(new BodyAttributeState(controller.Name, controller.Weight));
            }
        }

        public List<BodyAttributeState> GetAllAttributeStates()
        {
            var results = new List<BodyAttributeState>(_attributeNames.Count);
            GetAllAttributeStates(results);
            return results;
        }

        /// <summary>
        /// If <see cref="RebuildOnConfigUpdated"/> is not enabled you must call this method after making modifications
        /// to the current <see cref="Config"/> asset.
        /// </summary>
        /// <param name="rebuildBlendShapeInidices">Enable this if the genie renderers structure or their mesh blend
        /// shapes have changed. Otherwise, the previously calculated shape indices (from the names) will be used</param>
        public void Rebuild(bool rebuildBlendShapeInidices = false)
        {
            // reset all blend shape handles and apply default weights if the mesh didn't change
            _defaultBlendShapeHandle.Reset();
            foreach (BlendShapeHandle handle in _blendShapeHandles.Values)
            {
                handle.Reset();
                if (!rebuildBlendShapeInidices)
                {
                    handle.ApplyDefault();
                }
            }

            // if no config then just clear collections and return
            if (!_config)
            {
                _controllers.Clear();
                _jointModifiers.Clear();
                _blendShapeHandles.Clear();
                _attributeNames.Clear();
                _skeletonModifier.RemoveAllModifiers();
                return;
            }
            
            // reset all joint modifiers
            _defaultJointModifier.Reset();
            foreach (GenieJointModifier modifier in _jointModifiers.Values)
            {
                modifier.Reset();
            }

            // rebuild all attribute controllers from the config
            var attributesToRemove = new HashSet<string>(_attributeNames);
            var jointModifiersToRemove = new HashSet<string>(_jointModifiers.Keys);
            var blendShapeHandlesToRemove = new HashSet<string>(_blendShapeHandles.Keys);
            _attributeNames.Clear();
            foreach (BodyAttributesConfig.Attribute attributeConfig in _config.Attributes)
            {
                if (!_controllers.TryGetValue(attributeConfig.name, out AttributeController controller))
                {
                    _controllers[attributeConfig.name] = controller = new AttributeController(this);
                }

                controller.Rebuild(attributeConfig);
                attributesToRemove.Remove(attributeConfig.name);
                controller.RemoveUsedJointsFrom(jointModifiersToRemove);
                controller.RemoveUsedBlendshapesFrom(blendShapeHandlesToRemove);
                _attributeNames.Add(attributeConfig.name);
            }

            // remove attributes, joint modifiers and blend shapes no longer used by the new config
            foreach (string attribute in attributesToRemove)
            {
                _controllers.Remove(attribute);
            }

            foreach (string blendShape in blendShapeHandlesToRemove)
            {
                _blendShapeHandles.Remove(blendShape);
            }

            foreach (string jointModifier in jointModifiersToRemove)
            {
                if (!_jointModifiers.TryGetValue(jointModifier, out GenieJointModifier modifier))
                {
                    continue;
                }

                _skeletonModifier.RemoveModifier(modifier);
                _jointModifiers.Remove(jointModifier);
            }
            
            // rebuild blend shape handles if specified
            if (rebuildBlendShapeInidices)
            {
                foreach (BlendShapeHandle handle in _blendShapeHandles.Values)
                {
                    handle.Rebuild();
                }
            }
            
            // apply all attribute controllers
            foreach (AttributeController controller in _controllers.Values)
            {
                controller.Apply();
            }

            Updated.Invoke();
        }
        
        public void Dispose()
        {
            if (_config)
            {
                _config.Updated -= OnConfigUpdated;
            }

            _genie.RootRebuilt -= OnGenieRootRebuilt;
            _config = null;
            _controllers.Clear();
            _attributeNames.Clear();
            
            // if the skeleton modifier is still added to the genie
            if (_skeletonModifier.Genie is not null)
            {
                // don't remove the skeleton modifier component as it could be used by other scripts. Just remove our joint modifiers
                foreach (GenieJointModifier modifier in _jointModifiers.Values)
                {
                    _skeletonModifier.RemoveModifier(modifier);
                }
            }
            
            // apply blend shape defaults
            foreach (BlendShapeHandle handle in _blendShapeHandles.Values)
            {
                handle.ApplyDefault();
            }

            _jointModifiers.Clear();
            _blendShapeHandles.Clear();
        }

        // method used by attribute controllers to register each of their joint configs (and get back the GenieJointModifier to use)
        private GenieJointModifier AddAttributeJoint(BodyAttributesConfig.Joint config)
        {
            if (string.IsNullOrEmpty(config.name))
            {
                return _defaultJointModifier;
            }

            // get or create modifier
            if (!_jointModifiers.TryGetValue(config.name, out GenieJointModifier modifier))
            {
                _jointModifiers[config.name] = modifier = new GenieJointModifier(config.name);
                _skeletonModifier.AddModifier(modifier);
            }
            
            // just one joint config set for scale late update is enough to set the joint modifier with scale late update too
            if (config.scaleLateUpdate)
            {
                modifier.ScaleLateUpdate = true;
            }

            // enable channels based on the config (don't disable any channel since they may have been enabled by other attributes)
            if (config.enablePosition)
            {
                modifier.PositionOperation = JointModifier.Operation.Offset;
            }

            if (config.enableRotation)
            {
                modifier.RotationOperation = JointModifier.Operation.Offset;
            }

            if (config.enableScale)
            {
                modifier.ScaleOperation = JointModifier.Operation.Offset;
            }

            return modifier;
        }

        // method used by attribute controllers to register each of their blend shape configs (and get back the handles to use)
        private void AddAttributeBlendShape(BodyAttributesConfig.BlendShape config, out BlendShapeHandle minHandle, out BlendShapeHandle maxHandle)
        {
            // get or create min blend shape handle
            if (string.IsNullOrEmpty(config.minName))
            {
                minHandle = _defaultBlendShapeHandle;
            }
            else if (!_blendShapeHandles.TryGetValue(config.minName, out minHandle))
            {
                _blendShapeHandles[config.minName] = minHandle = new BlendShapeHandle(config.minName, _genie);
                minHandle.Rebuild();
            }
            
            // get or create max blend shape handle
            if (string.IsNullOrEmpty(config.maxName))
            {
                maxHandle = _defaultBlendShapeHandle;
            }
            else if (!_blendShapeHandles.TryGetValue(config.maxName, out maxHandle))
            {
                _blendShapeHandles[config.maxName] = maxHandle = new BlendShapeHandle(config.maxName, _genie);
                maxHandle.Rebuild();
            }

            // add one to the number of controlling attributes for each handle (add only one if both handles are the same)
            if (minHandle == maxHandle)
            {
                ++minHandle.ControllingAttributes;
            }
            else
            {
                ++minHandle.ControllingAttributes;
                ++maxHandle.ControllingAttributes;
            }
        }

        private void OnConfigUpdated()
        {
            if (RebuildOnConfigUpdated)
            {
                Rebuild();
            }
        }

        private void OnGenieRootRebuilt()
        {
            // joint modifiers are already updated by the skeleton modifier component
            
            // update blend shape handles (rebuild them and reapply the weights)
            foreach (BlendShapeHandle handle in _blendShapeHandles.Values)
            {
                handle.Rebuild();
                handle.Apply();
            }
        }
    }
}