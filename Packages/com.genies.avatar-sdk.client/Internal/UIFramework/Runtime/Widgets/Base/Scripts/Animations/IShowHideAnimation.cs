using UnityEngine;

namespace Genies.UI.Widgets
{
    public interface IShowHideAnimation
    {
        RectTransform RectTransform { get; set; }
        void Show();
        void Hide();
    }
}
