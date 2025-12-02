using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.UI.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.UI.Scroller
{
    /// <summary>
    /// Optimizes a scroll rect by using a concept of cell holders, holders are empty game objects that help
    /// with the layout of the content. Right now the biggest hit would be on instantiating those holders, but they are
    /// pooled. We check if a holder is visible or not using view port calculations and if it is visible we send a request
    /// to <see cref="IOptimizedScrollerCellSource"/> to return the cell view to show and parent it to the holder. If it goes
    /// out of view we return it to the <see cref="IOptimizedScrollerCellSource"/>
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class OptimizedScroller : MonoBehaviour
    {
        [SerializeField]
        private ScrollRect _scrollRect;

        public event Action ScrollValueChanged;
        private IOptimizedScrollerCellSource _source;
        private List<OptimizedScrollerCellHolder> _cellHolders = new List<OptimizedScrollerCellHolder>();
        private Rect? _previousViewPortRect = null;
        private Rect? _previousContentRect = null;

        public int TotalCount { get; private set; } = 0;

        private RectTransform _scrollRectTransform { get; set; }
        public RectTransform Content => _scrollRect.content;
        private RectTransform _viewPort => _scrollRect.viewport ? _scrollRect.viewport : _scrollRectTransform;
        public bool IsVertical => _scrollRect.vertical;

        private void Awake()
        {
            _scrollRectTransform = _scrollRect.transform as RectTransform;
        }

        private void OnEnable()
        {
            _scrollRect.onValueChanged.AddListener(OnScroll);
        }

        private void OnDisable()
        {
            _scrollRect.onValueChanged.RemoveListener(OnScroll);
        }

        /// <summary>
        /// Get the scrolling normalized position
        /// </summary>
        /// <returns></returns>
        public float GetNormalizedPosition()
        {
            var normalizedPos = _scrollRect.normalizedPosition;
            if (_scrollRect.vertical)
            {
                return normalizedPos.y;
            }

            return normalizedPos.x;
        }

        /// <summary>
        /// Returns the _scrollRect, this allows being able to access its OnValueChange method and other functionality such as disable/enable scrolling.
        /// </summary>
        public ScrollRect GetScrollRect()
        {
            return _scrollRect;
        }

        /// <summary>
        /// Returns the cell GameObject at given index, this might be null if the holder isn't in view.
        /// </summary>
        /// <param name="index"> The cell index </param>
        public GameObject GetCellAt(int index)
        {
            if (index < 0 || TotalCount - 1 < index)
            {
                CrashReporter.Log("Tried to get an element that is out of bounds", LogSeverity.Warning);
                return null;
            }

            return _cellHolders[index].HeldCellView;
        }

        /// <summary>
        /// Scroll to the element and return the loaded cell view.
        /// </summary>
        /// <param name="index"> Index to scroll to </param>
        /// <param name="duration"> Scroll duration </param>
        /// <returns></returns>
        public async UniTask<GameObject> ScrollToAndGetElementAt(int index, float duration = 0.1f)
        {
            // ensure index is within the valid range.
            index = Mathf.Clamp(index, 0, TotalCount-1);

            if (TotalCount == 0)
            {
                return null;
            }

            RebuildLayout();

            var target = _cellHolders[index];

            var normalizedPos = _scrollRect.normalizedPosition;

            if (_scrollRect.vertical)
            {
                var targetNormalizedYPos = _scrollRect.GetScrollToCenterNormalizedPosition(target.rectTransform);
                normalizedPos.y = targetNormalizedYPos;
            }
            else
            {
                var targetNormalizedXPos = _scrollRect.GetScrollToCenterNormalizedPosition(target.rectTransform, RectTransform.Axis.Horizontal);
                normalizedPos.x = targetNormalizedXPos;
            }

            if (duration <= 0)
            {
                _scrollRect.normalizedPosition = normalizedPos;
            }
            else
            {
                await _scrollRect.AnimateNormalizedPos(normalizedPos, duration).SetEase(Ease.InOutSine).AsyncWaitForCompletion();
            }

            //Ensure we have the target element cell
            UpdateVisibleChildren();
            return target.HeldCellView;
        }

        /// <summary>
        /// Set the data source for this scroller
        /// </summary>
        /// <param name="source"> The data source </param>
        public void SetSource(IOptimizedScrollerCellSource source)
        {
            _source = source;
        }

        /// <summary>
        /// Refills the cells with the given count and snaps to the given
        /// index
        /// </summary>
        /// <param name="count"> Count of cells to create </param>
        /// <param name="startIndex"> Index of the first cell </param>
        public void FillCells(int count, int startIndex = 0)
        {
            if (_source == null)
            {
                Debug.LogError("Can't use the scroller without specifying a source first");
                return;
            }

            startIndex = Mathf.Max(startIndex, 0);

            //Remove previous cells
            ClearCells();

            var pos = Content.anchoredPosition;

            if (_scrollRect.vertical)
            {
                pos.y = 0;
            }
            else
            {
                pos.x = 0;
            }

            Content.anchoredPosition = pos;

            TotalCount = count;
            for (int i = 0; i < TotalCount; i++)
            {
                var cellHolder = OptimizedScrollerFactory.GetCellHolderFromPool(_source.GetCellSize(i), Content);
                cellHolder.rectTransform.SetParent(Content);
                cellHolder.rectTransform.localScale = Vector3.one;
                _cellHolders.Add(cellHolder);
            }

            ScrollToAndGetElementAt(startIndex, 0).Forget();
        }

        /// <summary>
        /// Update the item count dynamically (for pagination) without resetting scroll position
        /// </summary>
        /// <param name="newCount">The new total count of items</param>
        public void UpdateItemCount(int newCount)
        {
            if (_source == null)
            {
                Debug.LogError("Can't update count without a source");
                return;
            }

            if (newCount <= TotalCount)
            {
                // Count didn't increase, nothing to do
                return;
            }

            // Save the current scroll position
            var savedPosition = Content.anchoredPosition;



            // Add new cell holders for the additional items
            for (int i = TotalCount; i < newCount; i++)
            {
                var cellSize = _source.GetCellSize(i);
                var cellHolder = OptimizedScrollerFactory.GetCellHolderFromPool(cellSize, Content);
                cellHolder.rectTransform.SetParent(Content, false); // false = worldPositionStays
                cellHolder.rectTransform.localScale = Vector3.one;
                cellHolder.rectTransform.localRotation = Quaternion.identity;

                _cellHolders.Add(cellHolder);
            }

            TotalCount = newCount;

            // Restore the scroll position
            Content.anchoredPosition = savedPosition;

            // Let LateUpdate handle the visibility check naturally on next frame
            // This prevents conflicts with layout recalculation
        }

        /// <summary>
        /// Clears the cells and returns them to the pool
        /// </summary>
        public void ClearCells()
        {
            Debug.Assert(TotalCount == _cellHolders.Count, "Count mismatch! something went wrong somewhere");

            TotalCount = 0;
            for (int i = _cellHolders.Count - 1; i >= 0; i--)
            {
                var holder   = _cellHolders[i];
                var cellView = holder.HeldCellView;

                if (cellView != null)
                {
                    holder.SetCellView(null);
                    _source.ReturnCellInstance(i, cellView);
                }

                _cellHolders.Remove(holder);
                OptimizedScrollerFactory.ReturnToPool(holder);
            }

            RebuildLayout();
        }

        /// <summary>
        /// Ensures the layout is correctly rebuilt
        /// </summary>
        /// <param name="isImmediate"> If the layout should be rebuilt immediately </param>
        private void RebuildLayout(bool isImmediate = true)
        {
            if (CanvasUpdateRegistry.IsRebuildingLayout())
            {
                return;
            }

            if (isImmediate)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
            }
            else
            {
                LayoutRebuilder.MarkLayoutForRebuild(Content);
            }
        }

        /// <summary>
        /// Loop through children and check which one is active and which one isn't.
        ///
        /// TODO: Calculate the start index and end index instead of looping through all children using content/viewport positions.
        /// </summary>
        private void UpdateVisibleChildren()
        {
            if (_cellHolders.Count == 0 || TotalCount == 0)
            {
                return;
            }

            for (int i = 0; i < TotalCount; i++)
            {
                var cellHolder = _cellHolders[i];

                if (_viewPort.FullyContains(cellHolder.rectTransform, cellHolder.Size * 2f))
                {
                    if (cellHolder.HeldCellView == null)
                    {
                        var newCell = _source.GetCellInstance(i);
                        if (newCell != null)
                        {
                            newCell.transform.SetParent(cellHolder.rectTransform, false);
                            newCell.transform.localScale = Vector3.one;
                            cellHolder.SetCellView(newCell);

                            RectTransform cellRt = newCell.transform as RectTransform;
                            if (cellRt != null)
                            {
                                cellRt.pivot = new Vector2(0.5f, 0.5f);
                                cellRt.anchorMin = Vector2.zero;
                                cellRt.anchorMax = Vector2.one;
                                cellRt.sizeDelta = Vector2.zero;
                                cellRt.localPosition = Vector3.zero;
                            }


                            _source.InitializeCell(i, newCell);
                        }
                    }

                }
                else
                {
                    if (cellHolder.HeldCellView != null)
                    {
                        _source.ReturnCellInstance(i, cellHolder.HeldCellView);
                        cellHolder.SetCellView(null);
                    }
                }

                //Animate
                var bounds                       = cellHolder.rectTransform.TransformBoundsTo(_viewPort);
                var normalizedPositionInViewPort = Rect.PointToNormalized(_viewPort.rect, bounds.center);
                cellHolder.NormalizedPositionChanged(_scrollRect.vertical ? normalizedPositionInViewPort.y : normalizedPositionInViewPort.x);
            }
        }

        private void OnScroll(Vector2 arg0)
        {
            UpdateVisibleChildren();
            ScrollValueChanged?.Invoke();
        }

        private void LateUpdate()
        {
            if (_viewPort.rect.Equals(_previousViewPortRect) && Content.rect.Equals(_previousContentRect))
            {
                return;
            }

            UpdateVisibleChildren();
            _previousViewPortRect = _viewPort.rect;
            _previousContentRect = Content.rect;
        }

        public bool HasScrollingSpace()
        {
            return IsVertical ? Content.rect.height > _viewPort.rect.height : Content.rect.width > _viewPort.rect.width;
        }
    }
}

