using Cysharp.Threading.Tasks;
using Genies.Customization.Framework;

namespace Genies.Customization.MegaEditor
{
    public class TransitionNodeCustomizationController : BaseCustomizationController
    {
        public override UniTask<bool> TryToInitialize(Customizer customizer)
        {
            _customizer = customizer;
            return UniTask.FromResult(true);
        }

        public override void StartCustomization()
        {
        }

        public override void StopCustomization()
        {

        }

        public override void Dispose()
        {

        }

    }
}
