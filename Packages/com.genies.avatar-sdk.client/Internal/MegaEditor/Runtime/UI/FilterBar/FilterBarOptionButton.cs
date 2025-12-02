using Genies.UI.Widgets;
using Genies.UIFramework;
using UnityEngine;

namespace Genies.Customization.MegaEditor
{
    public class FilterBarOptionButton : GeniesButton
    {
        [SerializeField] private Chip _chip;

        private FilterBarOption _optionData;

        public void Initialize(FilterBarOption optionData, bool isSelected = false)
        {
            _optionData = optionData;

            _chip.IsCountEnabled = true;
            _chip.IsIconEnabled = false;
            _chip.SetLabel(optionData.displayName);
            _chip.SetCount(optionData.countDisplay);

            onClick.AddListener(OnClick);
            SetSelected(isSelected);
        }

        private void OnClick()
        {
            _optionData.onClicked?.Invoke();
        }

        public void Dispose()
        {
            onClick.RemoveAllListeners();
        }

        public void SetSelected(bool isSelected)
        {
            _chip.IsSelected = isSelected;
        }
    }
}
