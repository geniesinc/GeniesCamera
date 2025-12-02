using System;
using System.Collections.Generic;
using Genies.Utilities;
using UnityEngine;
using ChannelRetargetBehavior = Genies.Avatars.BlendShapeAnimatorConfig.ChannelRetargetBehavior;
using Channel = Genies.Avatars.BlendShapeAnimatorConfig.Channel;
using DrivenAttribute = Genies.Avatars.BlendShapeAnimatorConfig.DrivenAttribute;

namespace Genies.Avatars
{
    /// <summary>
    /// Maps animator parameter to blend shape weights on one or multiple <see cref="SkinnedMeshRenderer"/> components.
    /// The parameter-to-blendshape mapping is given by the referenced <see cref="BlendShapeAnimatorConfig"/> asset.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class BlendShapeAnimatorBehaviour : MonoBehaviour
    {
        public BlendShapeAnimatorConfig config;
        [SerializeField] private List<SkinnedMeshRenderer> renderers = new();

        public List<SkinnedMeshRenderer> Renderers => renderers;

        // state
        private readonly List<DrivenAttrData> _drivenAttrData = new();
        private Animator _animator;
        private AnimatorParameters _animatorParameters;
        private Dictionary<RendererBlendshape, float> _shapeWeights;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (!_animator)
            {
                Debug.LogError($"[{nameof(BlendShapeAnimatorBehaviour)}] missing Animator component");
            }

            _animatorParameters = new AnimatorParameters(_animator);
            _shapeWeights = new Dictionary<RendererBlendshape, float>();

            RebuildMappings();
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                RebuildMappings();
            }
        }

        private void OnDestroy()
        {
            RestoreInitialWeights();
            _drivenAttrData.Clear();
        }

        /// <summary>
        /// Call this to rebuild the mappings if the config or renderers changed.
        /// </summary>
        /// <param name="includedParameters">If provided, only channels mapped to parameters contained here will be
        /// mapped. This can be used to prevent some warning/error logs happening on each LateUpdate.</param>
        public void RebuildMappings(AnimatorParameters includedParameters = null)
        {
            // restore previous weights and clear driven attr data
            RestoreInitialWeights();
            _drivenAttrData.Clear();

            if (!config)
            {
                return;
            }

            if (_animatorParameters == null)
            {
                return;
            }

            if (includedParameters != null)
            {
                _animatorParameters = includedParameters;
            }
            else
            {
                _animatorParameters.Refresh();
            }

            // create the mappings between animator parameters and mesh blendshapes
            foreach (Channel channel in config.channels)
            {
                if (!_animatorParameters.Contains(channel.inputChannelName))
                {
                    continue;
                }

                foreach (DrivenAttribute drivenAttr in channel.drivenAttributes)
                {
                    foreach (string submesh in drivenAttr.targetSubmeshes)
                    {
                        string blendShapeName = $"{submesh}_blendShape.{drivenAttr.outputChannelName}";
                        CreateDrivenAttributeData(channel.inputChannelName, blendShapeName, drivenAttr.retargetBehavior, drivenAttr.targetWeight);
                    }

                    // glTF exports have all submeshes merged into a single blend shape, this line will support that
                    CreateDrivenAttributeData(channel.inputChannelName, drivenAttr.outputChannelName, drivenAttr.retargetBehavior, drivenAttr.targetWeight);
                }
            }
        }

        private void LateUpdate()
        {
            if (!_animator.enabled || !_animator.runtimeAnimatorController)
            {
                return;
            }

            _shapeWeights.Clear();

            foreach (DrivenAttrData data in _drivenAttrData)
            {
                if (!data.Renderer || data.Renderer.sharedMesh != data.Mesh)
                {
                    continue;
                }

                // After the Genie loads, but before the Smart Avatar Controller is added,
                // the Animator may not have the expected parameters. This check will save us 100+ warnings per session.
                if (!AnimatorHasParameter(_animator, data.AnimatorParameterId, AnimatorControllerParameterType.Float))
                {
                    continue;
                }

                float value = _animator.GetFloat(data.AnimatorParameterId);
                value = data.Behaviour switch
                {
                    ChannelRetargetBehavior.PositiveControl => value > 0.0f ? value : 0.0f,
                    ChannelRetargetBehavior.NegativeControl => value < 0.0f ? -value : 0.0f,
                    ChannelRetargetBehavior.TargetWeight => Mathf.Lerp(data.InitialBlendShapeWeight, data.TargetWeight, value),
                    _ => value,
                };

                value *= 100; // Get into 0-100 space

                RendererBlendshape rbs = new RendererBlendshape(data.Renderer, data.BlendShapeIndex);
                _shapeWeights.TryGetValue(rbs, out float previous);
                value += previous;
                value = Mathf.Clamp(value, data.InitialBlendShapeWeight, data.MaxBlendShapeWeight);
                _shapeWeights[rbs] = value;
            }

            foreach (var kvp in _shapeWeights)
            {
                kvp.Key.Renderer.SetBlendShapeWeight(kvp.Key.BlendShapeIndex, kvp.Value);
            }
        }

        private void RestoreInitialWeights()
        {
            foreach (DrivenAttrData data in _drivenAttrData)
            {
                if (data.Renderer && data.Mesh && data.Renderer.sharedMesh == data.Mesh)
                {
                    int blendShapeCount = data.Mesh.blendShapeCount;
                    if (data.BlendShapeIndex >= 0 && data.BlendShapeIndex < blendShapeCount)
                    {
                        data.Renderer.SetBlendShapeWeight(data.BlendShapeIndex, data.InitialBlendShapeWeight);
                    }
                }
            }
        }

        private void CreateDrivenAttributeData(string inputChannelName, string blendShapeName, ChannelRetargetBehavior behavior, float targetWeight)
        {
            foreach (SkinnedMeshRenderer renderer in renderers)
            {
                if (!renderer)
                {
                    continue;
                }

                Mesh mesh = renderer.sharedMesh;
                if (!mesh)
                {
                    continue;
                }

                int blendShapeIndex = mesh.GetBlendShapeIndex(blendShapeName);
                if (blendShapeIndex < 0)
                {
                    continue;
                }

                // get the maximum weight from the blend shape
                int lastFrameIndex = mesh.GetBlendShapeFrameCount(blendShapeIndex) - 1;
                float maxWeight = mesh.GetBlendShapeFrameWeight(blendShapeIndex, lastFrameIndex);

                float initialWeight = renderer.GetBlendShapeWeight(blendShapeIndex);
                int animatorParameterId = Animator.StringToHash(inputChannelName);

                _drivenAttrData.Add(new DrivenAttrData(renderer, mesh, blendShapeIndex, maxWeight, initialWeight, animatorParameterId, behavior, targetWeight));
            }
        }

        private static bool AnimatorHasParameter(Animator anim, int id, AnimatorControllerParameterType type)
        {
            foreach (var p in anim.parameters)
            {
                if (p.type == type && p.nameHash == id)
                {
                    return true;
                }
            }
            return false;
        }

        private readonly struct DrivenAttrData
        {
            public readonly SkinnedMeshRenderer Renderer;
            public readonly Mesh Mesh;
            public readonly int BlendShapeIndex;
            public readonly float MaxBlendShapeWeight;
            public readonly float InitialBlendShapeWeight;
            public readonly int AnimatorParameterId;
            public readonly ChannelRetargetBehavior Behaviour;
            public readonly float TargetWeight;

            public DrivenAttrData(SkinnedMeshRenderer renderer, Mesh mesh, int blendShapeIndex, float maxBlendShapeWeight,
                float initialBlendShapeWeight, int animatorParameterId, ChannelRetargetBehavior behaviour, float targetWeight)
            {
                Renderer = renderer;
                Mesh = mesh;
                BlendShapeIndex = blendShapeIndex;
                MaxBlendShapeWeight = maxBlendShapeWeight;
                InitialBlendShapeWeight = initialBlendShapeWeight;
                AnimatorParameterId = animatorParameterId;
                Behaviour = behaviour;
                TargetWeight = targetWeight;
            }
        }

        private readonly struct RendererBlendshape : IEquatable<RendererBlendshape>
        {
            public readonly SkinnedMeshRenderer Renderer;
            public readonly int BlendShapeIndex;

            public RendererBlendshape(SkinnedMeshRenderer renderer, int blendShapeIndex)
            {
                Renderer = renderer;
                BlendShapeIndex = blendShapeIndex;
            }

            public bool Equals(RendererBlendshape other)
            {
                return (Renderer?.GetInstanceID() ?? 0) == (other.Renderer?.GetInstanceID() ?? 0) &&
                       BlendShapeIndex == other.BlendShapeIndex;
            }

            public override bool Equals(object obj)
            {
                return obj is RendererBlendshape other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + (Renderer != null ? Renderer.GetInstanceID() : 0);
                    hash = hash * 23 + BlendShapeIndex;
                    return hash;
                }
            }
            public static bool operator ==(RendererBlendshape left, RendererBlendshape right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(RendererBlendshape left, RendererBlendshape right)
            {
                return !(left == right);
            }
        }
    }
}
