using UnityEngine;
using UnityEngine.UI;

namespace Genies.Looks.MultiOptionPopup.Scripts
{
    public class SelectableMultiOptionButton : MultiOptionButton
    {
        [SerializeField] private Image _selectedIcon;
        [SerializeField] private Image _unselectedIcon;

        public void SetSelected(bool selected)
        {
            _selectedIcon.gameObject.SetActive(selected);
            _unselectedIcon.gameObject.SetActive(!selected);
        }

        public void SetColorButton(Color color)
        {
            text.color = color;
            if (icon != null)
            {
                icon.color = color;
            }
        }
    }
}
