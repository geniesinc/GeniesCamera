using System.Threading;
using Cysharp.Threading.Tasks;

namespace Genies.Utilities
{
    public sealed class PlayerLoopFrameTiming : IOperationQueueFrameTiming
    {
        public string Name { get; }
        
        public readonly PlayerLoopTiming Timing;

        public PlayerLoopFrameTiming(PlayerLoopTiming timing)
        {
            Timing = timing;
            Name = timing.ToString();
        }
        
        public UniTask Yield()
        {
            return UniTask.Yield(Timing, CancellationToken.None);
        }
    }
}
