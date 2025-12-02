using System.Threading;
using Cysharp.Threading.Tasks;

namespace Genies.Customization.Framework
{
    public interface ICommand
    {
        UniTask ExecuteAsync(CancellationToken cancellationToken = default);
        UniTask UndoAsync(CancellationToken cancellationToken = default);
    }
}