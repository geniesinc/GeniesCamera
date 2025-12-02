using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.UI.Components.Widgets;
using Genies.UIFramework;
using UnityEngine;

namespace Genies.Customization.MegaEditor
{
    public class FilterBar : MonoBehaviour
    {
        [SerializeField] private RectTransform _contentRt;
        [SerializeField] private ScrollableToggleBar _toggleBar;

        [SerializeField] private FilterBarOptionButton _buttonPrefab;
        private List<FilterBarOptionButton> _filterButtons = new List<FilterBarOptionButton>();

        private FilterBarOptionButton CreateFilterButton(FilterBarOption option, bool isSelected = false)
        {
            //Probably use pooling here
            var o = Instantiate(_buttonPrefab, _contentRt);
            o.Initialize(option, isSelected);
            return o;
        }

        public async void SetOptions(List<FilterBarOption> options, int selectedIndex = -1)
        {
            //Dispose current filters first.
            Dispose();

            //Disable continue button
            _toggleBar.gameObject.SetActive(true);

            //Create new ones
            for (var index = 0; index < options.Count; index++)
            {
                var isSelected = index == selectedIndex;
                var option = options[index];
                var node = CreateFilterButton(option, isSelected);
                _filterButtons.Add(node);
            }

            await _toggleBar.SetButtons(_filterButtons.ConvertAll(input => (GeniesButton)input));
            _toggleBar.Show();

            await UniTask.DelayFrame(2);
            _toggleBar.SetSelected(selectedIndex);
        }

        public void SetSelected(int selectedIndex)
        {
            foreach (var filterBarOptionButton in _filterButtons)
            {
                filterBarOptionButton.SetSelected(false);
            }

            _filterButtons[selectedIndex].SetSelected(true);
            _toggleBar.SetSelected(selectedIndex);
        }

        public void Dispose()
        {
            //Probably add pooling and use the toggle bar widget instead.
            if (_filterButtons == null || _filterButtons.Count <= 0)
            {
                return;
            }

            _toggleBar.Dispose();

            foreach (var button in _filterButtons)
            {
                button.Dispose();
                Destroy(button.gameObject);
            }

            _filterButtons.Clear();
        }
    }
}

