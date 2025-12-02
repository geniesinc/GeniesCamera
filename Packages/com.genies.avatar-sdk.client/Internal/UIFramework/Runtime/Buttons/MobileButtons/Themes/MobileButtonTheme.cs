using UnityEngine;
using UnityEngine.UI;

namespace Genies.UIFramework
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "ButtonTheme", menuName = "UIFramework/MobileButtonColorTheme")]
#endif
    public class MobileButtonTheme : ScriptableObject
    {
        public Color defaultOuterFrame;
        public Color defaultInnerFrame;
        public Color defaultTextColor;
        public float defaultOutlineWidth;

        public Color hoverOuterFrame;
        public Color hoverInnerFrame;
        public Color hoverTextColor;
        public float hoverOutlineWidth;

        public Color selectedOuterFrame;
        public Color selectedInnerFrame;
        public Color selectedTextColor;
        public float selectedOutlineWidth;

        public Color disabledOuterFrame;
        public Color disabledInnerFrame;
        public Color disabledTextColor;
        public float disabledOutlineWidth;
    }
}