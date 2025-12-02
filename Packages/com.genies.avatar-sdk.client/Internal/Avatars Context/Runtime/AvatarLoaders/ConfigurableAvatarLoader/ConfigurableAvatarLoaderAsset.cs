using Cysharp.Threading.Tasks;
using Genies.Avatars;
using UnityEngine;

namespace Genies.Avatars.Context
{
    /// <summary>
    /// <see cref="AvatarLoaderAsset"/> implementation that offers some generic configuration parameters. It is the
    /// serializable asset version of <see cref="ConfigurableAvatarLoader"/>.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "ConfigurableAvatarLoader", menuName = "Genies/Avatar Loaders/Configurable Avatar Loader")]
#endif
    public sealed class ConfigurableAvatarLoaderAsset : AvatarLoaderAsset, IAvatarDefinitionSource
    {
        [SerializeField]
        private SerializableAvatarLoader configuration;

        public override async UniTask<ISpeciesGenieController> LoadControllerAsync(Transform parent = null)
        {
            ISpeciesGenieController controller = await configuration.LoadControllerAsync(parent);
            if (controller?.Genie is { IsDisposed: false })
            {
                controller.Genie.Root.name = name;
            }

            return controller;
        }

        public UniTask<string> GetDefinitionAsync()
            => configuration.GetDefinitionAsync();
    }
}
