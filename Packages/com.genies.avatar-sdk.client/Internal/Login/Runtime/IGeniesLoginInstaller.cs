using Genies.ServiceManagement;
using VContainer; // Required for IInstaller from IGeniesInstaller

namespace Genies.Login
{
    public interface IGeniesLoginInstaller : IGeniesInstaller
    {
        new int OperationOrder => DefaultInstallationGroups.CoreServices;
    }
}
