using Cysharp.Threading.Tasks;

namespace Genies.Utilities
{
    public interface IOperationQueueFrameTiming
    {
        string Name { get; }
        
        UniTask Yield();
    }
}
