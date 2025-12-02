using Genies.Customization.Framework.Navigation;
using Genies.FeatureFlags;
using Genies.ServiceManagement;
using UnityEngine;

namespace Genies.Looks.Customization.GraphEvaluators
{
    
    /// <summary>
    /// Evaluates which node to go to next depending on whether the selected feature flag is set
    /// </summary>
    [CreateNodeMenu("Customizer UI/Evaluators/Feature Flag")]
    public class FeatureFlagEvaluator : NavigationEvaluationNode
    {
        private IFeatureFlagsManager _FeatureFlagsManager => ServiceManager.Get<IFeatureFlagsManager>();

        [SerializeField]
        private SerializableFeatureFlag _featureFlag;

        [Output(connectionType = ConnectionType.Override)]
        public BaseNavigationNode FeatureFlagOn;
        [Output(connectionType = ConnectionType.Override)]
        public BaseNavigationNode FeatureFlagOff;

        protected override INavigationNode EvaluateAndGetNext()
        {
            var flag = _FeatureFlagsManager is not null && _FeatureFlagsManager.IsFeatureEnabled(_featureFlag);

            if (flag)
            {
                return FeatureFlagOn;
            }

            return FeatureFlagOff;
        }

        public override void Link()
        {
            foreach (var port in Ports)
            {
                if (port.fieldName == nameof(FeatureFlagOn))
                {
                    FeatureFlagOn = (BaseNavigationNode)port.Connection?.node;
                }

                if (port.fieldName == nameof(FeatureFlagOff))
                {
                    FeatureFlagOff = (BaseNavigationNode)port.Connection?.node;
                }
            }
        }
    }
}
