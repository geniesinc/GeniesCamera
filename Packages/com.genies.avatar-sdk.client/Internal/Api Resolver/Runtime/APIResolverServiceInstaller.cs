
using Genies.ServiceManagement;
using UnityEngine;
using VContainer;

namespace Genies.APIResolver
{

    [AutoResolve]
    public class APIResolverServiceInstaller: IGeniesInstaller
    {
        public int OperationOrder => DefaultInstallationGroups.CoreServices;

        [SerializeField] private APIResolverData _resolverData;

        public void Install(IContainerBuilder builder)
        {
            builder.Register<IAPIResolverService, APIResolverService>(Lifetime.Singleton)
                .WithParameter(_resolverData)
                .AsSelf();
        }
    }
}
