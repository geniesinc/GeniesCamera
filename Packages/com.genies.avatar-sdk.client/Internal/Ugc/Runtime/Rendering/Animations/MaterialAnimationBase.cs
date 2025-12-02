using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Ugc
{
    public abstract class MaterialAnimationBase : IMaterialAnimation
    {
        public Material Material
        {
            get => _material;
            set
            {
                Stop();
                _material = value;
            }
        }
        
        public bool IsPlaying => _cancellationSource != null;
        
        private Material _material;
        private CancellationTokenSource _cancellationSource;
        
        public MaterialAnimationBase() { }
        public MaterialAnimationBase(Material material)
        {
            _material = material;
        }
        
        // must never restore the material state and must not throw if cancelled
        protected abstract UniTask PlayAsync(ValueAnimation animation, Material material, CancellationToken cancellationToken);
        protected abstract void RestoreMaterialState(Material material);

        public async UniTask PlayAsync(ValueAnimation animation, bool ignoreIfPlaying = false)
        {
            if (!_material || (ignoreIfPlaying && IsPlaying))
            {
                return;
            }

            Stop();
            
            _cancellationSource = new CancellationTokenSource();
            CancellationToken cancellationToken = _cancellationSource.Token;
            
            await PlayAsync(animation, _material, cancellationToken);
            
            if (!cancellationToken.IsCancellationRequested)
            {
                RestoreMaterialState(_material);
            }

            _cancellationSource?.Dispose();
            _cancellationSource = null;
        }

        public void Stop()
        {
            Stop(true);
        }

        public void StopNoRestore()
        {
            Stop(false);
        }

        private void Stop(bool restoreMaterialState)
        {
            if (_cancellationSource is null)
            {
                return;
            }

            _cancellationSource.Cancel();
            _cancellationSource.Dispose();
            _cancellationSource = null;
            
            if (restoreMaterialState)
            {
                RestoreMaterialState(_material);
            }
        }
    }
}
