using Cysharp.Threading.Tasks;
using VContainer;

namespace Genies.ServiceManagement
{
    /// <summary>
    /// Interface for implementing initialization logic in your installer, will always be called after <see cref="IGeniesInstaller"/>s
    /// of the same group. The <see cref="InitializationOrder"/> can be overridden to specify initialization order in the same group.
    /// by default all initializers in a group won't depend on one another unless they have different <see cref="InitializationOrder"/> this means
    /// that they will all be called at the same time using <see cref="UniTask.WhenAll"/>
    /// </summary>
    public interface IGeniesInitializer : IGroupedOperation
    {
        int InitializationOrder => 0;
        UniTask Initialize();
    }
}
