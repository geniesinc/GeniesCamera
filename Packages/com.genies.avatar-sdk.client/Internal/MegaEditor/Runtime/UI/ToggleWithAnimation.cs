using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.UI.Animations;
using UnityEngine;

namespace Genies.Looks.Customization.UI
{
    public sealed class ToggleWithAnimation : MonoBehaviour
    {
        [SerializeField]
        private ToggleWithAnimationOptionButton optionButtonPrefab;

        [SerializeField]
        private RectTransform selectionMarker;

        public float markerTransitionDuration = 0.25f;

        public int SelectedOptionIndex { get; private set; }
        public string SelectedOptionLabel => _buttons[SelectedOptionIndex].Label;

        public event Action<int> OptionSelected;

        private RectTransform _rectTransform;
        private bool _areOptionsSet;
        private ToggleWithAnimationOptionButton[] _buttons;
        private Coroutine _markerTransitionCoroutine;

        private void OnEnable()
        {
            WaitForLayoutAndRefreshSelectionMarker().Forget();
        }

        private void OnDisable()
        {
            if (_markerTransitionCoroutine != null)
            {
                StopCoroutine(_markerTransitionCoroutine);
            }

            _markerTransitionCoroutine = null;
        }

        public void SetOptions(IEnumerable<string> labels)
        {
            if (labels is null)
            {
                return;
            }

            var labelsArray = labels.ToArray();
            if (labelsArray.Length == 0)
            {
                return;
            }

            Clear();
            _buttons = new ToggleWithAnimationOptionButton[labelsArray.Length];

            for (int i = 0; i < labelsArray.Length; ++i)
            {
                ToggleWithAnimationOptionButton button = Instantiate(optionButtonPrefab, transform);
                button.Clicked += OnButtonClicked;
                button.Set(i, labelsArray[i]);
                _buttons[i] = button;
            }

            SelectedOptionIndex = 0;
            _areOptionsSet = true;

            WaitForLayoutAndRefreshSelectionMarker().Forget();
        }

        public void SelectOption(int optionIndex, bool notifyOptionSelected = true, bool animatedTransition = true)
        {
            if (!_areOptionsSet || optionIndex < 0 || optionIndex >= _buttons.Length)
            {
                return;
            }

            SelectedOptionIndex = optionIndex;

            if (notifyOptionSelected)
            {
                OptionSelected?.Invoke(optionIndex);
            }

            TransitionMakerTo(SelectedOptionIndex, animatedTransition);
        }

        private void Clear()
        {
            if (!_areOptionsSet)
            {
                return;
            }

            foreach (ToggleWithAnimationOptionButton button in _buttons)
            {
                button.Clicked -= OnButtonClicked;
                Destroy(button.gameObject);
            }

            _buttons = null;
            _areOptionsSet = false;
        }

        private void OnButtonClicked(int index)
        {
            // if there are only two options then act as a simple toggle
            if (SelectedOptionIndex == index && _buttons.Length == 2)
            {
                SelectedOptionIndex = index == 0 ? 1 : 0;
            }
            else
            {
                SelectedOptionIndex = index;
            }

            OptionSelected?.Invoke(SelectedOptionIndex);
            TransitionMakerTo(SelectedOptionIndex);
        }

        private async UniTaskVoid WaitForLayoutAndRefreshSelectionMarker()
        {
            // do nothing if the GO is inactive, the OnEnable method will refresh the marker properly
            if (!gameObject.activeInHierarchy)
            {
                return;
            }

            // we just need to wait for one frame for the layout to be ready
            await UniTask.Yield();

            TransitionMakerTo(SelectedOptionIndex, animatedTransition: false);
        }

        private void TransitionMakerTo(int targetButtonIndex, bool animatedTransition = true)
        {
            // don't perform any animations if inactive, the OnEnable method will properly refresh the marker when invoked
            if (!gameObject.activeInHierarchy || _buttons is null)
            {
                return;
            }

            if (_markerTransitionCoroutine != null)
            {
                StopCoroutine(_markerTransitionCoroutine);
            }

            if (animatedTransition)
            {
                _markerTransitionCoroutine = StartCoroutine(StartAnimatedMarkerTransition(targetButtonIndex));
                return;
            }

            // preform instant transition
            RectTransform buttonTransform = _buttons[SelectedOptionIndex].RectTransform;
            float targetPositionX = buttonTransform.localPosition.x;
            Vector2 targetSize = selectionMarker.sizeDelta;
            targetSize.x = buttonTransform.sizeDelta.x;

            Vector3 localPosition = selectionMarker.localPosition;
            localPosition.x = targetPositionX;
            selectionMarker.localPosition = localPosition;
            selectionMarker.sizeDelta = targetSize;
        }

        private IEnumerator StartAnimatedMarkerTransition(int targetButtonIndex)
        {
            RectTransform buttonTransform = _buttons[targetButtonIndex].RectTransform;
            float targetPositionX = buttonTransform.localPosition.x;
            Vector2 targetSize = selectionMarker.sizeDelta;
            targetSize.x = buttonTransform.sizeDelta.x;

            var settings = AnimationSettings.WithEase(Ease.InOutSine);
            settings.AutoStart = true;
            
            var group = UIAnimatation.CreateGroup();
            group.Add(selectionMarker.AnimateSizeDelta(targetSize, markerTransitionDuration, settings));
            group.AddParallel(selectionMarker.AnimateLocalMoveX(targetPositionX, markerTransitionDuration, settings));
            group.Start();
            yield return group.WaitForCompletion();
        }
    }
}
