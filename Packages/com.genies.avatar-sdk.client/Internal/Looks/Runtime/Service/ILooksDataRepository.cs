using Genies.DataRepositoryFramework;
using Genies.Looks.Models;

namespace Genies.Looks.Service
{
    /// <summary>
    /// Data repository for retrieving, creating, updating and delete looks. Using an interface
    /// so that the implementer is able to mock the implementation with local data.
    /// </summary>
    public interface ILooksDataRepository : IDataRepository<LookData>
    {

    }
}
