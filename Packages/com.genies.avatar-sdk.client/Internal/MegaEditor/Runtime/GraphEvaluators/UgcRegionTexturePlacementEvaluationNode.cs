using Genies.Customization.MegaEditor;
using Genies.Customization.Framework.Navigation;

namespace Genies.Looks.Customization.GraphEvaluators
{
    /// <summary>
    /// Evaluates which node to go to next depending on whether users click the items under the PlaceImage tab or the Area tab.
    /// </summary>
    [CreateNodeMenu("Customizer UI/Evaluators/Ugc Region Texture Placement Evaluation Node")]
    public class UgcRegionTexturePlacementEvaluationNode : NavigationEvaluationNode
    {
        [Output(connectionType = ConnectionType.Override)]
        public BaseNavigationNode IsGoingToStyles;
        [Output(connectionType = ConnectionType.Override)]
        public BaseNavigationNode IsGoingToPlaceImage;

        protected override INavigationNode EvaluateAndGetNext()
        {
            var regionEditState = CustomizationContext.CurrentUgcRegionEditState;

            if (regionEditState == UgcRegionEditState.Regions)
            {
                return IsGoingToStyles;
            }

            return IsGoingToPlaceImage;
        }

        public override void Link()
        {
            foreach (var port in Ports)
            {
                if (port.fieldName == nameof(IsGoingToStyles))
                {
                    IsGoingToStyles = (BaseNavigationNode)port.Connection?.node;
                }


                if (port.fieldName == nameof(IsGoingToPlaceImage))
                {
                    IsGoingToPlaceImage = (BaseNavigationNode)port.Connection?.node;
                }
            }
        }
    }
}
