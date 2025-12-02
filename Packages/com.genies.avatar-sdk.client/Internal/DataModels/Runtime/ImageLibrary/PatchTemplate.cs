using UnityEngine;

namespace Genies.Models
{
    /// <summary>
    /// Meant for wearables (including generative) and can extend to hair and other “things”
    /// Patches can hold normal maps that create an illusion of bumps/ 3d appearance
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "DecalTemplate", menuName = "Genies/ImageLibrary/DecalTemplate")]
#endif
    public class PatchTemplate : LibraryAssetTemplate
    {

    }
}
