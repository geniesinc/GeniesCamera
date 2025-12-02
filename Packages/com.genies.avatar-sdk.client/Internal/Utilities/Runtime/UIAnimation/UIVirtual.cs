using System;

namespace Genies.UI.Animations
{
    /// <summary>
    /// UIVirtual static class
    /// </summary>
    public static class UIVirtual
    {
        public static float EasedValue(float from, float to, float t, Genies.UI.Animations.Ease ease)
        {
            return Genies.UI.Animations.AnimateVirtual.EasedValue(from, to, t, ease);
        }
    }
}

