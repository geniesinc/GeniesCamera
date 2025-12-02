using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.UI.Widgets
{
    public class Chip : MonoBehaviour, ISelectable
    {
        public string ToggleName;
        [SerializeField] private TextMeshProUGUI _label;
        [SerializeField] private TextMeshProUGUI _count;
        [SerializeField] private Image _selectedBackground;
        [SerializeField] private Image _icon;
        public string Label => _label.text;

        public bool IsIconEnabled 
        { 
            get => _icon.gameObject.activeSelf; 
            set 
            { 
                _icon.gameObject.SetActive(value); 
            } 
        }

        public bool IsCountEnabled
        {
            get => _count.transform.parent.gameObject.activeSelf;
            set
            {
                _count.transform.parent.gameObject.SetActive(value);
            }
        }

        private void Awake()
        {
            Debug.Assert(_icon != null, "_icon is not set!");
            Debug.Assert(_selectedBackground != null, "_selectedBackground is not set!");
            Debug.Assert(_count != null, "_count is not set!");
            Debug.Assert(_label != null, "_label is not set!");
        }

        public bool IsSelected
        {
            get => _selectedBackground.gameObject.activeSelf;
            set => _selectedBackground.gameObject.SetActive(value);
        }

        public void SetLabel(string text)
        {
            _label.text = text;
        }

        public void SetCount(int amount)
        {
            _count.text = amount.ToString();
        }

        public void SetSprite(Sprite sprite)
        {
            _icon.sprite = sprite;
        }

        public void SetWaitingCount()
        {
            _count.text = "...";
        }

        public void ChangeSelectionStatus()
        {
            IsSelected = !IsSelected;
        }

    }
}
