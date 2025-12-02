using System;
using Cysharp.Threading.Tasks;
using Genies.UI.Animations;
using UnityEngine;
using UnityEngine.UI;

using UIAnimator = Genies.UI.Animations.UIAnimator;

namespace Genies.Customization.Framework
{
    public class EditOrDeleteCustomColorController : MonoBehaviour
    {
        public event Action OnEditClicked;
        public event Action OnDeleteClicked;

        [Header("References")]
        public Button editButton;
        public Button deleteButton;
        public RectTransform editButtonRT;
        public RectTransform deleteButtonRT;

        [Header("Tween Configs")]
        public Vector2 editButtonFinalPosition;
        public Vector2 deleteButtonFinalPosition;
        public float enableDuration = 0.25f;
        public float disableDuration = 0.1f;

        private readonly Vector2 _editButtonInitialPosition = Vector2.zero;
        private readonly Vector2 _deleteButtonInitialPosition = Vector2.zero;

        private UIAnimator editButtonTween;
        private UIAnimator deleteButtonTween;

        public bool IsActive { get; private set; }

        private GameObject _currentCell;

        private void Awake()
        {
            _currentCell = null;
        }

        private void OnEnable()
        {
            editButton.onClick.AddListener(OnEditButtonClicked);
            deleteButton.onClick.AddListener(OnDeleteButtonClicked);
        }

        private void OnDisable()
        {
            editButton.onClick.RemoveListener(OnEditButtonClicked);
            deleteButton.onClick.RemoveListener(OnDeleteButtonClicked);
        }

        private void OnEditButtonClicked()
        {
            OnEditClicked?.Invoke();
        }

        private void OnDeleteButtonClicked()
        {
            OnDeleteClicked?.Invoke();
        }

        private void Update()
        {
            if (_currentCell != null && transform.position != _currentCell.transform.position)
            {
                transform.position = _currentCell.transform.position;
            }
        }

        public async UniTask Enable(GameObject cellToFollow)
        {
            editButtonRT.gameObject.SetActive(true);
            deleteButtonRT.gameObject.SetActive(true);

            _currentCell = cellToFollow;
            transform.position = _currentCell.transform.position;

            editButtonTween = editButtonRT.SpringLocalPosition(editButtonFinalPosition, SpringPhysics.Presets.Gentle);
            deleteButtonTween = deleteButtonRT.SpringLocalPosition(deleteButtonFinalPosition, SpringPhysics.Presets.Gentle);
            await deleteButtonTween;

            IsActive = true;
        }

        private async UniTask Disable()
        {
            editButtonTween = editButtonRT.SpringLocalPosition(_editButtonInitialPosition, SpringPhysics.Presets.Slow);
            deleteButtonTween = deleteButtonRT.SpringLocalPosition(_deleteButtonInitialPosition, SpringPhysics.Presets.Slow);
            await deleteButtonTween;

            _currentCell = null;
            IsActive = false;
        }

        public void DeactivateButtonsImmediately()
        {
            _currentCell = null;
            IsActive = false;

            editButtonRT.gameObject.SetActive(false);
            deleteButtonRT.gameObject.SetActive(false);
        }

        public async UniTask DisableAndDeactivateButtons()
        {
            await Disable();

            editButtonRT.gameObject.SetActive(false);
            deleteButtonRT.gameObject.SetActive(false);
        }

        public void SetDeleteButtonInteractable(bool interactable)
        {
            deleteButton.interactable = interactable;
        }
    }
}
