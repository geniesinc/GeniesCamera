using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Genies.Avatars
{
    public sealed partial class BodyController
    {
        private sealed class AttributeController
        {
            public string Name => _config.name;
            
            public float Weight;
            
            // dependencies
            private readonly BodyController _controller;
            
            // state
            private BodyAttributesConfig.Attribute _config;
            private JointState[]                   _jointStates;
            private BlendShapeState[]              _blendShapeStates;

            public AttributeController(BodyController controller)
            {
                _controller = controller;
                _jointStates = Array.Empty<JointState>();
                _blendShapeStates = Array.Empty<BlendShapeState>();
            }
            
            public void Apply()
            {
                for (int i = 0; i < _jointStates.Length; ++i)
                {
                    ApplyJoint(ref _jointStates[i]);
                }

                for (int i = 0; i < _blendShapeStates.Length; ++i)
                {
                    ApplyBlendShape(ref _blendShapeStates[i]);
                }
            }

            public void Rebuild(BodyAttributesConfig.Attribute config)
            {
                _config = config;
                
                // rebuild joint states
                _jointStates = new JointState[config.joints.Count];
                for (int i = 0; i < config.joints.Count; ++i)
                {
                    _jointStates[i] = new JointState
                    {
                        Config   = config.joints[i],
                        Modifier = _controller.AddAttributeJoint(config.joints[i]),
                        Position = Vector3.zero,
                        Rotation = Vector3.zero,
                        Scale    = Vector3.one,
                    };
                }
                
                // rebuild blend shape states
                _blendShapeStates = new BlendShapeState[config.blendShapes.Count];
                for (int i = 0; i < config.blendShapes.Count; ++i)
                {
                    _controller.AddAttributeBlendShape(config.blendShapes[i], out BlendShapeHandle minHandle, out BlendShapeHandle maxHandle);
                    _blendShapeStates[i] = new BlendShapeState
                    {
                        Config    = config.blendShapes[i],
                        MinHandle = minHandle,
                        MaxHandle = maxHandle,
                        MinWeight = 0.0f,
                        MaxWeight = 0.0f,
                    };
                }
            }
            
            public void RemoveUsedJointsFrom(ICollection<string> joints)
            {
                for (int i = 0; i < _jointStates.Length; ++i)
                {
                    joints.Remove(_jointStates[i].Config.name);
                }
            }
            
            public void RemoveUsedBlendshapesFrom(ICollection<string> blendShapes)
            {
                for (int i = 0; i < _blendShapeStates.Length; ++i)
                {
                    blendShapes.Remove(_blendShapeStates[i].Config.minName);
                    blendShapes.Remove(_blendShapeStates[i].Config.maxName);
                }
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void ApplyJoint(ref JointState state)
            {
                /**
                 * In order to avoid having to recompute all attributes affecting this joint modifier, this method will
                 * only revert the offsets previously applied by this attribute (contained in the state), then calculate
                 * new offsets and apply them to the modifier.
                 */
                
                float weight;
                Vector3 configPosition;
                Vector3 configRotation;
                Vector3 configScale;
                
                if (Weight < 0.0f)
                {
                    weight = -Weight;
                    configPosition = state.Config.minPosition;
                    configRotation = state.Config.minRotation;
                    configScale = state.Config.minScale;
                }
                else
                {
                    weight = Weight;
                    configPosition = state.Config.maxPosition;
                    configRotation = state.Config.maxRotation;
                    configScale = state.Config.maxScale;
                }
                
                if (state.Config.enablePosition)
                {
                    Vector3 modifierPosition = state.Modifier.Position;
                    modifierPosition -= state.Position;
                    state.Position = weight * configPosition;
                    state.Modifier.Position = modifierPosition + state.Position;
                }
                
                if (state.Config.enableRotation)
                {
                    Vector3 modifierRotation = state.Modifier.Rotation;
                    modifierRotation -= state.Rotation;
                    state.Rotation = weight * configRotation;
                    state.Modifier.Rotation = modifierRotation + state.Rotation;
                }

                if (!state.Config.enableScale)
                {
                    return;
                }

                // get the modifier scale and un-apply current scale
                Vector3 modifierScale = state.Modifier.Scale;
                modifierScale.x /= state.Scale.x;
                modifierScale.y /= state.Scale.y;
                modifierScale.z /= state.Scale.z;
                
                // transform config scale based on settings
                if (state.Config.uniformScale)
                {
                    configScale.y = configScale.z = configScale.x;
                }

                if (state.Config.invertScale)
                {
                    configScale.x = 1.0f / configScale.x;
                    configScale.y = 1.0f / configScale.y;
                    configScale.z = 1.0f / configScale.z;
                }
                
                // calculate the scale that this attribute joint will apply to the modifier and apply it
                state.Scale = Vector3.one + weight * (configScale - Vector3.one);
                state.Modifier.Scale = Vector3.Scale(modifierScale, state.Scale);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void ApplyBlendShape(ref BlendShapeState state)
            {
                /**
                 * In order to avoid having to recompute all attributes affecting this blend shape handle, this method
                 * will only revert the weight previously applied by this attribute (contained in the state), then
                 * calculate new weight and apply it to the handle.
                 */
                
                if (state.MinHandle == state.MaxHandle)
                {
                    if (Weight < 0.0f)
                    {
                        UpdateBlendShapeHandle(ref state.MinWeight, state.Config.minDefaultWeight, state.Config.minTargetWeight, -Weight, state.MinHandle);
                    }
                    else
                    {
                        UpdateBlendShapeHandle(ref state.MinWeight, state.Config.maxDefaultWeight, state.Config.maxTargetWeight, Weight, state.MinHandle);
                    }

                    // if both handles are the same then use state.MinWeight to save the sate but keep MaxWeight in sync
                    state.MaxWeight = state.MinWeight;
                    state.MinHandle.Apply();
                    return;
                }
                
                // try to get min and max blend shape handles
                if (Weight < 0.0f)
                {
                    UpdateBlendShapeHandle(ref state.MinWeight, state.Config.minDefaultWeight, state.Config.minTargetWeight, -Weight, state.MinHandle);
                    UpdateBlendShapeHandle(ref state.MaxWeight, state.Config.maxDefaultWeight, state.MaxHandle);
                }
                else
                {
                    UpdateBlendShapeHandle(ref state.MinWeight, state.Config.minDefaultWeight, state.MinHandle);
                    UpdateBlendShapeHandle(ref state.MaxWeight, state.Config.maxDefaultWeight, state.Config.maxTargetWeight, Weight, state.MaxHandle);
                }
                
                state.MinHandle.Apply();
                state.MaxHandle.Apply();
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void UpdateBlendShapeHandle(ref float stateWeight, float defaultWeight, float targetWeight, float attributeWeight, BlendShapeHandle handle)
            {
                // revert previously applied weight by this attribute
                handle.Weight -= stateWeight;
                
                // calculate the new weight from this attribute and apply it
                stateWeight = defaultWeight + attributeWeight * (targetWeight - defaultWeight);
                handle.Weight += stateWeight;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void UpdateBlendShapeHandle(ref float stateWeight, float weight, BlendShapeHandle handle)
            {
                handle.Weight -= stateWeight;
                stateWeight = weight;
                handle.Weight += stateWeight;
            }
            
            private struct JointState
            {
                public BodyAttributesConfig.Joint Config;
                public GenieJointModifier Modifier;
            
                // current offsets applied by this attribute to the joint modifier
                public Vector3 Position;
                public Vector3 Rotation;
                public Vector3 Scale;
            }

            private struct BlendShapeState
            {
                public BodyAttributesConfig.BlendShape Config;
                public BlendShapeHandle MinHandle;
                public BlendShapeHandle MaxHandle;
                
                // current weight applied by this attribute to the min and max blend shapes
                public float MinWeight;
                public float MaxWeight;
            }
        }
    }
}