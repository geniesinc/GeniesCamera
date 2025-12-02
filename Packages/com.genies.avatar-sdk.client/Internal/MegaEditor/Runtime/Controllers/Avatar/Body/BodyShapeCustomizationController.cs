using Cysharp.Threading.Tasks;
using Genies.Customization.Framework;

namespace Genies.Customization.MegaEditor
{
    /// <summary>
    /// Business logic for the body 'shape' node
    /// </summary>
    /// <remarks>Just passes the control along to presets for now</remarks>
    public class BodyShapeCustomizationController : BaseCustomizationController
    {
        public override UniTask<bool> TryToInitialize(Customizer customizer)
        {
            _customizer = customizer;
            return UniTask.FromResult(true);
        }

        public override async void StartCustomization()
        {
            _customizer.GoToNode(_customizer.CurrentNode.Children[0], false);
            // wait until navigation is initialized
            await UniTask.Delay(1);
            _customizer.SetSelectedNavBarIndex(0);
        }

        public override void StopCustomization()
        {

        }

        public override void Dispose()
        {

        }

    }
}
