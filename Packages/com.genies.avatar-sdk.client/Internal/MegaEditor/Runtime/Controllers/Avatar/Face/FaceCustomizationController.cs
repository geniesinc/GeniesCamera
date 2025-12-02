using Cysharp.Threading.Tasks;
using Genies.Customization.Framework;
using Genies.Customization.Framework.Navigation;
using Genies.Inventory;
using Toolbox.Core;
using UnityEngine;

namespace Genies.Customization.MegaEditor
{
    public class FaceCustomizationController : BaseCustomizationController
    {
        //Navigation noder per types registered
        [SerializeField]
        private SerializedDictionary<AvatarBaseCategory, CustomizationConfig> _registeredNodesPerCategory;

        public override UniTask<bool> TryToInitialize(Customizer customizer)
        {
            _customizer = customizer;
            return UniTask.FromResult(true);
        }

        public override async void StartCustomization()
        {
            var nodeToTransition = ResolveNavigationNode();
            _customizer.GoToNode(nodeToTransition.Item1, false);
            await UniTask.Delay(1);
            _customizer.SetSelectedNavBarIndex(nodeToTransition.Item2);
        }

        private (INavigationNode, int) ResolveNavigationNode()
        {
            AvatarBaseCategory category = CustomizationContext.CurrentDnaCustomizationViewState;
            if (_registeredNodesPerCategory.TryGetValue(category, out CustomizationConfig configSelected))
            {
                for (int i = 0; i < _customizer.CurrentNode.Children.Count; i++)
                {
                    var childNode = _customizer.CurrentNode.Children[i];
                    if (childNode.Controller.BreadcrumbName.Equals(configSelected.BreadcrumbName))
                    {
                        return (childNode, i);
                    }

                }
            }

            return (_customizer.CurrentNode.Children[0], 0);
        }

        public override void StopCustomization()
        {

        }

        public override void Dispose()
        {

        }
    }
}
