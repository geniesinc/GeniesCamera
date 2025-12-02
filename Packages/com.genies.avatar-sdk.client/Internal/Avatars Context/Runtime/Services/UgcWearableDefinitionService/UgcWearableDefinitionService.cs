using System;
using Cysharp.Threading.Tasks;
using Genies.Wearables;
using Newtonsoft.Json;
using UnityEngine;
using IUgcWearableDefinitionService = Genies.Ugc.IUgcWearableDefinitionService;

namespace Genies.Avatars.Context
{
    /// <summary>
    /// Fetches <see cref="Wearable"/>s from any given implementation of the <see cref="IWearableService"/>.
    /// </summary>
    public sealed class UgcWearableDefinitionService : IUgcWearableDefinitionService
    {
        // dependencies
        private readonly IWearableService _wearableService;

        public UgcWearableDefinitionService(IWearableService wearableService)
        {
            _wearableService = wearableService;
        }

        public async UniTask<Ugc.Wearable> FetchAsync(string wearableId)
        {
            // try to load the wearable from the API
            Genies.Services.Model.Wearable wearableModel = await _wearableService.GetWearableByIdAsync(wearableId);
            if (wearableModel is null)
            {
                return TryLoadFromAvatarEmbeddedData(wearableId);
            }

            // get the definition
            string serializedDefinition = wearableModel.WearableDefinition;
            if (string.IsNullOrEmpty(serializedDefinition))
            {
                return TryLoadFromAvatarEmbeddedData(wearableId);
            }

            // try to deserialize the definition
            try
            {
                Ugc.Wearable wearable = JsonConvert.DeserializeObject<Ugc.Wearable>(serializedDefinition);

                // make sure to register this in the avatar embedded avatar if succesfully loaded
                if (wearable is not null)
                {
                    AvatarEmbeddedData.SetData(wearableId, wearable);
                }

                return wearable;
            }
            catch (Exception exception)
            {
                Debug.LogError($"[{nameof(UgcWearableDefinitionService)}] could not deserialize the wearable definition for ID {wearableId}.\n{exception}");
                return null;
            }
        }

        private Ugc.Wearable TryLoadFromAvatarEmbeddedData(string wearableId)
        {
            return AvatarEmbeddedData.TryGetData(wearableId, out Ugc.Wearable wearable) ? wearable : null;
        }
    }
}
