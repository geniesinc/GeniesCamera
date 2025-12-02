using Cysharp.Threading.Tasks;

namespace Genies.Avatars.Context
{
    public interface IAvatarDefinitionSource
    {
        UniTask<string> GetDefinitionAsync();
    }
}