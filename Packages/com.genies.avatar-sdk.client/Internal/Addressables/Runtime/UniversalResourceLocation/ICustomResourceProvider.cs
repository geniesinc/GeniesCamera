using Cysharp.Threading.Tasks;
using Genies.Refs;
using UnityEngine;

namespace Genies.Addressables.UniversalResourceLocation
{
    public interface ICustomResourceProvider
    {
        public UniTask<Ref<Sprite>> Provide(string internalId);
    }
}
