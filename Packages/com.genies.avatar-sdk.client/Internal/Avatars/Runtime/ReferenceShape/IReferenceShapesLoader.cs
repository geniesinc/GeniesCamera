using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Genies.Avatars
{
    public interface IReferenceShapesLoader
    {
        UniTask<Dictionary<string, IReferenceShape>> LoadShapesAsync();
    }
}