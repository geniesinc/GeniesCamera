namespace Genies.ServiceManagement
{
    public interface IGroupedOperation
    {
        /// <summary>
        /// Installers/Initializers with the same group will be installed into the same scope, the groups
        /// are in ascending order, lower groups get created and initialized first which allows later groups to
        /// access them using <see cref="ServiceManager"/> by default we have 3 groups in <see cref="DefaultInstallationGroups"/>
        /// </summary>
        int OperationOrder => DefaultInstallationGroups.DefaultServices;
    }
}
