using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Ugc
{
    public interface IMaterialAnimation
    {
        Material Material { get; set; }
        bool IsPlaying { get; }
        
        UniTask PlayAsync(ValueAnimation animation, bool ignoreIfPlaying = false);
        void Stop();
        void StopNoRestore();
    }
}
