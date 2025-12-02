using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.Customization.Framework.Actions;
using Genies.Customization.Framework.ItemPicker;
using Genies.Customization.Framework.Navigation;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Customization.Framework
{
    /// <summary>
    ///     The customizer is a framework for customizing any entity, it relies on 4 main features
    ///     - Navigation using <see cref="NavigationGraph" /> and <see cref="INavigationNode" />
    ///     - <see cref="CustomizationConfig" /> each node is associated with a customization controller.
    ///     - <see cref="ActionBar" /> A bar for the actions the user can take. ex: undo/redo, save, exit, etc...
    ///     - <see cref="NavBar" /> A navigation bar to transition between <see cref="INavigationNode" />
    ///     This class handles dispatching events and navigation.
    /// </summary>
    public class Customizer : MonoBehaviour
    {
        [SerializeField]
        private bool _debugMode;

        [SerializeField]
        private TextMeshProUGUI _nodeDebugText;

        [SerializeField]
        private NavigationGraph _navGraph;

        [FormerlySerializedAs("_animator")]
        [SerializeField]
        private CustomizerViewBase _view;

        [SerializeField]
        private CanvasGroup _canvasGroup;

        private readonly Stack<NavigationStack> _navigationStacks = new Stack<NavigationStack>();

        private INavigationNode _currentNode;
        public INavigationNode CurrentNode => _currentNode;

        public bool IsEditing { get; private set; }
        private NavigationStack _CurrentNavigationStack => _navigationStacks.Count > 0 ? _navigationStacks.Peek() : null;
        public CustomizerViewBase View => _view;
        public bool HasEditingNode => _currentNode?.EditItemNode != null;
        public bool HasCreateNode => _currentNode?.CreateItemNode != null;

        private void Awake()
        {
            IsEditing = false;
            _view.AnimateOut(true).Forget();
        }

        public event Action SaveRequested;
        public event Action ShareRequested;
        public event Action CreateRequested;
        public event Action ExitRequested;
        public event Action EditFinished;
        public event Action<INavigationNode> NodeChanged;

        /// <summary>
        ///     Animate in and navigate to the root node.
        /// </summary>
        public UniTask StartCustomization(NavigationGraph navGraph)
        {
            if (IsEditing)
            {
                return UniTask.CompletedTask;
            }

            if (navGraph == null)
            {
                CrashReporter.Log("No nav graph assigned to the customizer");
            }

            _navGraph = navGraph;
            _view.Initialize(this);

            Bind();
            GoToRoot();

            IsEditing = true;
            return UniTask.CompletedTask;
        }

        /// <summary>
        ///     Animate out and dispose the customizer.
        /// </summary>
        public async UniTask StopCustomization()
        {
            if (!IsEditing)
            {
                return;
            }

            Unbind();

            if (_view != null)
            {
                await _view.AnimateOut();
            }

            _currentNode?.Controller?.StopCustomization();
            _currentNode?.Controller?.Dispose();
            _currentNode = null;

            _navigationStacks.Clear();
            _view?.Dispose();
            ItemPickerCellPool.Dispose();
            IsEditing = false;
            _nodeDebugText.gameObject.SetActive(false);

        }

        /// <summary>
        ///     Bind events
        /// </summary>
        private void Bind()
        {
            _view.ActionBar.RedoRequested += OnRedo;
            _view.ActionBar.UndoRequested += OnUndo;
            _view.ActionBar.SaveRequested += OnSaveRequested;
            _view.ActionBar.ShareRequested += OnShareRequested;
            _view.ActionBar.CreateRequested += OnCreateRequested;
            _view.ActionBar.SubmitRequested += OnSubmitRequested;
            _view.ActionBar.ExitRequested += OnExitRequested;
            _view.ActionBar.ResetAllRequested += ResetAllRequested;
        }

        /// <summary>
        ///     Unbind all events
        /// </summary>
        private void Unbind()
        {
            _view.ActionBar.RedoRequested -= OnRedo;
            _view.ActionBar.UndoRequested -= OnUndo;
            _view.ActionBar.SaveRequested -= OnSaveRequested;
            _view.ActionBar.ShareRequested -= OnShareRequested;
            _view.ActionBar.CreateRequested -= OnCreateRequested;
            _view.ActionBar.SubmitRequested -= OnSubmitRequested;
            _view.ActionBar.ExitRequested -= OnExitRequested;
            _view.ActionBar.ResetAllRequested -= ResetAllRequested;
        }

        #region Actions

        /// <summary>
        ///     Register an undoable <see cref="ICommand" />
        /// </summary>
        /// <param name="command"> The command to register </param>
        /// <param name="onMainStack">Force a command to be registered on the main stack </param>
        public void RegisterCommand(ICommand command, bool onMainStack = false)
        {
            if (onMainStack)
            {
                NavigationStack rootStack = _navigationStacks.ToArray()[_navigationStacks.Count - 1];
                rootStack.RegisterCommand(command);
                return;
            }

            _CurrentNavigationStack.RegisterCommand(command);
            _view.OnUndoRedo(_CurrentNavigationStack.HasUndo(), _CurrentNavigationStack.HasRedo());
        }

        /// <summary>
        /// Removes all items from the current undo-redo stack
        /// </summary>
        public void ClearUndoRedo()
        {
            _CurrentNavigationStack.ClearUndoRedoService();
        }

        /// <summary>
        ///     Handle executing the undo logic and dispatch events to <see cref="CustomizationConfig" />
        /// </summary>
        private void OnUndo()
        {
            async UniTask UndoAsync()
            {
                _canvasGroup.blocksRaycasts = false;
                await _CurrentNavigationStack.UndoCommand();
                _canvasGroup.blocksRaycasts = true;
                _currentNode.Controller.OnUndoRedo();
            }

            UndoAsync().Forget();
            _view.OnUndoRedo(_CurrentNavigationStack.HasUndo(), _CurrentNavigationStack.HasRedo());
        }

        /// <summary>
        ///     Handle executing the redo logic and dispatch events to <see cref="CustomizationConfig" />
        /// </summary>
        private void OnRedo()
        {
            async UniTask RedoAsync()
            {
                _canvasGroup.blocksRaycasts = false;
                await _CurrentNavigationStack.RedoCommand();
                _canvasGroup.blocksRaycasts = true;
                _currentNode.Controller.OnUndoRedo();
            }

            RedoAsync().Forget();
            _view.OnUndoRedo(_CurrentNavigationStack.HasUndo(), _CurrentNavigationStack.HasRedo());
        }

        /// <summary>
        ///     When the user is requesting to exit the customizer. The logic for exiting saving is the following:
        ///     1) Traverse through every <see cref="NavigationStack" /> and every node and find if any node has a custom exit
        ///     action.
        ///     2) If the node exit logic was successful, the traversal ends there and we open the previous node.
        ///     3) If the user dismissed the exit logic, we stop the traversal and open the node that interrupted the traversal.
        ///     4) If no nodes had custom exit logic, we invoke an event to let any listeners know.
        /// </summary>
        private void OnExitRequested()
        {
            SaveOrExitAsync(ExitRequested, false).Forget();
        }

        /// <summary>
        ///     When the user is requesting to exit the customizer. The logic for saving is the following:
        ///     1) Traverse through every <see cref="NavigationStack" /> and every node and find if any node has a custom save
        ///     action.
        ///     2) If the node save logic was successful, the traversal ends there and we open the previous node.
        ///     3) If the user dismissed the save logic, we stop the traversal and open the node that interrupted the traversal.
        ///     4) If no nodes had custom save logic, we invoke an event to let any listeners know.
        /// </summary>
        private void OnSaveRequested()
        {
            SaveOrExitAsync(SaveRequested, false).Forget();
        }

        /// <summary>
        ///     When the user is requesting to exit the customizer. The logic for sharing is the following:
        ///     1) Traverse through every <see cref="NavigationStack" /> and every node and find if any node has a custom share
        ///     action.
        ///     2) If the node share logic was successful, the traversal ends there and we open the previous node.
        ///     3) If the user dismissed the share logic, we stop the traversal and open the node that interrupted the traversal.
        ///     4) If no nodes had custom share logic, we invoke an event to let any listeners know.
        /// </summary>
        private void OnShareRequested()
        {
            SaveOrExitAsync(ShareRequested, false).Forget();
        }

        /// <summary>
        ///     When the user is requesting to exit the customizer. The logic for creating is the following:
        ///     1) Traverse through every <see cref="NavigationStack" /> and every node and find if any node has a custom create
        ///     action.
        ///     2) If the node create logic was successful, the traversal ends there and we open the previous node.
        ///     3) If the user dismissed the create logic, we stop the traversal and open the node that interrupted the traversal.
        ///     4) If no nodes had custom create logic, we invoke an event to let any listeners know.
        /// </summary>
        private void OnCreateRequested()
        {
            SaveOrExitAsync(CreateRequested, true).Forget();
        }

        /// <summary>
        ///     Traverse the <see cref="_CurrentNavigationStack" /> and try to run the exit or save
        ///     action async on each node.
        /// </summary>
        /// <param name="endAction"> The type of end action to invoke after save/exit </param>
        /// <param name="isCreate"> Whether this is a 'create' action </param>
        private async UniTask SaveOrExitAsync(Action endAction, bool isCreate)
        {
            // Get if its an exit request
            bool isExit = endAction == ExitRequested;

            if (_CurrentNavigationStack == null)
            {
                return;
            }

            await _CurrentNavigationStack.SaveOrExitAsync(isExit, isCreate);

            //Only pop non main stacks (count > 1)
            if (_CurrentNavigationStack.IsEmpty() && _navigationStacks.Count > 1)
            {
                _navigationStacks.Pop();
            }

            //Go back to the root if we're at the root stack and it's empty.
            //Notify any listeners that the customization experience is ended.
            if (_navigationStacks.Count == 1 && _CurrentNavigationStack.IsEmpty())
            {
                //Push the root node back in.
                GoToRoot();

                //Invoke the given action
                endAction?.Invoke();
            }
            else
            {
                GoToNode(_CurrentNavigationStack.CurrentNode(), true);
            }
        }

        /// <summary>
        ///     Notify the controller that a submit action was requested
        /// </summary>
        private void OnSubmitRequested()
        {
            if (_currentNode == null)
            {
                return;
            }

            async UniTask SubmitAsync()
            {
                //If the submit succeeded, exit the node and go back one.
                var didSubmit = await _currentNode.Controller.OnSubmit();

                if (didSubmit)
                {
                    if (_CurrentNavigationStack.CanGoBack())
                    {
                        GoBack(1);
                    }
                    //It's not a root navigation stack
                    else if (_navigationStacks.Count > 1)
                    {
                        _navigationStacks.Pop();
                        GoToNode(_CurrentNavigationStack.CurrentNode(), true);
                    }
                }
            }

            SubmitAsync().Forget();
        }

        /// <summary>
        ///     Notify the controller that the user requested resetting all changes made
        ///     to the current customization.
        /// </summary>
        private void ResetAllRequested()
        {
            _currentNode.Controller.OnResetAllChanges();
        }

        #endregion

        #region View Configuration

        private bool HasNewNavOptions(INavigationNode targetNode)
        {
            List<NavBarNodeButtonData> options = targetNode.Controller.GetCustomNavBarOptions();

            //Has custom options
            if (options != null && options.Count > 0)
            {
                return true;
            }

            //Has children
            if (!IsLeafNode(targetNode))
            {
                return true;
            }

            //Has a continue option
            return (targetNode.IsStackable && _CurrentNavigationStack.CanGoBack()) || targetNode.IsStackable;
        }

        /// <summary>
        /// Send for nav bar the index that is selected by consumer
        /// </summary>
        /// <param name="index"></param>
        public void SetSelectedNavBarIndex(int index)
        {
            _view.NavBar.SetSelected(index);
        }

        #endregion

        #region Navigation

        public bool HasUndo()
        {
            return _CurrentNavigationStack.HasUndo();
        }

        public bool HasRedo()
        {
            return _CurrentNavigationStack.HasRedo();
        }

        public INavigationNode GetLastSelectedChildForNode(INavigationNode targetNode)
        {
            return _CurrentNavigationStack.GetLastNonStackableChild(targetNode);
        }

        // This will prevent customizer from automatically selecting non stackable child node when you go back to target node
        public void RemoveLastSelectedChildForNode(INavigationNode targetNode)
        {
            _CurrentNavigationStack.RemoveLastNonStackableChild(targetNode);
        }

        public void RemoveLastSelectedChildForCurrentNode()
        {
            RemoveLastSelectedChildForNode(_CurrentNavigationStack.CurrentNode());
        }

        public IReadOnlyCollection<string> GetBreadCrumbs()
        {
            return _CurrentNavigationStack.GetBreadCrumbs();
        }

        public int GetBreadCrumbCount()
        {
            return _CurrentNavigationStack.GetBreadCrumbCount();
        }

        public bool CanGoBack()
        {
            return _CurrentNavigationStack.CanGoBack();
        }

        /// <summary>
        ///     Notify that the user click a create new item CTA.
        /// </summary>
        public void GoToCreateItemNode()
        {
            if (_currentNode == null)
            {
                return;
            }

            GoToNode(_currentNode.CreateItemNode, false);
        }

        /// <summary>
        ///     Notify that the user clicked on an item that is editable.
        /// </summary>
        public void GoToEditItemNode()
        {
            if (_currentNode == null)
            {
                return;
            }

            GoToNode(_currentNode.EditItemNode, false);
        }

        /// <summary>
        ///     If the navigation node has no children
        /// </summary>
        /// <param name="targetNode"> The node to check </param>
        public bool IsLeafNode(INavigationNode targetNode)
        {
            return targetNode.Children.Count == 0;
        }

        /// <summary>
        ///     Go to the root of the <see cref="_navGraph" />
        /// </summary>
        private void GoToRoot()
        {
            INavigationNode node = _navGraph.GetRootNode();

            if (_navigationStacks.Count == 0)
            {
                _navigationStacks.Push(new NavigationStack());
            }
            else if (_navigationStacks.Count > 1)
            {
                CrashReporter.Log(
                    "Can't navigate to default root if other roots are still active",
                    LogSeverity.Warning
                );
                return;
            }

            if (_CurrentNavigationStack.CurrentRootNode() == node)
            {
                CrashReporter.Log("Root node is already assigned", LogSeverity.Warning);
                return;
            }

            _CurrentNavigationStack.Push(node);
            GoToNode(node, false);
        }

        public void GoBack(int backDistance)
        {
            if (_CurrentNavigationStack.GoBack(backDistance))
            {
                GoToNode(_CurrentNavigationStack.CurrentNode(), true);
            }
        }

        /// <summary>
        ///     Navigate to a node.
        /// </summary>
        /// <param name="targetNode"> Node to navigate to </param>
        /// <param name="isGoingBack"> If we are going back in navigation </param>
        public void GoToNode(INavigationNode targetNode, bool isGoingBack)
        {
            if (targetNode == null)
            {
                return;
            }

            targetNode = targetNode.GetEvaluatedNode();

            //Run the logic async since navigating to a node requires it's controller
            //accepts the navigation
            GoToNodeAsync(targetNode, isGoingBack).Forget();
        }

        private async UniTask GoToNodeAsync(INavigationNode targetNode, bool isGoingBack)
        {
            if (targetNode == null)
            {
                return;
            }

            //If it's a root node or we have no tree navigators, we want to create
            //a new tree navigator.
            if (_navigationStacks.Count == 0 ||
                (targetNode.IsRootNode &&
                    _CurrentNavigationStack.CurrentNode() != targetNode))
            {
                _navigationStacks.Push(new NavigationStack());
            }

            //The active node, will be the node we will show, for the navbar only, we use the target node.
            INavigationNode resolvedNode          = targetNode;
            INavigationNode lastNonStackableChild = _CurrentNavigationStack.GetLastNonStackableChild(targetNode);

            //If the target node had a selected child that wasn't stackable,
            //we want to navigate to that child.
            //
            //TODO this logic can probably be improved as it could be confusing. But example of this would be
            //If you're editing hair and from the selected hair element you go to editing color presets, going back
            //should take you to the hair node again.
            if (lastNonStackableChild != null)
            {
                if (lastNonStackableChild == _currentNode)
                {
                    return;
                }

                //Check if we can open the last non stackable child of the target node.
                var canOpen = await lastNonStackableChild.Controller.TryToInitialize(this);

                if (canOpen)
                {
                    resolvedNode = lastNonStackableChild;
                }
            }
            else
            {
                if (resolvedNode == _currentNode)
                {
                    //Remove empty stacks
                    if (_CurrentNavigationStack.IsEmpty())
                    {
                        _navigationStacks.Pop();
                    }

                    return;
                }

                //Check if we can open target node, if not we just return and keep the current node open.
                var canOpen = await resolvedNode.Controller.TryToInitialize(this);

                if (!canOpen)
                {
                    //Remove empty stacks
                    if (_CurrentNavigationStack.IsEmpty())
                    {
                        _navigationStacks.Pop();
                    }

                    return;
                }
            }

            //Push the target node (we don't push the non stackable node here)
            _CurrentNavigationStack.Push(targetNode);


            //Reference previous node
            INavigationNode previous = _currentNode;


            CustomizerViewConfig viewConfig       = resolvedNode.Controller.CustomizerViewConfig;
            Color?               backgroundColor  = viewConfig.hasCustomBackgroundColor ? viewConfig.backgroundColor : (Color?)null;
            var                  hasNewNavOptions = HasNewNavOptions(targetNode);


            //Prepare the customizer view animations for the upcoming node.
            UniTask viewAnimation = _view.GetNodeTransitionAnimation(
                viewConfig.customizerViewFlags,
                viewConfig.customizationEditorHeight,
                viewConfig.showGlobalNavBar,
                hasNewNavOptions,
                isGoingBack,
                immediate: false,
                backgroundColor: backgroundColor
            );

            //Stop customization of previous node
            _currentNode?.Controller?.StopCustomization();

            //Set current node. Ensure we get the actual node that was evaluated.
            _currentNode = resolvedNode;

            //Setup view data
            _view.ConfigureView(resolvedNode, targetNode);

#if UNITY_EDITOR
            //Show debug text to know which node you're on.
            if (_debugMode)
            {
                _nodeDebugText.gameObject.SetActive(true);
                _nodeDebugText.text = _currentNode.Controller.BreadcrumbName;
            }
            else
            {
                _nodeDebugText.gameObject.SetActive(false);
            }
#endif

            //Start customization
            _currentNode.Controller.StartCustomization();

            //Dispose previous node.
            previous?.Controller?.Dispose();

            // New rule to check if the current node has children and we are not
            // going back then we should open the default child node if specified
            if ( !isGoingBack && _currentNode.Children?.Count > 0 && CurrentNode.DefaultChildNavigationNodeToOpen != null )
            {
                var (childNode, index) = GetDefaultChildNodeWithIndex(_currentNode);
                if (childNode != null && index >= 0)
                {
                    await GoToNodeAsync(childNode, false);
                    SetSelectedNavBarIndex(index);
                    return;
                }
            }
            else if ( !isGoingBack && _currentNode.Children?.Count > 0 && CurrentNode.OpenFirstChildNodeAsDefault)
            {
                // Leaving the old logic in place right now. Will be removed when the above node is set properly.
                INavigationNode firstChildNod = _currentNode.Children.FirstOrDefault();
                await GoToNodeAsync(firstChildNod, isGoingBack);
                SetSelectedNavBarIndex(0);
                return;
            }

            //Fire node changed event
            NodeChanged?.Invoke(_currentNode);

            await viewAnimation;
        }

        /// <summary>
        /// Share the event when the actions during editing is finished
        /// </summary>
        public void FinishingEdit()
        {
            EditFinished?.Invoke();
        }

        public async UniTask RefreshItemPicker(bool goToTop = false)
        {
            // wait for item picker to be ready
            while (View == null ||
                   View.PrimaryItemPicker == null ||
                   View.PrimaryItemPicker.IsInitializingCount ||
                   !View.PrimaryItemPicker.gameObject.activeInHierarchy)
            {
                await UniTask.Yield();
            }

            // wait for the scroll rect to be fully ready
            await WaitForScrollRectReady(View.PrimaryItemPicker);

            var currentSelectedIndex = View.PrimaryItemPicker.GetCurrentSelectedIndex();
            int targetIndex = currentSelectedIndex >= 0 ? currentSelectedIndex : 0;

            if (goToTop)
            {
                targetIndex = 0;
            }

            View.PrimaryItemPicker.FixItemPositioningForGridLayout(targetIndex).Forget();
        }

        /// <summary>
        /// Ensures the scroll rect is ready before attempting to scroll by checking
        /// its components and changes to its position
        /// </summary>
        /// <param name="itemPicker">The item picker with components of the scroll rect</param>
        private async UniTask WaitForScrollRectReady(ScrollingItemPicker itemPicker)
        {
            const int maxWaitFrames = 60;
            const int stabilityFrames = 3; // number of frames to wait for scroll rect to be stable
            int frameCount = 0;

            Vector3 lastScrollRectPosition = Vector3.zero;
            Vector2 lastContentPosition = Vector2.zero;
            int stableFrameCount = 0;

            while (frameCount < maxWaitFrames)
            {
                await UniTask.Yield(PlayerLoopTiming.Update);
                frameCount++;

                // check if the scroll rect has valid content size and layout
                if (itemPicker.Content != null &&
                    itemPicker.Content.rect.width > 0 &&
                    itemPicker.Padding != null)
                {
                    Vector3 currentScrollRectPosition = itemPicker.transform.position;
                    Vector2 currentContentPosition = itemPicker.Content.anchoredPosition;

                    // check if positions are stable
                    bool positionsStable = Vector3.Distance(currentScrollRectPosition, lastScrollRectPosition) < 0.01f &&
                        Vector2.Distance(currentContentPosition, lastContentPosition) < 0.01f;

                    if (positionsStable)
                    {
                        stableFrameCount++;
                        if (stableFrameCount >= stabilityFrames)
                        {
                            await UniTask.Yield(PlayerLoopTiming.Update);
                            break;
                        }
                    }
                    else
                    {
                        stableFrameCount = 0; // reset if positions changed
                    }

                    lastScrollRectPosition = currentScrollRectPosition;
                    lastContentPosition = currentContentPosition;
                }
            }
        }

        /// <summary>
        /// Navigate to a specific category node by breadcrumb name (e.g., "Dresses", "Shirts")
        /// </summary>
        /// <param name="categoryName">The breadcrumb name of the category to navigate to</param>
        /// <param name="rootNode">The node to begin navigating from</param>
        /// <returns>True if the category was found and navigated to, false otherwise</returns>
        public async UniTask<bool> NavigateToBreadcrumbCategory(INavigationNode rootNode, string categoryName)
        {

            // find the specific category node and its index
            var (categoryNode, categoryIndex) = FindCategoryNodeWithIndex(rootNode, categoryName);
            if (categoryNode != null)
            {
                // navigate to the specific category
                GoToNode(categoryNode, false);

                // wait for navigation to complete
                await UniTask.Yield();

                // update the navbar selection to show the correct tab as selected
                SetSelectedNavBarIndex(categoryIndex);

                // refresh item picker to scroll to top
                await RefreshItemPicker(true);

                return true;
            }

            return false;
        }

        /// <summary>
        /// Navigates to a node with a specific breadcrumb name
        /// </summary>
        /// <param name="categoryName">The breadcrumb name of the category to navigate to</param>
        /// <param name="rootNode">The node to begin searching from</param>
        /// <returns>True if the category was found and set up successfully, false otherwise</returns>
        public async UniTask<bool> NavigateToNestedCategory(INavigationNode rootNode, string categoryName)
        {
            // find the specific category node and its navbar context
            var (categoryNode, parentNode, categoryIndex) = FindCategoryNodeWithParentContext(rootNode, categoryName);
            if (categoryNode != null && parentNode != null)
            {
                // navigate to parent to establish the correct navbar context
                GoToNode(parentNode, false);
                await UniTask.Yield();

                // navigate to the actual nested category using framework navigation
                GoToNode(categoryNode, false);

                // refresh item picker
                await RefreshItemPicker(true);

                // set the navbar selection
                SetSelectedNavBarIndex(categoryIndex);

                return true;
            }
            else if (categoryNode != null && parentNode == null)
            {
                // handle top-level categories normally
                return await NavigateToBreadcrumbCategory(rootNode, categoryName);
            }

            return false;
        }

        /// <summary>
        /// Get and return category node and its index of the default child navigation node
        /// </summary>
        private (INavigationNode node, int index) GetDefaultChildNodeWithIndex(INavigationNode rootNode)
        {
            if (rootNode.DefaultChildNavigationNodeToOpen == null)
            {
                return (null, -1);
            }

            INavigationNode defaultChildNodeToOpen = rootNode.DefaultChildNavigationNodeToOpen;
            int index = rootNode.Children.IndexOf(defaultChildNodeToOpen);
            return (defaultChildNodeToOpen, index);
        }

        /// <summary>
        /// Recursively find a category node by its breadcrumb name and return both the node and its top-level parent index for navbar selection
        /// </summary>
        private (INavigationNode node, int index) FindCategoryNodeWithIndex(INavigationNode rootNode, string categoryName)
        {
            if (string.Equals(rootNode?.Controller?.BreadcrumbName?.ToLower(), categoryName.ToLower()))
            {
                return (rootNode, 0); // root node is at index 0
            }

            if (rootNode?.Children != null)
            {
                // first check direct children (top-level categories)
                for (int i = 0; i < rootNode.Children.Count; i++)
                {
                    var child = rootNode.Children[i];
                    if (string.Equals(child?.Controller?.BreadcrumbName?.ToLower(), categoryName.ToLower()))
                    {
                        return (child, i);
                    }
                }

                // then recursively check grandchildren and deeper nested categories
                for (int i = 0; i < rootNode.Children.Count; i++)
                {
                    var child = rootNode.Children[i];
                    var (result, _) = FindCategoryNodeWithIndex(child, categoryName);
                    if (result != null)
                    {
                        // return the found nested node but with the top-level parent's index
                        return (result, i);
                    }
                }
            }

            return (null, -1);
        }

        /// <summary>
        /// Find a category node with its parent context for proper navbar handling
        /// </summary>
        private (INavigationNode node, INavigationNode parent, int index) FindCategoryNodeWithParentContext(INavigationNode rootNode, string categoryName)
        {
            if (string.Equals(rootNode?.Controller?.BreadcrumbName?.ToLower(), categoryName.ToLower()))
            {
                return (rootNode, null, 0); // root node has no parent
            }

            if (rootNode?.Children != null)
            {
                // first check direct children (top-level categories)
                for (int i = 0; i < rootNode.Children.Count; i++)
                {
                    var child = rootNode.Children[i];
                    if (string.Equals(child?.Controller?.BreadcrumbName?.ToLower(), categoryName.ToLower()))
                    {
                        return (child, rootNode, i); // return child with its parent
                    }
                }

                // then recursively check grandchildren and deeper nested categories
                for (int i = 0; i < rootNode.Children.Count; i++)
                {
                    var child = rootNode.Children[i];
                    var (result, parent, index) = FindCategoryNodeWithParentContext(child, categoryName);
                    if (result != null)
                    {
                        // for nested items, return the found node with its immediate parent
                        return (result, child, index);
                    }
                }
            }

            return (null, null, -1);
        }

        #endregion
    }
}
