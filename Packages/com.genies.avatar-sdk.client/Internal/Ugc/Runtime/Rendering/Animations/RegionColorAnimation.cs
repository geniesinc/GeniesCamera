using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Shaders;
using UnityEngine;

namespace Genies.Ugc
{
    /// <summary>
    /// A mega shader material animation for the region color.
    /// </summary>
    public class RegionColorAnimation : MaterialAnimationBase
    {
        public readonly int RegionIndex;
        
        private Color _fromColor;
        
        public RegionColorAnimation(int regionIndex)
            : this(null, regionIndex) { }
        
        public RegionColorAnimation(Material material, int regionIndex)
            : base(material)
        {
            RegionIndex = regionIndex;
        }

        protected override async UniTask PlayAsync(ValueAnimation animation, Material material, CancellationToken cancellationToken)
        {
            const float oneThird = 1.0f / 3.0f;
            
            // get the current color and compute the target color for the animation
            _fromColor = material.GetColor(MegaShaderRegionProperty.Color, RegionIndex);
            // this will be a value in the range [0, 1] which indicates how dark (close to 0) or how light (close to 1) is the color
            float average = oneThird * (_fromColor.r + _fromColor.g + _fromColor.b);
            Color toColor = average <= 0.5f ? Color.white : Color.black;
            
            await animation.StartAnimationAsync(value =>
            {
                Color currentColor = Color.LerpUnclamped(_fromColor, toColor, value);
                material.SetColor(MegaShaderRegionProperty.Color, RegionIndex, currentColor);
            }, cancellationToken: cancellationToken);
        }

        protected override void RestoreMaterialState(Material material)
        {
            material.SetColor(MegaShaderRegionProperty.Color, RegionIndex, _fromColor);
        }
    }
}
