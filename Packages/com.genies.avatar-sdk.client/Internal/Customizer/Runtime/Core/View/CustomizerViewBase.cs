using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.Customization.Framework.Actions;
using Genies.Customization.Framework.ItemPicker;
using Genies.Customization.Framework.Navigation;
using Toolbox.Core;
using UnityEngine;

namespace Genies.Customization.Framework
{
    public abstract class CustomizerViewBase : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField]
        private NavBar _navBar;

        [SerializeField]
        private ActionBar _actionBar;

        [SerializeField]
        private ScrollingItemPicker _primaryItemPicker;

        [SerializeField]
        private ScrollingItemPicker _secondaryItemPicker;

        [SerializeField]
        private EditOrDeleteCustomColorController _editOrDeleteCustomColorController;

        [SerializeField]
        [EnumToggles]
        private ActionBarFlags _disallowedFlags;

        [SerializeField]
        private SerializedDictionary<CustomizerViewLayer, RectTransform> _layers;

        private readonly Dictionary<string, Component> _extraViews = new Dictionary<string, Component>();

        protected readonly CustomizerViewConfig _defaultConfig = new CustomizerViewConfig
        {
            actionBarFlags = ActionBarFlags.Save |
                ActionBarFlags.Share |
                ActionBarFlags.Create |
                ActionBarFlags.Exit |
                ActionBarFlags.Redo |
                ActionBarFlags.Undo,
            customizerViewFlags = CustomizerViewFlags.NavBar | CustomizerViewFlags.ActionBar,
            hasCustomBackgroundColor = false,
        };

        protected Customizer _customizer;

        public ActionBar ActionBar => _actionBar;
        public NavBar NavBar => _navBar;

        /// <summary>
        /// Example usage: Selecting main category items
        /// </summary>
        public ScrollingItemPicker PrimaryItemPicker => _primaryItemPicker;
        public ScrollingItemPicker SecondaryItemPicker => _secondaryItemPicker;
        public EditOrDeleteCustomColorController EditOrDeleteController => _editOrDeleteCustomColorController;

        public void Initialize(Customizer customizer)
        {
            _customizer = customizer;
            ActionBar.Initialize();
            OnInitialized();
        }

        public void Dispose()
        {
            _actionBar.Dispose();
            _navBar.Dispose();

            foreach (KeyValuePair<string, Component> view in _extraViews)
            {
                Component obj = view.Value;

                if (obj != null)
                {
                    Destroy(obj.gameObject);
                }
            }

            _extraViews.Clear();
            OnDispose();
        }

        protected abstract void OnInitialized();
        protected abstract void OnDispose();

        protected abstract void OnConfigureView(
            INavigationNode resolvedNode,
            INavigationNode requestedNode);

        public virtual void OnUndoRedo(bool hasUndo, bool hasRedo)
        {
            ActionBar.ToggleUndoRedoActivity(hasUndo, hasRedo);
        }

        public void ConfigureView(
            INavigationNode resolvedNode,
            INavigationNode requestedNode)
        {
            ConfigureActionBar(resolvedNode);
            ConfigureNavigationBar(requestedNode);
            OnConfigureView(resolvedNode, requestedNode);
        }

        /// <summary>
        /// Configure the navigation bar for the current node.
        /// </summary>
        /// <param name="targetNode"> The current node </param>
        protected virtual void ConfigureNavigationBar(INavigationNode targetNode)
        {
            List<NavBarNodeButtonData> navBarOptions = targetNode.Controller.GetCustomNavBarOptions();

            //If there are children and no custom options
            if ((navBarOptions == null || navBarOptions.Count <= 0) && !_customizer.IsLeafNode(targetNode))
            {
                navBarOptions = new List<NavBarNodeButtonData>();
                foreach (INavigationNode childNode in targetNode.Children)
                {
                    if (childNode == null)
                    {
                        continue;
                    }

                    INavigationNode lastChildNode = _customizer.GetLastSelectedChildForNode(targetNode);

                    CustomizerViewConfig childViewConfig = childNode.Controller.CustomizerViewConfig;
                    var option = new NavBarNodeButtonData
                    {
                        icon = childViewConfig.icon,
                        displayName = childViewConfig.navBarButtonName,
                        clickCommand = () => _customizer.GoToNode(childNode, false),
                        overridePrefab = childViewConfig.customNodePrefab,
                        showNotification = childNode.Controller.HasNotification(),
                        isSelected = lastChildNode == childNode && !childNode.IsStackable,
                    };

                    navBarOptions.Add(option);
                }
            }

            CustomizerViewConfig customizerViewConfig = targetNode.Controller.CustomizerViewConfig;
            if (navBarOptions == null || navBarOptions.Count == 0)
            {
                if (targetNode.IsStackable && _customizer.CanGoBack())
                {
                    //We only show the continue button if the node is stackable and we have no other options.
                    _navBar.ShowContinueOption(() => _customizer.GoBack(1));
                }
                else if (targetNode.IsStackable)
                {
                    _navBar.Dispose();
                }

                return;
            }

            //Populate bar
            _navBar.SetOptions(
                                navBarOptions,
                                customizerViewConfig.showNavBarCreateNewItemCta ? _customizer.GoToCreateItemNode : (Action)null
                                );
        }

        private void ConfigureActionBar(INavigationNode node)
        {
            CustomizerViewConfig config = node.Controller?.CustomizerViewConfig ?? _defaultConfig;

            var flags = config.actionBarFlags;
            flags &= ~_disallowedFlags;

            ActionBar.SetActionFlags(flags);
            ActionBar.ToggleUndoRedoActivity(_customizer.HasUndo(), _customizer.HasRedo());
        }

        public abstract UniTask AnimateOut(bool immediate = false, Color? backgroundColor = null);

        public abstract UniTask GetNodeTransitionAnimation(
            CustomizerViewFlags viewFlags,
            float customizationEditorHeight = 0,
            bool showGlobalNavigation = false,
            bool navOptionsChanged = false,
            bool isGoingBack = false,
            bool immediate = false,
            Color? backgroundColor = null);

        /// <summary>
        ///     A helper method for creating dynamic views for controllers.
        ///     Will cache all these views until the customization is finished.
        /// </summary>
        /// <param name="viewId"> Required id of the view, this helps us retrieve cached views </param>
        /// <param name="layer"> The layer to create the view in </param>
        /// <param name="viewObj"> The Object of the view </param>
        /// <typeparam name="T"> The type to return </typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public T GetOrCreateViewInLayer<T>(string viewId, CustomizerViewLayer layer, T viewObj) where T : Component
        {
            if (string.IsNullOrEmpty(viewId) || viewObj == null)
            {
                CrashReporter.LogHandledException(
                                                    new InvalidOperationException(
                                                                                $"Trying to instantiate a view in the customizer without a valid setup! id: {viewId} | obj: {viewObj}"
                                                                                )
                                                );
            }

            var key = $"{viewId}-{layer}";

            if (_extraViews.TryGetValue(key, out Component currentGo) && currentGo != null)
            {
                return currentGo as T;
            }

            _layers.TryGetValue(layer, out RectTransform parentTransform);

            T createdView = Instantiate(viewObj, parentTransform, false);
            _extraViews[key] = createdView;

            return createdView;
        }

        /// <summary>
        /// Virtual method for forcing the refresh of the view.
        /// </summary>
        public virtual void RefreshView()
        {
        }

        /// <summary>
        /// Virtual method for forcing the hide of the view.
        /// </summary>
        public virtual void HideView()
        {
        }
    }
}
