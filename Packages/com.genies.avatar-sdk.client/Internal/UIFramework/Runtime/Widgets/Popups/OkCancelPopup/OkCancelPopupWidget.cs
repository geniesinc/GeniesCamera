using Cysharp.Threading.Tasks;
using Genies.UIFramework;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Genies.UI.Widgets
{
    public class OkCancelPopupWidget : PopupWidget
    {
        public enum ExitResult
        {
            Ok,
            Cancel,
            Close
        }

        public UnityEvent OnOkButtonClicked = new UnityEvent();
        public UnityEvent OnCancelButtonClicked = new UnityEvent();
        public UnityEvent OnCloseButtonClicked = new UnityEvent();
        [SerializeField] private OutlineButton OkButton;
        [SerializeField] private GeniesButton CancelButton;
        [SerializeField] private Button CloseButton;

        private Button _backgroundBlockerButton;

        private UniTaskCompletionSource<ExitResult> _inputCompletionSource;

        protected virtual void Awake()
        {
            _backgroundBlockerButton = BackgroundBlocker?.GetComponent<Button>();
        }

        public UniTask<ExitResult> ShowAndWaitInputAsync()
        {
            _inputCompletionSource ??= new UniTaskCompletionSource<ExitResult>();
            Show();
            return _inputCompletionSource.Task;
        }

        public override void OnEnable()
        {
            base.OnEnable();
            OkButton?.OnClick.AddListener(Ok);
            CancelButton?.onClick.AddListener(Cancel);
            CloseButton?.onClick.AddListener(Close);
            _backgroundBlockerButton?.onClick.AddListener(Close);
        }

        public void OnDisable()
        {
            OkButton?.OnClick.RemoveListener(Ok);
            CancelButton?.onClick.RemoveListener(Cancel);
            CloseButton?.onClick.RemoveListener(Close);
            _backgroundBlockerButton?.onClick.RemoveListener(Close);
            OnUserInput(ExitResult.Close);
        }

        protected override void OnShown()
        {
            OkButton?.SetButtonEnabled(true);
        }

        protected virtual async void Ok()
        {
            await HideAsync();
            OnOkButtonClicked.Invoke();
            OnUserInput(ExitResult.Ok);
        }

        private async void Cancel()
        {
            await HideAsync();
            OnCancelButtonClicked.Invoke();
            OnUserInput(ExitResult.Cancel);
        }

        private async void Close()
        {
            await HideAsync();
            OnCloseButtonClicked.Invoke();
            OnUserInput(ExitResult.Close);
        }

        private void OnUserInput(ExitResult result)
        {
            if (_inputCompletionSource is null)
            {
                return;
            }

            _inputCompletionSource.TrySetResult(result);
            _inputCompletionSource = null;
        }
    }
}
