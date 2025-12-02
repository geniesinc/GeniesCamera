using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Genies.UIFramework
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(GridLayoutGroup))]
    public class AdjustGridLayoutCellSize : MonoBehaviour
    {
        public enum Axis
        {
            X,
            Y
        };

        public enum RatioMode
        {
            Free,
            Fixed
        };

        [FormerlySerializedAs("expand")] [SerializeField]
        private Axis _expand;

        [FormerlySerializedAs("ratioMode")] [SerializeField]
        private RatioMode _ratioMode;

        [FormerlySerializedAs("cellRatio")] [SerializeField]
        private float _cellRatio = 1;

        private RectTransform _transform;
        private GridLayoutGroup _grid;

        private void Awake()
        {
            _transform = (RectTransform)base.transform;
            _grid = GetComponent<GridLayoutGroup>();
        }

        // Start is called before the first frame update
        private void Start()
        {
            UpdateCellSize();
        }

        public void SetSize(float width, float height)
        {
            if (_expand == Axis.X)
            {
                _cellRatio = height / width;
            }
            else
            {
                _cellRatio = width / height;
            }

            UpdateCellSize();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (Application.isPlaying)
            {
                UpdateCellSize();
            }
        }

#if UNITY_EDITOR
        [ExecuteAlways]
        private void Update()
        {
            UpdateCellSize();
        }
#endif

        private void OnValidate()
        {
            _transform = (RectTransform)base.transform;
            _grid = GetComponent<GridLayoutGroup>();
            UpdateCellSize();
        }

        private void UpdateCellSize()
        {
            if (_grid == null)
            {
                return;
            }

            var count = _grid.constraintCount;
            if (_expand == Axis.X)
            {
                float spacing = (count - 1) * _grid.spacing.x;
                float contentSize = _transform.rect.width - _grid.padding.left - _grid.padding.right - spacing;
                float sizePerCell = contentSize / count;
                _grid.cellSize = new Vector2(sizePerCell,
                    _ratioMode == RatioMode.Free ? _grid.cellSize.y : sizePerCell * _cellRatio);
            }
            else //if (expand == Axis.Y)
            {
                float spacing = (count - 1) * _grid.spacing.y;
                float contentSize = _transform.rect.height - _grid.padding.top - _grid.padding.bottom - spacing;
                float sizePerCell = contentSize / count;
                _grid.cellSize = new Vector2(_ratioMode == RatioMode.Free ? _grid.cellSize.x : sizePerCell * _cellRatio,
                    sizePerCell);
            }
        }
    }
}