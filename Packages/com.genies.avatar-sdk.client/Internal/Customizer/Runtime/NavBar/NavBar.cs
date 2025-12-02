using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.UI.Components.Widgets;
using Genies.UIFramework;
using UnityEngine;
using UnityEngine.UI;

namespace Genies.Customization.Framework.Navigation
{
    public class NavBar : MonoBehaviour
    {
        [SerializeField]
        private RectTransform _contentRt;
        [SerializeField]
        private ScrollableToggleBar _toggleBar;
        [SerializeField]
        private Button _continueButton;
        [SerializeField]
        private NavBarNodeButton _nodePrefab;
        [SerializeField]
        private GeniesButton _createItemButton;

        private List<NavBarNodeButton> _nodes = new List<NavBarNodeButton>();

        private NavBarNodeButton CreateNode(NavBarNodeButtonData nodeButtonData)
        {
            //Probably use pooling here
            var prefab = nodeButtonData.overridePrefab ? nodeButtonData.overridePrefab : _nodePrefab;
            var o      = Instantiate(prefab, _contentRt);
            o.Initialize(nodeButtonData);
            return o;
        }

        public void SetSelected(int index)
        {
            _toggleBar.SetSelected(index);
        }

        public async void SetOptions(List<NavBarNodeButtonData> options, Action createItem)
        {
            //Dispose current nodes first.
            Dispose();

            //Disable continue button
            _continueButton.gameObject.SetActive(false);
            _toggleBar.gameObject.SetActive(true);

            var selectedOptionIndex = -1;
            //Create new ones
            for (var index = 0; index < options.Count; index++)
            {
                var option = options[index];

                //Select the first one.
                if (option.isSelected && selectedOptionIndex < 0)
                {
                    selectedOptionIndex = index;
                }

                var node   = CreateNode(option);
                _nodes.Add(node);
            }

            if (createItem != null)
            {
                _createItemButton.onClick.AddListener(createItem.Invoke);
                _createItemButton.gameObject.SetActive(true);
            }
            else
            {
                _createItemButton.gameObject.SetActive(false);
            }

            await _toggleBar.SetButtons(_nodes.ConvertAll(input => (GeniesButton)input));
            _toggleBar.Show();

            await UniTask.DelayFrame(2);
            _toggleBar.SetSelected(selectedOptionIndex);
        }

        /// <summary>
        /// Hides toggles and only shows a single button to continue
        /// </summary>
        public void ShowContinueOption(Action continueCommand)
        {
            //Dispose of current nodes first
            Dispose();

            _continueButton.onClick.AddListener(() => continueCommand?.Invoke());
            _continueButton.gameObject.SetActive(true);
            _toggleBar.gameObject.SetActive(false);
        }

        public void Dispose()
        {
            //Probably add pooling and use the toggle bar widget instead.
            if (_nodes == null || _nodes.Count <= 0)
            {
                return;
            }

            _toggleBar.Dispose();

            foreach (var node in _nodes)
            {
                node.Dispose();
                Destroy(node.gameObject);
            }

            _nodes.Clear();
            _continueButton.onClick.RemoveAllListeners();
            _createItemButton.onClick.RemoveAllListeners();

            _continueButton.gameObject.SetActive(false);
            _createItemButton.gameObject.SetActive(false);
        }
    }
}
