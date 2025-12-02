using Cysharp.Threading.Tasks;
using Genies.Refs;

namespace Genies.Components.ShaderlessTools
{
    public interface IShaderlessAssetService
    {
        public UniTask<Ref<T>> LoadShadersAsync<T>(Ref<T> assetRef);
    }
}
