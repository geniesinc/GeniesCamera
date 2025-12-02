using Genies.Components.Dynamics;
using Genies.FeatureFlags;
using Genies.ServiceManagement;
using UnityEngine;

namespace Genies.Dynamics.Assets
{
    /// <summary>
    /// Handles the enabling and disabling of the Dynamics system based on the feature flag setting.
    /// </summary>
    public class GeniesDynamicsFeatureFlagChecker : MonoBehaviour
    {
        private IFeatureFlagsManager _FeatureFlagsManager => ServiceManager.Get<IFeatureFlagsManager>();

        [SerializeField] private DynamicsManager _dynamicsManager;

        private void Start()
        {
            _dynamicsManager.OnCheckDynamicsEnabled += CheckDynamicsEnabled;
        }

        private bool CheckDynamicsEnabled() => true; //we merge the Feature Flag, so it always be available

        private void OnDestroy()
        {
            _dynamicsManager.OnCheckDynamicsEnabled -= CheckDynamicsEnabled;
        }
    }
}
