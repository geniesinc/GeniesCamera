using UnityEngine;

namespace Genies.UI.Scroller
{
    /// <summary>
    /// Extension methods for RectTransform to provide additional utility functions for UI layout and positioning calculations.
    /// </summary>
    public static class RectTransformExtensions
    {
        private static readonly Vector3[] _corners = new Vector3[4];

        /// <summary>
        /// Returns a Rect in world space dimensions using <see cref="RectTransform.GetWorldCorners"/>.
        /// </summary>
        /// <param name="rectTransform">The RectTransform to get world rect for.</param>
        /// <returns>A Rect representing the world space bounds of the RectTransform.</returns>
        public static Rect GetWorldRect(this RectTransform rectTransform)
        {
            // This returns the world space positions of the corners in the order
            // [0] bottom left,
            // [1] top left
            // [2] top right
            // [3] bottom right
            rectTransform.GetWorldCorners(_corners);

            Vector2 min  = _corners[0];
            Vector2 max  = _corners[2];
            Vector2 size = max - min;

            return new Rect(min, size);
        }

        /// <summary>
        /// Checks if a <see cref="RectTransform"/> fully encloses another one.
        /// </summary>
        /// <param name="rectTransform">The containing RectTransform.</param>
        /// <param name="other">The RectTransform to check if contained.</param>
        /// <param name="padding">Optional padding to apply to the containing rect.</param>
        /// <returns>True if the first RectTransform fully contains the second one.</returns>
        public static bool FullyContains (this RectTransform rectTransform, RectTransform other, Vector2 padding = default)
        {

            padding *= other.lossyScale;
            var rect      = rectTransform.GetWorldRect();
            rect.xMax += padding.x;
            rect.xMin -= padding.x;
            rect.yMax += padding.y;
            rect.yMin -= padding.y;

            var otherRect = other.GetWorldRect();

            // Now that we have the world space rects simply check
            // if the other rect lies completely between min and max of this rect
            return rect.xMin <= otherRect.xMin
                   && rect.yMin <= otherRect.yMin
                   && rect.xMax >= otherRect.xMax
                   && rect.yMax >= otherRect.yMax;
        }

        /// <summary>
        /// Transform the bounds of the current rect transform to the space of another transform.
        /// </summary>
        /// <param name="source">The rect to transform</param>
        /// <param name="target">The target space to transform to</param>
        /// <returns>The transformed bounds</returns>
        public static Bounds TransformBoundsTo(this RectTransform source, Transform target)
        {
            // Based on code in ScrollRect's internal GetBounds and InternalGetBounds methods
            var bounds = new Bounds();
            if (source != null)
            {
                source.GetWorldCorners(_corners);

                var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
                var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

                var matrix = target.worldToLocalMatrix;
                for (int j = 0; j < 4; j++)
                {
                    Vector3 v = matrix.MultiplyPoint3x4(_corners[j]);
                    vMin = Vector3.Min(v, vMin);
                    vMax = Vector3.Max(v, vMax);
                }

                bounds = new Bounds(vMin, Vector3.zero);
                bounds.Encapsulate(vMax);
            }

            return bounds;
        }
    }
}
