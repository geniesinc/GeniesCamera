using UnityEngine;
using System;
using System.Collections.Generic;

namespace Genies.ServiceManagement
{
    /// <summary>
    /// A serialized settings object with configuration for all class marked with <see cref="AutoResolveAttribute"/>
    /// the settings will be serialized an instance will be stored for each auto resolver. This is also used to enable/disable
    /// specific auto resolvers as needed.
    /// </summary>
#if GENIES_INTERNAL
    [CreateAssetMenu(fileName = "AutoResolverSettings", menuName = "Genies/AutoResolverSettings", order = 1)]
#endif
    public class AutoResolverSettings : ScriptableObject
    {
        [SerializeField]
        internal List<ResolverSettings> ResolverSettingsList = new List<ResolverSettings>();

        /// <summary>
        /// Is the auto resolver installation enabled?
        /// </summary>
        /// <param name="type"> Type of the auto resolver </param>
        /// <returns></returns>
        public bool IsInstallationEnabled(Type type)
        {
            var resolverSetting = ResolverSettingsList.Find(x => x.ResolverType == type);
            return resolverSetting == null || resolverSetting.IsInstallerEnabled;
        }

        /// <summary>
        /// Is the auto resolver initialization enabled?
        /// </summary>
        /// <param name="type"> Type of the auto resolver </param>
        /// <returns></returns>
        public bool IsInitializationEnabled(Type type)
        {
            var resolverSetting = ResolverSettingsList.Find(x => x.ResolverType == type);
            return resolverSetting == null || resolverSetting.IsInitializerEnabled;
        }


        /// <summary>
        /// Returns the serialized instance for that resolver type
        /// </summary>
        /// <param name="type"> Auto resolver type </param>
        /// <returns></returns>
        public object GetResolverInstance(Type type)
        {
            var resolverSetting = ResolverSettingsList.Find(x => x.ResolverType == type);
            return resolverSetting != null ? resolverSetting.ResolverInstance : null;
        }
    }

    [Serializable]
    internal class ResolverSettings
    {
        public Type ResolverType => ResolverInstance?.GetType();

        public string ResolverTypeName => ResolverType != null ? ResolverType.FullName : string.Empty;

        public bool IsInstallerEnabled = true;
        public bool IsInitializerEnabled = true;

        /// <summary>
        /// Serialize Reference handle serializing non unity objects by reference
        /// </summary>
        [SerializeReference]
        public object ResolverInstance;
    }
}
