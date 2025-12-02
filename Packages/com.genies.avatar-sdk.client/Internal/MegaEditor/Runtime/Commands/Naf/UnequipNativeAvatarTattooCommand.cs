using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Customization.Framework;
using Genies.Naf;
using GnWrappers;

namespace Genies.Looks.Customization.Commands
{
    public class UnequipNativeAvatarTattooCommand : ICommand
    {
        private readonly NativeUnifiedGenieController _controller;
        private readonly MegaSkinTattooSlot           _slot;
        private readonly string                       _previousTattooGuid;

        public UnequipNativeAvatarTattooCommand(MegaSkinTattooSlot slot, NativeUnifiedGenieController controller)
        {
            _controller         = controller;
            _slot               = slot;
            _previousTattooGuid = controller.GetEquippedTattoo(slot);
        }

        public UniTask ExecuteAsync(CancellationToken cancellationToken = default)
        {
            return _controller.UnequipTattooAsync(_slot);
        }

        public UniTask UndoAsync(CancellationToken cancellationToken = default)
        {
            if (!string.IsNullOrEmpty(_previousTattooGuid))
            {
                return _controller.EquipTattooAsync(_slot, _previousTattooGuid);
            }

            return UniTask.CompletedTask;
        }
    }
}
