namespace Genies.ServiceManagement
{
    /// <summary>
    /// Marker interface indicating this installer has dependencies on other installers.
    /// Implement IRequiresInstaller&lt;T&gt; to declare specific installer dependencies.
    /// </summary>
    public interface IHasInstallerRequirements
    {
        // Marker interface - actual requirements defined through IRequiresInstaller<T>
    }

    /// <summary>
    /// Indicates this installer requires another specific installer to be registered first.
    /// The required installer must have an earlier OperationOrder or be in the same group but processed earlier.
    /// </summary>
    /// <typeparam name="TRequiredInstaller">The installer type that must be registered first</typeparam>
    public interface IRequiresInstaller<TRequiredInstaller> : IHasInstallerRequirements
        where TRequiredInstaller : IGeniesInstaller
    {
        // Marker interface - no implementation needed
    }
}
