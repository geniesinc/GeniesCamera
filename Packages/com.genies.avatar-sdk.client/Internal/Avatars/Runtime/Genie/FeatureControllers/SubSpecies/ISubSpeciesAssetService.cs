using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Models;
using Genies.Refs;
using UnityEngine;

namespace Genies.Avatars
{
    public interface ISubSpeciesAssetService
    {
        UniTask<Ref<SubSpeciesContainer>> LoadContainerAsync(string id, string lod = AssetLod.Default);
    }
}
