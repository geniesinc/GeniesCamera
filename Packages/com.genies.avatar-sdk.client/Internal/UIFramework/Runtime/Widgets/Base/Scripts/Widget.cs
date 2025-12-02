using System;
using System.Threading.Tasks;
using Genies.UI.Transitions;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.UI.Widgets {
    /// <summary>
    /// Base class for all UI widgets that support transition animations.
    /// Provides common functionality for UI components including RectTransform and CanvasGroup management.
    /// </summary>
    public abstract class Widget : MonoBehaviour, ITransitionable {
        /// <summary>
        /// Gets the RectTransform component used for position and scale transitions.
        /// </summary>
        public RectTransform RectTransform { get; private set; }
        
        /// <summary>
        /// Gets the CanvasGroup component used for alpha/fade transitions.
        /// </summary>
        public CanvasGroup CanvasGroup { get; private set; }

        private void Awake()
        {
            RectTransform = gameObject.GetComponent<RectTransform>();
            if (RectTransform == null)
            {
                throw new Exception("Could not find Rect component for Screen");
            }

            CanvasGroup = gameObject.GetComponent<CanvasGroup>();
            if (CanvasGroup == null) {
                CanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            OnWidgetInitialized();
        }

        /// <summary>
        /// Called when the widget has been initialized with its required components.
        /// Override this method to perform custom initialization logic.
        /// </summary>
        public virtual void OnWidgetInitialized() {
        }

        protected async void DisableLayoutGroupsAfterTransition() {
            SetLayoutGroupsEnabled(true);
            await Task.Delay((int)(TransitionService.DefaultInDuration * 1000f));
            if (this == null)
            {
                return;
            }

            SetLayoutGroupsEnabled(false);
        }

        protected void SetLayoutGroupsEnabled(bool value) {
            var layoutGroups = GetComponentsInChildren<VerticalLayoutGroup>(!value);
            foreach (var layoutGroup in layoutGroups) {
                layoutGroup.enabled = value;
            }

            var horizontalLayoutGroups = GetComponentsInChildren<HorizontalLayoutGroup>(!value);
            foreach (var layoutGroup in horizontalLayoutGroups) {
                layoutGroup.enabled = value;
            }
        }
    }
}
