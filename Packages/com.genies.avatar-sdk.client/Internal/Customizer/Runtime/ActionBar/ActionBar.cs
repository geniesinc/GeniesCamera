using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.Customization.Framework.Actions
{
    public class ActionBar : MonoBehaviour, IActionBar
    {
        public event Action UndoRequested;
        public event Action RedoRequested;
        public event Action SaveRequested;
        public event Action ShareRequested;
        public event Action CreateRequested;
        public event Action ExitRequested;
        public event Action ResetAllRequested;
        public event Action SubmitRequested;


        [SerializeField]
        private ActionBarButton _undoButton;

        [SerializeField]
        private ActionBarButton _redoButton;

        [SerializeField]
        private ActionBarButton _saveButton;

        [SerializeField]
        private ActionBarButton _shareButton;

        [SerializeField]
        private ActionBarButton _createButton;

        [SerializeField]
        private ActionBarButton _submitButton;

        [SerializeField]
        private ActionBarButton _exitButton;

        public ActionBarButton ExitButton => _exitButton;

        [SerializeField]
        private Button _resetAllButton;

        private Dictionary<ActionBarFlags, GameObject> _buttonsMapping;
        private readonly ActionBarFlags[] _allFlagOptions = (ActionBarFlags[])Enum.GetValues(typeof(ActionBarFlags));

        public void Initialize()
        {
            _undoButton.onClick.AddListener(() => UndoRequested?.Invoke());
            _redoButton.onClick.AddListener(() => RedoRequested?.Invoke());
            _saveButton.onClick.AddListener(() => SaveRequested?.Invoke());
            _shareButton.onClick.AddListener(() => ShareRequested?.Invoke());
            _createButton.onClick.AddListener(() => CreateRequested?.Invoke());
            _submitButton.onClick.AddListener(() => SubmitRequested?.Invoke());
            _exitButton.onClick.AddListener(() => ExitRequested?.Invoke());
            _resetAllButton.onClick.AddListener(() => ResetAllRequested?.Invoke());

            _buttonsMapping = new Dictionary<ActionBarFlags, GameObject>
            {
                { ActionBarFlags.Undo, _undoButton.gameObject },
                { ActionBarFlags.Redo, _redoButton.gameObject },
                { ActionBarFlags.Exit, _exitButton.gameObject },
                { ActionBarFlags.Save, _saveButton.transform.parent.gameObject },
                { ActionBarFlags.Share, _shareButton.gameObject },
                { ActionBarFlags.Create, _createButton.gameObject },
                { ActionBarFlags.Submit, _submitButton.gameObject },
                { ActionBarFlags.ResetAll, _resetAllButton.gameObject },
            };
        }

        public void Dispose()
        {
            _undoButton.onClick.RemoveAllListeners();
            _redoButton.onClick.RemoveAllListeners();
            _saveButton.onClick.RemoveAllListeners();
            _shareButton.onClick.RemoveAllListeners();
            _createButton.onClick.RemoveAllListeners();
            _submitButton.onClick.RemoveAllListeners();
            _exitButton.onClick.RemoveAllListeners();
            _resetAllButton.onClick.RemoveAllListeners();
        }

        public void ToggleUndoRedoActivity(bool hasUndo, bool hasRedo)
        {
            _undoButton.ToggleActivity(hasUndo);
            _redoButton.ToggleActivity(hasRedo);
        }

        public void SetActionFlags(ActionBarFlags barFlags)
        {
            foreach (var flagOption in _allFlagOptions)
            {
                if (flagOption == ActionBarFlags.None)
                {
                    continue;
                }

                var isActive = barFlags.HasFlag(flagOption);
                var flagGo   = _buttonsMapping[flagOption];
                flagGo.SetActive(isActive);
            }
        }

        public void InvokeExitRequested()
        {
            ExitRequested?.Invoke();
        }
    }
}
