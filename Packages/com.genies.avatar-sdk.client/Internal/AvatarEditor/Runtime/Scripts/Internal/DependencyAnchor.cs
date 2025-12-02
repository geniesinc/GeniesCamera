using UnityEngine;
using TMPro;

namespace Genies.AvatarEditor
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal class DependencyAnchor : MonoBehaviour
#else
    public class DependencyAnchor : MonoBehaviour
#endif
    {
        [SerializeField] private TMP_Text _textMarker;
    }
}
