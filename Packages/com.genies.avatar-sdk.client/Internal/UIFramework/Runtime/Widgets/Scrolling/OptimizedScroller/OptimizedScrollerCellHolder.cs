using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Genies.UI.Scroller
{
    /// <summary>
    /// The main object of the optimized scrolling, represents an empty object
    /// that is used as a stub for scaling the content of the scroller
    /// </summary>
    public class OptimizedScrollerCellHolder : MonoBehaviour
    {
        public RectTransform rectTransform;
        public LayoutElement layoutElement;
        public GameObject HeldCellView { get; private set; }
        public Vector2 Size => rectTransform.sizeDelta;
        private IScrollableAnimator[] _animators;

        public void SetCellView(GameObject cellView)
        {
            HeldCellView = cellView;

            Vector3 pos = rectTransform.anchoredPosition3D;
            pos.z = 0;
            rectTransform.anchoredPosition3D = pos;

            _animators = cellView == null ? null : gameObject.GetComponentsInChildren<IScrollableAnimator>();
        }

        public void NormalizedPositionChanged(float normalizedPos)
        {
            if (_animators == null || _animators.Length == 0)
            {
                return;
            }

            foreach (var anim in _animators)
            {
                anim.Animate(normalizedPos);
            }
        }
    }
}
