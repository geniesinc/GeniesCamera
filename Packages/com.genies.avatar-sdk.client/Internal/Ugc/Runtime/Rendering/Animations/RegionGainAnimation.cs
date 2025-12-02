using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Shaders;
using UnityEngine;

namespace Genies.Ugc
{
    /// <summary>
    /// A mega shader material animation for the pattern gain.
    /// </summary>
    public class RegionGainAnimation : MaterialAnimationBase
    {
        public readonly int RegionIndex;
        
        private float _gain;
        
        public RegionGainAnimation(int regionIndex)
            : this(null, regionIndex) { }
        
        public RegionGainAnimation(Material material, int regionIndex)
            : base(material)
        {
            RegionIndex = regionIndex;
        }

        protected override async UniTask PlayAsync(ValueAnimation animation, Material material, CancellationToken cancellationToken)
        {
            _gain = material.GetFloat(MegaShaderRegionProperty.PatternGain, RegionIndex);
            
            await animation.StartAnimationAsync(value =>
            {
                float currentGain = Mathf.LerpUnclamped(_gain, 0.0f, value);
                material.SetFloat(MegaShaderRegionProperty.PatternGain, RegionIndex, currentGain);
            }, cancellationToken: cancellationToken);
        }

        protected override void RestoreMaterialState(Material material)
        {
            material.SetFloat(MegaShaderRegionProperty.PatternGain, RegionIndex, _gain);
        }
    }
}
