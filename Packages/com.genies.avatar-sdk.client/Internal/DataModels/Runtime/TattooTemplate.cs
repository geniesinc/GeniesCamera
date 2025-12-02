using Genies.MakeupPresets;
using UnityEngine;

namespace Genies.Models
{
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "TattooTemplate", menuName = "Genies/Tattoos/TattooTemplate")]
#endif
    public class TattooTemplate : DecoratedSkinTemplate
    {
        public TattooCategory category;
    }
}
