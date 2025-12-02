using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.UI.Scroller;
using Genies.UI.Widgets;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Genies.Customization.Framework.ItemPicker
{
    public class ScrollingItemPicker : MonoBehaviour, IOptimizedScrollerCellSource
    {
        [SerializeField]
        private OptimizedScroller _scrollRect;

        [SerializeField]
        private LayoutGroup _layoutGroup;

        [FormerlySerializedAs("_noneOrNewCtaContainer")]
        [SerializeField]
        private RectTransform _ctaContainer;

        [FormerlySerializedAs("_defaultCtaControls")]
        [SerializeField]
        private NoneOrNewCTAController _ctaControls;

        [SerializeField]
        private LayoutElement _ctaLayoutElement;

        [SerializeField]
        private RectMask2D _rectMask2D;
        private float _defaultOffset;

        public RectTransform Content => _scrollRect.Content;

        public event Action OnScroll
        {
            add
            {
                _scrollRect.ScrollValueChanged += value;
            }
            remove
            {
                _scrollRect.ScrollValueChanged -= value;
            }
        }
        public event Action Hidden;
        public event Action SelectionChanged;
        public event Action OnItemSelected;

        private Dictionary<Transform, CancellationTokenSource> _cellInitializationTasksCts;
        private CancellationTokenSource _currentClickTaskCts, _initializeCountCts, _loadMoreItemsCts;

        private UniTaskCompletionSource _initializeCountCompletionSource;
        private UniTaskCompletionSource<bool> _lastClickTaskCompletionSource;

        private NoneOrNewCTAController _currentCtaController;
        private ItemPickerCellView _currentSelected;
        private ItemPickerCtaConfig _currentCtaConfig;
        private HorizontalLayoutGroup _contentHLG;

        public RectOffset Padding => _layoutGroup.padding;
        public bool IsShowing { get; private set; }
        public IItemPickerDataSource Source { get; private set; }
        protected OptimizedScroller _ScrollRect => _scrollRect;

        protected LayoutGroup _LayoutGroup => _layoutGroup;

        protected int _TotalItemsCount { get; private set; }

        public bool IsInitializingCount { get; private set; }
        public event Action<IItemPickerDataSource> SourceChanged;

        private bool _isPaginationLoadInProgress = false;
        private bool _isInitialSetupComplete = false;
        private const float PaginationTriggerThreshold = 0.8f; // Trigger at 80% scroll

        private void Awake()
        {
            _contentHLG = _scrollRect.Content.GetComponent<HorizontalLayoutGroup>();
        }

        private void OnEnable()
        {
            if (_scrollRect != null)
            {
                _scrollRect.ScrollValueChanged += OnScrollValueChanged;
            }
        }

        private void OnDisable()
        {
            if (_scrollRect != null)
            {
                _scrollRect.ScrollValueChanged -= OnScrollValueChanged;
            }
        }

        private void OnScrollValueChanged()
        {
            CheckForPaginationTrigger().Forget();
        }

        private async UniTaskVoid CheckForPaginationTrigger()
        {
            // Don't trigger pagination during initial setup
            if (!_isInitialSetupComplete)
            {
                return;
            }

            // Only proceed if we have a source with pagination support
            if (Source == null || _isPaginationLoadInProgress || !Source.HasMoreItems || Source.IsLoadingMore)
            {
                return;
            }

            // Check scroll position
            float normalizedPosition = _ScrollRect.GetNormalizedPosition();

            // For horizontal scrolling, trigger near the end (right side)
            // For vertical scrolling, trigger near the bottom
            bool shouldTriggerLoad = _ScrollRect.IsVertical
                ? normalizedPosition <= (1f - PaginationTriggerThreshold)
                : normalizedPosition >= PaginationTriggerThreshold;

            if (!shouldTriggerLoad)
            {
                return;
            }

            _isPaginationLoadInProgress = true;
            try
            {
                // Load more items
                _loadMoreItemsCts = new CancellationTokenSource();
                bool loadedMore = await Source.LoadMoreItemsAsync(_loadMoreItemsCts.Token);

                if (loadedMore)
                {
                    // Update the total count without resetting scroll position
                    await UpdateCountWithoutScrollReset();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading more items during pagination: {e}");
            }
            finally
            {
                _isPaginationLoadInProgress = false;
                _loadMoreItemsCts.Cancel();
                _loadMoreItemsCts = null;
            }
        }

        public async UniTask<GameObject> JumpToIndexAndGet(int index, float time)
        {
            if (index < 0)
            {
                index = 0;
            }

            return await _ScrollRect.ScrollToAndGetElementAt(index, time);
        }

        private void Update()
        {
            UpdateCtaSize();
        }

        private void UpdateCtaSize()
        {
            if (_currentCtaController == null || !_currentCtaController.gameObject.activeSelf || _TotalItemsCount <= 0)
            {
                if (_rectMask2D != null)
                {
                    var prevmaskPadding = _rectMask2D.padding;
                    _rectMask2D.padding = new Vector4(_defaultOffset,
                    prevmaskPadding.y, prevmaskPadding.z, prevmaskPadding.w);
                }

                return;
            }

            bool hasScrollingSpace = _ScrollRect.HasScrollingSpace();
            var collapsedSize = _currentCtaController.GetMaxCollapsedSize(_ScrollRect.IsVertical);
            var expandedSize = _currentCtaController.GetMaxExpandedSize(_ScrollRect.IsVertical);

            if (!hasScrollingSpace || !_currentCtaController.IsCollapsable)
            {
                _currentCtaController.ResizeButtons(1);

                if (_ScrollRect.IsVertical)
                {
                    _ctaLayoutElement.preferredHeight = expandedSize;
                }
                else
                {
                    _ctaLayoutElement.preferredWidth = expandedSize;
                }

                UpdateMask(collapsedSize, expandedSize, 1);

                UpdateContentHorizontalLayoutGroup(expandedSize);

                return;
            }

            float range = 1f / _TotalItemsCount;
            float value = _ScrollRect.GetNormalizedPosition();
            float fill = Mathf.Clamp01(_ScrollRect.IsVertical ? (1 - value) / range : value / range);

            _currentCtaController.ResizeButtons(1 - fill);

            if (_ScrollRect.IsVertical)
            {
                _ctaLayoutElement.preferredHeight = Mathf.Lerp(expandedSize, collapsedSize, fill);
            }
            else
            {
                _ctaLayoutElement.preferredWidth = Mathf.Lerp(expandedSize, collapsedSize, fill);
            }

            UpdateMask(collapsedSize, expandedSize, fill);
            UpdateContentHorizontalLayoutGroup(expandedSize);
        }

        private void UpdateContentHorizontalLayoutGroup(float expandedSize)
        {
            int newValue = (int)expandedSize + (int)_defaultOffset;
            if (_contentHLG != null && _contentHLG.padding.left != newValue)
            {
                _contentHLG.padding.left = (int)expandedSize + (int)_defaultOffset;
                LayoutRebuilder.MarkLayoutForRebuild(Content);
            }
        }

        private void UpdateMask(float collapsedSize, float expandedSize, float fill)
        {
            if (_rectMask2D != null)
            {
                var prevmaskPadding = _rectMask2D.padding;
                _rectMask2D.padding = new Vector4(
                    (int)Mathf.Lerp(expandedSize, collapsedSize, fill) + _defaultOffset,
                    prevmaskPadding.y, prevmaskPadding.z, prevmaskPadding.w);
            }
        }

        /// <summary>
        /// Initialize the item picker with its data source. This will also handle refilling the new cells.
        /// </summary>
        /// <param name="source"> The data source for the item picker cells </param>
        public async UniTaskVoid Show(IItemPickerDataSource source)
        {
            CancelCellInitializationTasks();
            CancelClickTasks();

            if (Source != null)
            {
                Hide();
            }

            if (source == null)
            {
                Hide();
                return;
            }

            IsShowing = true;
            gameObject.SetActive(true);

            IsInitializingCount = true;
            _isInitialSetupComplete = false;
            _TotalItemsCount = -1;
            Source = source;
            ConfigureLayout();
            ConfigureCta();

            _ScrollRect.SetSource(this);

            //Initialize count
            await InitializeCountAsync();

            IsInitializingCount = false;

            var currentSelected = GetCurrentSelectedIndex();

            if (_currentCtaController != null)
            {
                _currentCtaController.SetNoneSelected(currentSelected < 0);
            }

            // Don't auto-scroll to selected item on initialization
            _ScrollRect.FillCells(_TotalItemsCount, -1);

            // Mark initial setup as complete after FillCells to prevent pagination from triggering during setup
            _isInitialSetupComplete = true;

            // Auto-scroll behavior removed - items will start at the top
            // if (currentSelected > 0)
            // {
            //     FixItemPositioningForGridLayout(currentSelected).Forget();
            // }

            SourceChanged?.Invoke(source);
        }

        private async UniTask InitializeCountAsync()
        {
            if (_initializeCountCompletionSource != null)
            {
                await _initializeCountCompletionSource.Task;
                return;
            }

            try
            {
                _initializeCountCompletionSource = new UniTaskCompletionSource();
                _initializeCountCts = new CancellationTokenSource();

                _TotalItemsCount = await Source.InitializeAndGetCountAsync(_initializeCountCts.Token);
            }
            finally
            {
                _initializeCountCompletionSource?.TrySetResult();
                _initializeCountCompletionSource = null;
            }
        }

        /// <summary>
        /// Clear all cells. (Despawn)
        /// </summary>
        public void Hide()
        {
            CancelCellInitializationTasks();
            CancelClickTasks();

            IsShowing = false;
            _isInitialSetupComplete = false;

            if (_currentSelected != null)
            {
                _currentSelected.ToggleSelected(false);
                _currentSelected = null;
            }

            _ScrollRect.ClearCells();
            Source = null;

            _lastClickTaskCompletionSource = null;


            gameObject.SetActive(false);

            Hidden?.Invoke();
            SourceChanged?.Invoke(null);
        }

        protected void OnHidden()
        {
            Hidden?.Invoke();
        }


        /// <summary>
        /// Toggles the selection off on the current selected.
        /// </summary>
        private void ClearCurrentSelected()
        {
            if (_currentSelected != null)
            {
                _currentSelected.ToggleSelected(false);
                _currentSelected = null;
            }

            if (_currentCtaController != null)
            {
                _currentCtaController.SetNoneSelected(GetCurrentSelectedIndex() < 0);
            }
        }

        /// <summary>
        /// Fixes positioning the current selected cell in view after filling the cells
        /// </summary>
        public async UniTask FixItemPositioningForGridLayout(int targetIndex)
        {
            // Wait for the layout to be ready
            const int maxWaitFrames = 30;
            int frameCount = 0;

            while (frameCount < maxWaitFrames)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
                frameCount++;

                // Check if the layout is ready
                if (Content != null && Content.rect.width > 0 && Content.rect.height > 0 &&
                    _LayoutGroup != null && gameObject.activeInHierarchy)
                {
                    // Force layout rebuild to ensure proper positioning
                    LayoutRebuilder.ForceRebuildLayoutImmediate(Content);
                    break;
                }
            }

            // Now fix the positioning using our corrected JumpToIndexAndGet method
            await JumpToIndexAndGet(targetIndex, 0.1f);
        }

        /// <summary>
        /// Jump to current selected if it changed and show it as selected.
        /// </summary>
        public async UniTaskVoid RefreshSelection()
        {
            if (Source == null || IsInitializingCount)
            {
                return;
            }

            var currentIndex = GetCurrentSelectedIndex();

            if (currentIndex <= -1)
            {
                ClearCurrentSelected();
                return;
            }

            var cellAtIndex = _ScrollRect.GetCellAt(currentIndex);

            //Its not in view and wasn't initialized, so we just scroll to it.
            if (cellAtIndex == null)
            {
                ClearCurrentSelected();
                await JumpToIndexAndGet(currentIndex, 0.1f);
                return;
            }

            cellAtIndex = await JumpToIndexAndGet(currentIndex, 0.1f);
            var cellAtIndexView = ItemPickerCellPool.GetCachedItemCellViewComponent(cellAtIndex.transform);

            if (_currentSelected != null)
            {
                if (cellAtIndexView == _currentSelected)
                {
                    return;
                }
            }

            ClearCurrentSelected();
            _currentSelected = cellAtIndexView;
            _currentSelected.ToggleSelected(true);
        }

        /// <summary>
        /// Re-initialize cells.
        /// </summary>
        public async UniTask RefreshData()
        {
            if (Source == null)
            {
                return;
            }

            CancelClickTasks();
            CancelCellInitializationTasks();
            ClearCurrentSelected();

            IsInitializingCount = true;
            _isInitialSetupComplete = false;
            await InitializeCountAsync();

            IsInitializingCount = false;
            _ScrollRect.FillCells(_TotalItemsCount, GetCurrentSelectedIndex());
            _isInitialSetupComplete = true;
        }

        /// <summary>
        /// Update the item count for pagination without resetting scroll position
        /// </summary>
        private async UniTask UpdateCountWithoutScrollReset()
        {
            if (Source == null)
            {
                return;
            }

            var previousCount = _TotalItemsCount;

            IsInitializingCount = true;
            await InitializeCountAsync();
            IsInitializingCount = false;

            // If count increased, add new cell holders to the scroller
            if (_TotalItemsCount > previousCount)
            {
                // The scroller will handle the new items when it needs them
                // We just need to update the internal count
                _ScrollRect.UpdateItemCount(_TotalItemsCount);
            }
        }

        /// <summary>
        /// Setup the CTA controller
        /// </summary>
        private void ConfigureCta()
        {
            _currentCtaConfig = Source?.GetCtaConfig();

            //Destroy old CTA if its not the default one
            if (_currentCtaController != null && _currentCtaController != _ctaControls)
            {
                Destroy(_currentCtaController.gameObject);
            }
            else if (_currentCtaController != null)
            {
                _currentCtaController.gameObject.SetActive(false);
            }

            //No CTAs
            if (_currentCtaConfig == null)
            {
                if (_ctaContainer != null)
                {
                    _ctaContainer.gameObject.SetActive(false);

                    if (_contentHLG != null)
                    {
                        _contentHLG.padding.left = (int)_defaultOffset;
                    }

                    if (_rectMask2D != null)
                    {
                        var prevmaskPadding = _rectMask2D.padding;
                        _rectMask2D.padding = new Vector4(_defaultOffset,
                        prevmaskPadding.y, prevmaskPadding.z, prevmaskPadding.w);
                    }
                }

                return;
            }

            if (_ctaContainer != null)
            {
                _ctaContainer.gameObject.SetActive(true);
            }

            var overridePrefab = _LayoutGroup is GridLayoutGroup ? _currentCtaConfig.GridLayoutCtaOverride : _currentCtaConfig.HorizontalLayoutCtaOverride;

            //Setup new CTAs
            if (overridePrefab != null)
            {
                _currentCtaController = Instantiate(overridePrefab, _ctaContainer, worldPositionStays: false);
            }
            else
            {
                _currentCtaController = _ctaControls;
            }

            //Null CTAs just disable and return
            if (_currentCtaController == null)
            {
                _ctaContainer.gameObject.SetActive(false);

                if (_contentHLG != null)
                {
                    _contentHLG.padding.left = (int)_defaultOffset;
                }

                return;
            }

            _currentCtaController.SetAndEnableActiveCTAButton(_currentCtaConfig.CtaType);

            //Unsubscribe
            _currentCtaController.NoneSelected -= OnNoneCtaClicked;
            _currentCtaController.CreateNewSelected -= OnCreateNewCtaClicked;

            //Resubscribe
            _currentCtaController.NoneSelected += OnNoneCtaClicked;
            _currentCtaController.CreateNewSelected += OnCreateNewCtaClicked;

            _currentCtaController.gameObject.SetActive(true);
        }

        private void OnCreateNewCtaClicked()
        {
            _currentCtaConfig?.CreateNewAction?.Invoke();
        }

        private void OnNoneCtaClicked()
        {
            async UniTask NoneCtaClick()
            {
                if (_currentCtaConfig?.NoneSelectedAsync == null)
                {
                    return;
                }

                CancelClickTasks();

                _currentClickTaskCts = new CancellationTokenSource();
                var cancellationToken = _currentClickTaskCts.Token;

                var succeeded = await _currentCtaConfig.NoneSelectedAsync.Invoke(cancellationToken);

                if (!succeeded)
                {
                    return;
                }


                if (_currentSelected != null)
                {
                    //Deselect current
                    _currentSelected.ToggleSelected(false);
                    _currentSelected.ToggleProcessingBadge(false);
                    _currentSelected = null;
                }

                _currentCtaController.SetNoneSelected(true);
                SelectionChanged?.Invoke();
            }

            NoneCtaClick().Forget();
        }

        /// <summary>
        /// Setup the layout for this source
        /// </summary>
        private void ConfigureLayout()
        {
            var layoutConfig = Source?.GetLayoutConfig();

            if (layoutConfig == null)
            {
                return;
            }

            var group = _LayoutGroup;

            if (group == null)
            {
                return;
            }

            var asGrid = group as GridLayoutGroup;
            if (asGrid != null)
            {
                var gridConfig = layoutConfig.gridLayoutConfig;
                asGrid.spacing = gridConfig.spacing;
                asGrid.constraintCount = gridConfig.columnCount;
                asGrid.cellSize = gridConfig.cellSize;
                asGrid.padding = gridConfig.padding;
                return;
            }

            var asHorizontalOrVertical = group as HorizontalOrVerticalLayoutGroup;
            if (asHorizontalOrVertical != null)
            {
                var horizontalOrVerticalConfig = layoutConfig.horizontalOrVerticalLayoutConfig;
                asHorizontalOrVertical.spacing = horizontalOrVerticalConfig.spacing;
                asHorizontalOrVertical.padding = horizontalOrVerticalConfig.padding;
            }

            _defaultOffset = _defaultOffset == 0 ? asHorizontalOrVertical.padding.left : _defaultOffset;
        }

        public int GetCurrentSelectedIndex()
        {
            if (Source == null)
            {
                return -1;
            }

            //Snap jump to target
            var selectedIndex = Source.GetCurrentSelectedIndex();
            return selectedIndex;
        }

        private bool ItemSelectedIsValidForProcessCTA()
        {
            if (Source == null)
            {
                return false;
            }

            var isValidForCTA = Source.ItemSelectedIsValidForProcessCTA();
            return isValidForCTA;
        }

        /// <summary>
        /// Cancel initialization tasks
        /// </summary>
        private void CancelCellInitializationTasks()
        {
            if (_cellInitializationTasksCts == null)
            {
                _cellInitializationTasksCts = new Dictionary<Transform, CancellationTokenSource>();
                return;
            }

            foreach (var kvp in _cellInitializationTasksCts)
            {
                kvp.Value?.Cancel();
                kvp.Value?.Dispose();
            }

            _cellInitializationTasksCts.Clear();

            _initializeCountCts?.Cancel();
            _initializeCountCts?.Dispose();
            _initializeCountCts = null;
        }

        /// <summary>
        /// Cancel click tasks
        /// </summary>
        private void CancelClickTasks()
        {
            _currentClickTaskCts?.Cancel();
            _currentClickTaskCts?.Dispose();
            _currentClickTaskCts = null;
        }

        /// <summary>
        /// This is a method from <see cref="IItemPickerDataSource"/> and its purpose is to initialize the spawned
        /// scroll rect item. The control for initialization is inverted to the <see cref="IItemPickerDataSource"/>
        /// </summary>
        /// <param name="cell"> The cell to initialized </param>
        /// <param name="idx"> The view index of the cell to initialize </param>
        public void InitializeCell(int idx, GameObject cell)
        {
            var cellTransform = cell.transform;
            var itemCell = ItemPickerCellPool.GetCachedItemCellViewComponent(cellTransform);

            //Setup cancellation
            if (_cellInitializationTasksCts.TryGetValue(cellTransform, out var cts))
            {
                cts?.Cancel();
                cts?.Dispose();
                cts = new CancellationTokenSource();
                _cellInitializationTasksCts[cellTransform] = cts;
            }
            else
            {
                cts = new CancellationTokenSource();
                _cellInitializationTasksCts.Add(cellTransform, cts);
            }

            //Await initialization
            if (IsInitializingCount)
            {
                itemCell.SetState(ItemCellState.NotInitialized);
                return;
            }

            itemCell.Initialize(
                                new ItemPickerCellData()
                                {
                                    OnClicked = () =>
                                    {
                                        OnItemCellClicked(idx, itemCell).Forget();
                                    }
                                }
                               );

            InitializeCellViewAsync(itemCell, idx, cts.Token).Forget();


            if (_currentCtaController != null)
            {
                _currentCtaController.SetCTAActive(ItemSelectedIsValidForProcessCTA());
            }
        }

        /// <summary>
        /// Initialize the cell's view async, the <see cref="IItemPickerDataSource.InitializeCellViewAsync"/> will handle
        /// the initialization logic.
        /// </summary>
        /// <param name="view"> The cell view to initialize </param>
        /// <param name="idx"> The index of the cell view </param>
        /// <param name="token"> The cancellation token </param>
        private async UniTask InitializeCellViewAsync(ItemPickerCellView view, int idx, CancellationToken token)
        {
            //Set the view state to initializing (usually this means we're waiting for it to load).
            view.SetState(ItemCellState.NotInitialized);

            //Set the cell selected if it matches the current selected index.
            var isSelected = GetCurrentSelectedIndex() == idx;

            //If the initialization was successful
            var didInitialize = await Source.InitializeCellViewAsync(view, idx, isSelected, token);

            //Request cancelled, no need to continue.
            if (token.IsCancellationRequested)
            {
                return;
            }


            //If the cell was initialized successfully, then set its state to initialized and
            //set as the current selected if it is.
            if (didInitialize)
            {
                isSelected = GetCurrentSelectedIndex() == idx;
                view.SetState(ItemCellState.Initialized);
                view.ToggleSelected(isSelected);

                if (isSelected)
                {
                    _currentSelected = view;
                }
            }
        }

        /// <summary>
        /// Get cell from pool
        /// </summary>
        /// <param name="index"> The index of the cell </param>
        /// <returns></returns>
        public GameObject GetCellInstance(int index)
        {
            if (Source != null)
            {
                var prefab = Source.GetCellPrefab(index);
                return ItemPickerCellPool.GetFromPool(prefab);
            }

            return null;
        }

        /// <summary>
        /// Return the cell to the pool
        /// </summary>
        /// <param name="index"> index of the cell </param>
        /// <param name="cell"> the cell game object </param>
        public void ReturnCellInstance(int index, GameObject cell)
        {
            var forTransform = cell.transform;

            //Cancel initialization and dispose
            if (_cellInitializationTasksCts.TryGetValue(forTransform, out var cts))
            {
                if (cts.Token.CanBeCanceled)
                {
                    cts?.Cancel();
                    cts?.Dispose();
                    _cellInitializationTasksCts.Remove(forTransform);
                }
            }

            //Since we're pooling our objects we should just null this since
            //it's out of view. When we scroll back to the index it will be
            //selected again any way.
            if (_currentSelected != null && forTransform == _currentSelected.transform)
            {
                ClearCurrentSelected();
            }

            var cellView = ItemPickerCellPool.GetCachedItemCellViewComponent(forTransform);
            Source.DisposeCellViewAsync(cellView, index);
            var prefab = Source.GetCellPrefab(index);
            ItemPickerCellPool.ReturnToPool(prefab, forTransform.gameObject);
        }

        /// <summary>
        /// Get the size of the cell at index.
        /// </summary>
        /// <param name="index"> The index of the cell </param>
        public Vector2 GetCellSize(int index)
        {
            if (_LayoutGroup is GridLayoutGroup asGrid)
            {
                return asGrid.cellSize;
            }

            if (Source == null)
            {
                return Vector2.zero;
            }

            return Source.GetCellSize(index);
        }

        /// <summary>
        /// When the cell is clicked, this method will handle the view states and task cancellation.
        /// The logic for the click comes from <see cref="IItemPickerDataSource"/>
        /// </summary>
        /// <param name="index"> The index of the cell </param>
        /// <param name="candidateCell"> The clicked cell view </param>
        private async UniTask OnItemCellClicked(int index, ItemPickerCellView candidateCell)
        {
            if (IsInitializingCount)
            {
                return;
            }

            //Get the click action from the source
            var isCurrentlySelected = candidateCell == _currentSelected;

            CancellationToken cancellationToken;

            //If it's a new click or the last selected is done processing
            if (!isCurrentlySelected || _lastClickTaskCompletionSource == null)
            {
                //Cancel current cell click if another happened.
                CancelClickTasks();
                _currentClickTaskCts = new CancellationTokenSource();
                cancellationToken = _currentClickTaskCts.Token;
                _lastClickTaskCompletionSource = null;
            }
            //If it's currently selected and still processing, return.
            else if (_lastClickTaskCompletionSource != null)
            {
                return;
            }

            OnItemSelected?.Invoke();
            var onClick = Source.OnItemClickedAsync(index, candidateCell, isCurrentlySelected, cancellationToken);

            //Just run the click action again
            if (isCurrentlySelected)
            {
                //Show processing view
                candidateCell.ToggleProcessingBadge(true);
                await onClick;

                if (_currentCtaController != null)
                {
                    _currentCtaController.SetNoneSelected(GetCurrentSelectedIndex() < 0);
                }

                //Hide processing view
                candidateCell.ToggleProcessingBadge(false);
                return;
            }

            if (_currentSelected != null)
            {
                _currentSelected.ToggleSelected(false);
                _currentSelected.ToggleProcessingBadge(false);
            }

            //Cache previous selected
            var previous = _currentSelected;

            //Set current selected
            _currentSelected = candidateCell;
            _lastClickTaskCompletionSource = new UniTaskCompletionSource<bool>();

            //Show selected
            candidateCell.ToggleSelected(true);

            //Show processing view
            candidateCell.ToggleProcessingBadge(true);

            //Run action and check if it was successful
            var didSucceed = await onClick;

            //Hide processing view
            candidateCell.ToggleProcessingBadge(false);

            if (_currentCtaController != null)
            {
                _currentCtaController.SetNoneSelected(GetCurrentSelectedIndex() < 0);
                _currentCtaController.SetCTAActive(ItemSelectedIsValidForProcessCTA());
            }

            //If it was cancelled, we return.
            if (cancellationToken.IsCancellationRequested)
            {
                _lastClickTaskCompletionSource = null;
                return;
            }

            if (_lastClickTaskCompletionSource != null)
            {
                _lastClickTaskCompletionSource.TrySetResult(didSucceed);
                _lastClickTaskCompletionSource = null;
            }

            //If the action didn't succeed, we deselect the clicked cell and set the previous selected.
            if (!didSucceed)
            {
                candidateCell.ToggleSelected(false);
                candidateCell.ToggleProcessingBadge(false);

                if (previous != null)
                {
                    previous.ToggleSelected(true);
                }

                _currentSelected = previous;
                return;
            }

            await JumpToIndexAndGet(index, 0.5f);
            SelectionChanged?.Invoke();
        }
    }
}
