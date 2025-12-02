using System;
using Genies.AvatarEditor.Core;
using Genies.Customization.Framework;
using Genies.Customization.Framework.Navigation;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.AvatarEditor
{
    [ExecuteAlways]
    [RequireComponent(typeof(GridLayoutGroup))]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class FixedSizeGrid : MonoBehaviour
#else
    public class FixedSizeGrid : MonoBehaviour
#endif
    {
        [SerializeField] private AvatarEditingScreen editingScreen;
        [SerializeField] private Customizer customizer;


        [Header("Tile Size (UI units after Canvas Scaler)")] [SerializeField]
        private float _baseCellWidth = 120f;

        [SerializeField] private float _baseCellHeight = 120f; // keep your aspect here (e.g., 1:1)

        [Header("Columns & Scaling")]
        [Tooltip(
            "We try to keep at least this many columns on phones. If not enough width, we can scale tiles down a bit.")]
        [SerializeField]
        private int _minColumnsToAimForGear = 3;

        [SerializeField] private int _minColumnsToAimForAvatar = 4;
        private int _minColumnsToAimFor = 3;

        [Tooltip("Allow shrinking tiles when screen is too narrow to fit _minColumnsToAimFor.")] [SerializeField]
        private bool _allowScaleDownToFitMinCols = true;

        [Tooltip("Smallest allowed scale when shrinking (1 = no shrink). E.g., 0.85 = shrink down to 85%.")]
        [Range(0.5f, 1f)]
        [SerializeField]
        private float _minScale = 0.85f;

        [Header("Layout")]
        [Tooltip("Rect we fill horizontally. If empty, uses this object's RectTransform.")]
        [SerializeField]
        private RectTransform _container;

        [Tooltip("Extra left/right padding beyond Grid.padding (e.g., safe area). x=left, y=right")] [SerializeField]
        private Vector2 _extraLRPadding = Vector2.zero;

        [Tooltip("Reserve width if your viewport expands when scrollbar appears (to avoid oscillations).")]
        [SerializeField]
        private float _reservedScrollbarWidth = 0f;

        [Tooltip("Top-stretch content alignment is recommended; set GridLayoutGroup.childAlignment = UpperCenter.")]
        [SerializeField]
        private TextAnchor childAlignment = TextAnchor.UpperCenter;

        // Internal
        private GridLayoutGroup grid;
        private RectTransform content;

        // Change detection to avoid snap-back while scrolling
        private float lastAvailWidth = -1f;
        private int lastCols = -1;
        private Vector2 lastCellSize = new Vector2(-1, -1);

        private void Awake()
        {
            grid = GetComponent<GridLayoutGroup>();
            content = GetComponent<RectTransform>();
            if (!_container)
            {
                _container = content;
            }

            if (customizer != null)
            {
                customizer.NodeChanged += OnNodeChanged;
            }
        }

        private void OnDestroy()
        {
            if (customizer != null)
            {
                customizer.NodeChanged -= OnNodeChanged;
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            Rebuild();
        }

        private void OnNodeChanged(INavigationNode node)
        {
            Rebuild(force: true);
        }

        private void Rebuild(bool force = false)
        {
            if (!grid || !content || !_container || !editingScreen)
            {
                return;
            }

            switch (editingScreen.CurrentMode)
            {
                case AvatarEditorMode.Avatar:
                    _minColumnsToAimFor = _minColumnsToAimForAvatar;
                    break;
                case AvatarEditorMode.Outfit:
                    _minColumnsToAimFor = _minColumnsToAimForGear;
                    break;
            }

            // Available width inside the _container
            float avail = _container.rect.width
                          - grid.padding.left - grid.padding.right
                          - _extraLRPadding.x - _extraLRPadding.y
                          - _reservedScrollbarWidth;

            if (avail <= 0f)
            {
                return;
            }

            // Only recompute on meaningful width change
            if (!force && Mathf.Abs(avail - lastAvailWidth) < 0.5f)
            {
                return;
            }

            lastAvailWidth = avail;

            float spacingX = Mathf.Max(0f, grid.spacing.x);
            float spacingY = Mathf.Max(0f, grid.spacing.y);

            // Start with no scaling
            float scale = 1f;
            float targetW = _baseCellWidth;
            float targetH = _baseCellHeight;

            // Compute how many columns fit with base size
            int cols = Mathf.FloorToInt((avail + spacingX) / (targetW + spacingX));
            cols = Mathf.Max(cols, _minColumnsToAimFor);

            // If too skinny for our minimum desired columns, optionally scale down (but not below _minScale)
            if (_allowScaleDownToFitMinCols && cols < _minColumnsToAimFor)
            {
                int minCols = _minColumnsToAimFor;
                float widthNeededForMinCols = minCols * _baseCellWidth * spacingX;
                scale = Mathf.Clamp(avail / widthNeededForMinCols, _minScale, 1f);
                targetW = _baseCellWidth * scale;
                targetH = _baseCellHeight * scale;

                // Recompute columns with scaled width (might now fit minCols)
                cols = Mathf.FloorToInt((avail + spacingX) / (targetW + spacingX));
                cols = Mathf.Max(cols, _minColumnsToAimFor);
            }

            // Early-out if nothing changed
            if (!force && cols == lastCols
                       && Mathf.Approximately(targetW, lastCellSize.x)
                       && Mathf.Approximately(targetH, lastCellSize.y))
            {
                return;
            }

            // Apply grid settings
            grid.childAlignment = childAlignment;
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = cols;

            // Round to avoid fractional bleed; round H up by 1px to avoid mask clipping
            float cellW = Mathf.Round(targetW);
            float cellH = Mathf.Round(targetH) + 1f;
            grid.cellSize = new Vector2(cellW, cellH);

            // Compute rows and set content height so ScrollRect can actually scroll
            int childCount = 0;
            for (int i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).gameObject.activeInHierarchy)
                {
                    childCount++;
                }
            }

            int rows = Mathf.CeilToInt(childCount / Mathf.Max(1f, cols));
            rows = Mathf.Max(rows, 1);

            float contentHeight = grid.padding.top + grid.padding.bottom
                                                   + rows * grid.cellSize.y
                                                   + (rows - 1) * spacingY;

            // Top-anchored content: sizeDelta.y is the height; keep anchoredPosition.y clamped
            Vector2 sz = content.sizeDelta;
            sz.y = contentHeight;
            content.sizeDelta = sz;

            Vector2 pos = content.anchoredPosition;
            pos.y = Mathf.Clamp(pos.y, 0f, Mathf.Max(0f, contentHeight - (_container.rect.height)));
            content.anchoredPosition = pos;

#if UNITY_EDITOR
            if (!Application.isPlaying) Canvas.ForceUpdateCanvases();
#endif

            lastCols = cols;
            lastCellSize = grid.cellSize;
        }
    }
}
