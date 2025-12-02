using VContainer.Unity;

namespace Genies.ServiceManagement
{
    /// <summary>
    /// Interface for registering/installing your services/dependencies. Installers of the same group will belong to the same scope.
    /// you should override the <see cref="IGroupedOperation.OperationOrder"/> if you want to control installation/initialization order.
    ///
    /// To declare dependencies on other installers, implement IRequiresInstaller&lt;TInstaller&gt; interfaces.
    /// Requirements will be validated at runtime to ensure proper installation order.
    /// </summary>
    public interface IGeniesInstaller : IInstaller, IGroupedOperation
    {
    }
}
