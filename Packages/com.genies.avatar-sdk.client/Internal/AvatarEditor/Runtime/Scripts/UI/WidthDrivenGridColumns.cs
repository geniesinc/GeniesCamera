using UnityEngine;
using UnityEngine.UI;

namespace Genies.AvatarEditor
{
    [ExecuteAlways]
    [RequireComponent(typeof(GridLayoutGroup))]
#if GENIES_SDK && !GENIES_INTERNAL
    internal class WidthDrivenGridColumns : MonoBehaviour
#else
    public class WidthDrivenGridColumns : MonoBehaviour
#endif
    {
        [Header("Density (UI units, after Canvas Scaler)")]
        [Tooltip("Minimum/target width per tile. Columns are computed so cells are at least this wide.")]
        [SerializeField]
        private float _desiredCellWidth = 120f; // ← set this to your baseline tile width

        [Tooltip("Tile aspect as Height/Width (1 = square, 1.25 = 4:5 card, etc.).")] [SerializeField]
        private float _tileAspect = 1f;

        [Header("Column Limits")] [SerializeField]
        private int _minCols = 2;

        [SerializeField] private int _maxCols = 12;

        [Header("Layout")] [Tooltip("Usually this object's RectTransform; the width we fill.")] [SerializeField]
        private RectTransform _container;

        [Tooltip("Extra L/R padding beyond Grid.padding (e.g., safe area insets).")] [SerializeField]
        private Vector2 _extraLRPadding = Vector2.zero; // x = left, y = right




        private GridLayoutGroup _grid;
        private RectTransform _content;
        private float _lastAvailableWidth = -1f;
        private Vector2 _lastCellSize = new Vector2(-1, -1);
        private float _widthEpsilon = 0.5f;

        private void Awake()
        {
            _grid = GetComponent<GridLayoutGroup>();
            _content = GetComponent<RectTransform>();
            if (!_container)
            {
                _container = _content;
            }
        }

        private void OnEnable() => Apply();

        private void OnRectTransformDimensionsChange()
        {
            Apply();
        }

        private void OnTransformChildrenChanged()
        {
            Apply(force: true);
        } // new/removed cells

        private void Apply(bool force = false)
        {
            if (!_grid || !_content || !_container)
            {
                return;
            }

            // Compute available width
            float avail = _container.rect.width
                          - _grid.padding.left - _grid.padding.right
                          - _extraLRPadding.x - _extraLRPadding.y;

            if (avail <= 0f)
            {
                return;
            }

            // Skip if width hasn't materially changed (prevents scroll snap)
            if (!force && Mathf.Abs(avail - _lastAvailableWidth) < _widthEpsilon)
            {
                return;
            }

            _lastAvailableWidth = avail;

            float spacing = Mathf.Max(0f, _grid.spacing.x);
            float minW = Mathf.Max(1f, _desiredCellWidth);

            // width-driven column count
            int cols = Mathf.FloorToInt((avail + spacing) / (minW + spacing));
            cols = Mathf.Clamp(cols, Mathf.Max(1, _minCols), _maxCols);

            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = cols;

            // Calculate target cell size
            float totalSpacing = spacing * (cols - 1);
            float cellW = (avail - totalSpacing) / cols;
            float cellH = cellW * Mathf.Max(0.0001f, _tileAspect);
            _grid.cellSize = new Vector2(Mathf.Floor(cellW), Mathf.Floor(cellH));

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

            float spacingY = Mathf.Max(0f, _grid.spacing.y);
            float contentHeight = _grid.padding.top + _grid.padding.bottom
                                                    + rows * _grid.cellSize.y
                                                    + (rows - 1) * spacingY;


            // Content must be top-anchored; height is sizeDelta.y
            var size = _content.sizeDelta;
            size.y = contentHeight;
            _content.sizeDelta = size;

            // Ensure top aligned (so first row starts at top)
            var pos = _content.anchoredPosition;
            pos.y = Mathf.Max(0f, pos.y); // clamp to top
            _content.anchoredPosition = pos;
        }
    }
}


