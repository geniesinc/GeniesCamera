using System;

namespace Genies.UI.Widgets
{
    public interface INavBar
    {
        bool IsShown { get; }
        void Show();
        void Hide();
        void HideWithDuration(Action onComplete = null);
        void HideImmediately();
    }
}
