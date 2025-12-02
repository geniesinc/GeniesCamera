namespace Genies.ServiceManagement
{
    /// <summary>
    /// Default groups for installation/resolving.
    /// </summary>
    public class DefaultInstallationGroups
    {
        public const int CoreDependency = -3000;
        public const int Configuration = -2000;
        public const int CoreServices = -1000;
        public const int PostCoreServices = -500;
        public const int DefaultServices = 0;
    }
}
