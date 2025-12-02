using System.Threading;
using Cysharp.Threading.Tasks;
using Genies.Avatars;

using Genies.CrashReporting;
using Genies.Customization.Framework;

namespace Genies.Looks.Customization.Commands
{
    /// <summary>
    /// Base command for any avatar modifications. Ensures the avatar is rebuilt
    /// after every modification.
    /// </summary>
    public abstract class UnifiedGenieModificationCommand : ICommand
    {
        private readonly UnifiedGenieController _controller;

        public UnifiedGenieModificationCommand(UnifiedGenieController controller)
        {
            _controller = controller;
        }

        public async UniTask ExecuteAsync(CancellationToken cancellationToken = default)
        {
            if (_controller == null)
            {
                CrashReporter.LogHandledException(new InvalidAvatarModificationException("Invalid unified controller"));
                return;
            }

            await ExecuteModificationAsync(_controller);

            if (cancellationToken != default)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }

            await _controller.RebuildGenieAsync();
        }

        public async UniTask UndoAsync(CancellationToken cancellationToken = default)
        {
            if (_controller == null)
            {
                CrashReporter.LogHandledException(new InvalidAvatarModificationException("Invalid unified controller"));
                return;
            }

            await UndoModificationAsync(_controller);

            if (cancellationToken != default)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
            }

            await _controller.RebuildGenieAsync();
        }

        protected abstract UniTask ExecuteModificationAsync(UnifiedGenieController controller);
        protected abstract UniTask UndoModificationAsync(UnifiedGenieController controller);
    }
}
