using UnityEngine;
using UnityEngine.UI;

namespace Genies.UI.Scroller
{
    /// <summary>
    /// Extension methods for ScrollRect to provide additional scrolling functionality and calculations.
    /// </summary>
    public static class ScrollRectExtensions
    {
        /// <summary>
        /// Normalize a distance to be used in verticalNormalizedPosition or horizontalNormalizedPosition.
        /// </summary>
        /// <param name="scrollRect">The ScrollRect to normalize distance for.</param>
        /// <param name="axis">Scroll axis, 0 = horizontal, 1 = vertical.</param>
        /// <param name="distance">The distance in the scroll rect's view's coordinate space.</param>
        /// <returns>The normalized scroll distance.</returns>
        public static float NormalizeScrollDistance(this ScrollRect scrollRect, int axis, float distance)
        {
            // Based on code in ScrollRect's internal SetNormalizedPosition method
            var viewport   = scrollRect.viewport;
            var viewRect   = viewport != null ? viewport : scrollRect.GetComponent<RectTransform>();
            var viewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);

            var content       = scrollRect.content;
            var contentBounds = content != null ? content.TransformBoundsTo(viewRect) : new Bounds();

            var hiddenLength = contentBounds.size[axis] - viewBounds.size[axis];
            return distance / hiddenLength;
        }

        /// <summary>
        /// Calculates the normalized scroll position needed to center a target RectTransform within the scroll view.
        /// </summary>
        /// <param name="scrollRect">The ScrollRect to calculate position for.</param>
        /// <param name="target">The target RectTransform to center in the view.</param>
        /// <param name="axis">The axis along which to calculate the centered position.</param>
        /// <returns>The normalized scroll position to center the target.</returns>
        public static float GetScrollToCenterNormalizedPosition(this ScrollRect scrollRect, RectTransform target, RectTransform.Axis axis = RectTransform.Axis.Vertical)
        {
            // The scroll rect's view's space is used to calculate scroll position
            var view = scrollRect.viewport ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();

            // Calcualte the scroll offset in the view's space
            var viewRect      = view.rect;
            var elementBounds = target.TransformBoundsTo(view);

            // Normalize and apply the calculated offset
            if (axis == RectTransform.Axis.Vertical)
            {
                var offset    = viewRect.center.y - elementBounds.center.y;
                var scrollPos = scrollRect.verticalNormalizedPosition - scrollRect.NormalizeScrollDistance(1, offset);
                return Mathf.Clamp(scrollPos, 0, 1);
            }
            else
            {
                var offset    = viewRect.center.x - elementBounds.center.x;
                var scrollPos = scrollRect.horizontalNormalizedPosition - scrollRect.NormalizeScrollDistance(0, offset);
                return Mathf.Clamp(scrollPos, 0, 1);
            }
        }
    }
}