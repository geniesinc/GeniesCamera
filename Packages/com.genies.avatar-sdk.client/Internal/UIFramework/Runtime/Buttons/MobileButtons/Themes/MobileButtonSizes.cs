using UnityEngine;
using UnityEngine.UI;

namespace Genies.UIFramework
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "MobileButtonSizes", menuName = "UIFramework/MobileButtonSizes")]
#endif
    public class MobileButtonSizes : ScriptableObject
    {
        public Vector2 XL;
        public Vector2 Large;
        public Vector2 Medium;
    }
}
