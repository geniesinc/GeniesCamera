using System.Collections.Generic;
using UnityEngine;

namespace Genies.Utils
{
    /// <summary>
    /// Standalone version of the AttrManager so we can support facial animation on custom avatars loaded as FBX files.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class BlendShapeAnimator : MonoBehaviour
    {
        public BlendShapeAnimatorConfig config;
        
        // state
        private readonly List<DrivenAttrData> _drivenAttrData = new();
        private Animator _animator;
        private SkinnedMeshRenderer[] _renderers;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _renderers = GetComponentsInChildren<SkinnedMeshRenderer>();
        }

        private void Start()
        {
            if (!_animator)
            {
                Debug.LogError($"[{nameof(BlendShapeAnimator)}] missing Animator component");
                return;
            }
            
            if (_renderers is null || _renderers.Length == 0)
            {
                Debug.LogError($"[{nameof(BlendShapeAnimator)}] missing skinned mesh renderer components");
                return;
            }
            
            // create the mappings between animator parameters and mesh blendshapes
            foreach (AttrManagerChannel channel in config.channels)
            {
                foreach (DrivenAttribute drivenAttr in channel.drivenAttributes)
                {
                    foreach (string submesh in drivenAttr.targetSubmeshes)
                    {
                        string blendShapeName = $"{submesh}_blendShape.{drivenAttr.outputChannelName}";
                        CreateDrivenAttributeData(channel.inputChannelName, blendShapeName, drivenAttr.retargetBehavior);
                    }
                    
                    // glTF exports have all submeshes merged into a single blend shape, this line will support that
                    CreateDrivenAttributeData(channel.inputChannelName, drivenAttr.outputChannelName, drivenAttr.retargetBehavior);
                }
            }
        }

        private void LateUpdate()
        {
            foreach (DrivenAttrData data in _drivenAttrData)
            {
                float value = _animator.GetFloat(data.AnimatorParameterName);
                value = data.Behaviour switch
                {
                    ChannelRetargetBehavior.PositiveControl => value > 0.0f ? value : 0.0f,
                    ChannelRetargetBehavior.NegativeControl => value < 0.0f ? -value : 0.0f,
                    _ => value,
                };
                
                data.Renderer.SetBlendShapeWeight(data.BlendShapeIndex, value * data.BlendShapeWeight);
            }
        }

        private void CreateDrivenAttributeData(string inputChannelName, string blendShapeName, ChannelRetargetBehavior behavior)
        {
            foreach (SkinnedMeshRenderer renderer in _renderers)
            {
                Mesh mesh = renderer.sharedMesh;
                if (!mesh)
                    continue;
                
                int blendShapeIndex = mesh.GetBlendShapeIndex(blendShapeName);
                if (blendShapeIndex < 0)
                    continue;
                
                // get the maximum weight from the blend shape
                int lastFrameIndex = mesh.GetBlendShapeFrameCount(blendShapeIndex) - 1;
                float weight = mesh.GetBlendShapeFrameWeight(blendShapeIndex, lastFrameIndex);
                
                _drivenAttrData.Add(new DrivenAttrData(renderer, blendShapeIndex, weight, inputChannelName, behavior));
            }
        }
        
        private void OnDestroy()
        {
            foreach (DrivenAttrData data in _drivenAttrData)
                data.Renderer.SetBlendShapeWeight(data.BlendShapeIndex, 0.0f);
            
            _drivenAttrData.Clear();
        }
        
        private readonly struct DrivenAttrData
        {
            public readonly SkinnedMeshRenderer Renderer;
            public readonly int BlendShapeIndex;
            public readonly float BlendShapeWeight;
            public readonly string AnimatorParameterName;
            public readonly ChannelRetargetBehavior Behaviour;

            public DrivenAttrData(SkinnedMeshRenderer renderer, int blendShapeIndex, float blendShapeWeight,
                string animatorParameterName, ChannelRetargetBehavior behaviour)
            {
                Renderer = renderer;
                BlendShapeIndex = blendShapeIndex;
                BlendShapeWeight = blendShapeWeight;
                AnimatorParameterName = animatorParameterName;
                Behaviour = behaviour;
            }
        }
    }
}