using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.UI.Scroller
{
    public class ScrollableScaleAnimator : MonoBehaviour, IScrollableAnimator
    {
        [FormerlySerializedAs("curve")] public AnimationCurve Curve;

        public void Animate(float normalizedValue)
        {
            var scale = Curve.Evaluate(normalizedValue);
            transform.localScale = Vector3.one * scale;
        }
    }
}
