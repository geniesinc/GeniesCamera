using System.Collections.Generic;
using UnityEngine;

namespace Genies.UI.Widgets
{
    public class StickyElement : MonoBehaviour
    {
        private List<CollapsibleGeniesButton> _buttons = new List<CollapsibleGeniesButton>();

        private void Awake()
        {
            var buttons = GetComponentsInChildren<CollapsibleGeniesButton>(true);

            if(buttons != null)
            {
                _buttons.AddRange(buttons);
            }
        }

        public void CollapseButtons()
        {
            foreach (var button in _buttons)
            {
                button.Collapse();
            }
        }

        public void ResizeButtons(float value)
        {
            foreach (var button in _buttons)
            {
                button.Resize(value);
            }
        }

        public void ExpandButtons()
        {
            foreach (var button in _buttons)
            {
                button.Expand();
            }
        }
    }
}
