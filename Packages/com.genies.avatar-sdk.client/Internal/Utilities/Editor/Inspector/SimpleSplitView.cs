using UnityEditor;
using UnityEngine;

namespace Genies.Utilities.Editor.Inspector
{
    public class SimpleSplitView
    {
        public bool init = false;
        public float splitPivot = 0f;
        public bool resizing = false;
        public Rect cursorChangeRect;
        public Rect cursorHintRect;

        public float cursorHintSize = 2f;
        public float cursorSize = 6f;
        public Color cursorHintColor = new Color(0f, 0f, 0f, 0.35f);

        // These thresholds are now set via the constructor.
        private readonly float minLeftPanelWidth;
        private readonly float minRightPanelWidth;

        public SimpleSplitView(float minLeftPanelWidth = 0, float minRightPanelWidth = 0)
        {
            this.minLeftPanelWidth = minLeftPanelWidth;
            this.minRightPanelWidth = minRightPanelWidth;
        }

        public bool Draw(Rect rect)
        {
            var startY = rect.y;
            bool isRepaint = Event.current.type == EventType.Repaint;

            // Calculate effective clamping values.
            float effectiveMinLeft = minLeftPanelWidth;
            float effectiveMinRight = minRightPanelWidth;
            if (rect.width < (minLeftPanelWidth + minRightPanelWidth))
            {
                // Not enough space: scale down proportionally.
                float totalMin = minLeftPanelWidth + minRightPanelWidth;
                effectiveMinLeft = rect.width * (minLeftPanelWidth / totalMin);
                effectiveMinRight = rect.width * (minRightPanelWidth / totalMin);
            }

            // Initialize splitPivot on first repaint.
            if (!this.init && isRepaint)
            {
                this.init = true;
                // Default value: 25% of total width clamped between effective thresholds.
                this.splitPivot = Mathf.Clamp(rect.width * 0.25f, effectiveMinLeft, rect.width - effectiveMinRight);
            }

            if (!this.resizing)
            {
                this.cursorChangeRect.Set(this.splitPivot - 2f, startY, this.cursorSize, rect.height);
                this.cursorHintRect.Set(this.splitPivot - 2f, startY, this.cursorHintSize, rect.height);
            }

            EditorGUI.DrawRect(this.cursorHintRect, this.cursorHintColor);
            EditorGUIUtility.AddCursorRect(this.cursorChangeRect, MouseCursor.ResizeHorizontal);

            // Start resizing if the mouse is down over the draggable area.
            if (Event.current.type == EventType.MouseDown && this.cursorChangeRect.Contains(Event.current.mousePosition))
            {
                this.resizing = true;
                Event.current.Use();
            }

            // Update the split pivot while dragging.
            if (this.resizing && (Event.current.type == EventType.MouseDrag || Event.current.type == EventType.Repaint))
            {
                var y = this.cursorChangeRect.y;
                var h = this.cursorChangeRect.height;
                // Clamp the splitPivot using the effective minimums.
                this.splitPivot = Mathf.Clamp(Event.current.mousePosition.x, effectiveMinLeft, rect.width - effectiveMinRight);
                this.cursorChangeRect.Set(this.splitPivot - 2, y, this.cursorSize, h);
                this.cursorHintRect.Set(this.splitPivot - 2, y, this.cursorHintSize, h);

                if (Event.current.type == EventType.MouseDrag)
                {
                    Event.current.Use();
                }
            }

            if (Event.current.type == EventType.MouseUp)
            {
                this.resizing = false;
            }

            return this.resizing;
        }
    }
}
