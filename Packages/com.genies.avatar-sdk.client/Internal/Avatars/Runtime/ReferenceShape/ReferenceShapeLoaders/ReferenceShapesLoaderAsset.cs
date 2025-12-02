using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Genies.Avatars
{
    public abstract class ReferenceShapesLoaderAsset : ScriptableObject, IReferenceShapesLoader
    {
        public abstract UniTask<Dictionary<string, IReferenceShape>> LoadShapesAsync();
    }
}