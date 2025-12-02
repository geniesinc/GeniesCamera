namespace Genies.ServiceManagement
{
    /// <summary>
    /// Singleton lifetime scopes can only be created by <see cref="ServiceManager"/> and this class
    /// is just used as a way to tag those scopes.
    /// </summary>
    internal class GeniesSingletonLifetimeScope : GeniesRootLifetimeScope
    {
    }
}
