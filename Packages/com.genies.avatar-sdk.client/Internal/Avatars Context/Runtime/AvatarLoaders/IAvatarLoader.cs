using Cysharp.Threading.Tasks;
using Genies.Avatars;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars.Context
{
    /// <summary>
    /// Represents a unique avatar configuration that can be loaded when required. You are not allowed to use a specific
    /// definition or be able to set the LOD or <see cref="AvatarsContext"/> since that is all handled by each specific
    /// implementation.
    /// </summary>
    public interface IAvatarLoader
    {
        UniTask<IGenie> LoadAsync(Transform parent = null);
        UniTask<Ref<IGeniePrefab>> LoadAsPrefabAsync();
        UniTask<ISpeciesGenieController> LoadControllerAsync(Transform parent = null);
    }
}