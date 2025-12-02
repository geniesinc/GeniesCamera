using System;
using VContainer;

namespace Genies.ServiceManagement
{
    /// <summary>
    /// A default installer that takes an installation action use if you don't need
    /// to create your own specific installers. 
    /// </summary>
    public class GeniesActionInstaller : IGeniesInstaller
    {
        public int Group { get; }

        private readonly Action<IContainerBuilder> _configuration;

        public GeniesActionInstaller(int group, Action<IContainerBuilder> configuration)
        {
            Group = group;
            _configuration = configuration;
        }

        public void Install(IContainerBuilder builder)
        {
            _configuration(builder);
        }

        public static implicit operator GeniesActionInstaller((int group, Action<IContainerBuilder> installer) installation)
            => new GeniesActionInstaller(installation.group, installation.installer);
    }
}
