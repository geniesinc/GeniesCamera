using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.Assets.Services;
using Genies.Models;
using Genies.Refs;
using UnityEngine;
using UnityEngine.Serialization;

namespace Genies.Avatars.Context
{
    /// <summary>
    /// Serializable <see cref="IAvatarLoader"/> implementation that offers some generic configuration parameters.
    /// It is the serializable version of <see cref="ConfigurableAvatarLoader"/>.
    /// </summary>
    [Serializable]
    public sealed class SerializableAvatarLoader : IAvatarLoader, IAvatarDefinitionSource
    {
        public string genieType = GenieTypeName.NonUma;
        public string lod = AssetLod.Default;
        public DefinitionSource avatarDefinitionSource = DefinitionSource.UnifiedDefault;
        public bool useNonUmaGenie;
        public List<GenieComponentAsset> components = new();
        public UmaGenie umaGeniePrefabOverride;
        [FormerlySerializedAs("nonUmaGeniePrefabOverride")] public EditableGenie _editableGeniePrefabOverride;
        public BodyTypeContainer bodyTypeContainerOverride;
        [Tooltip("For now we need to know what species this body type container override is from, to obtain some specific data when building the species asset")]
        public string bodyTypeContainerOverrideSpecies;
        [TextArea(3, 20)] public string embeddedDefinition;

        private readonly ConfigurableAvatarLoader _loader = new();
        private readonly UserAvatarDefinitionSource _userAvatarDefinitionSource = new();
        private readonly UserLookDefinitionSource _latestLookDefinitionSource = new(getDrafts: false);
        private readonly UserLookDefinitionSource _latestDraftDefinitionSource = new(getDrafts: true);

        public async UniTask<IGenie> LoadAsync(Transform parent = null)
        {
            ISpeciesGenieController controller = await LoadControllerAsync(parent);
            return controller?.Genie;
        }

        public UniTask<Ref<IGeniePrefab>> LoadAsPrefabAsync()
        {
            Debug.LogError($"[{nameof(SerializableAvatarLoader)}] this loader does not support loading as prefab");
            return UniTask.FromResult<Ref<IGeniePrefab>>(default);
        }

        public UniTask<ISpeciesGenieController> LoadControllerAsync(Transform parent = null)
        {
            _loader.Lod = lod;
            _loader.DefinitionSource = this;
            _loader.UseNonUmaGenie = useNonUmaGenie;
            _loader.Components = components;
            _loader.UmaGeniePrefabOverride = umaGeniePrefabOverride;
            _loader.EditableGeniePrefabOverride = _editableGeniePrefabOverride;

            if (bodyTypeContainerOverride)
            {
                Ref<BodyTypeContainer> containerRef = CreateRef.FromAny(bodyTypeContainerOverride); // dummy ref, does nothing when disposed
                SpeciesAsset speciesAsset = SpeciesLoader.GetAssetFromContainer(bodyTypeContainerOverrideSpecies, lod, containerRef, genieType);
                _loader.SpeciesAssetOverride = speciesAsset;
            }
            else
            {
                _loader.SpeciesAssetOverride = null;
            }

            return _loader.LoadControllerAsync(parent);
        }

        public async UniTask<string> GetDefinitionAsync()
        {
            string definition = avatarDefinitionSource switch
            {
                DefinitionSource.UnifiedDefault      => AvatarExtensions.SerializedDefaultDefinition(),
                DefinitionSource.DollDefault         => $"{{\"Species\":\"{GenieSpecies.Dolls}\"}}",
                DefinitionSource.Embedded            => embeddedDefinition,
                DefinitionSource.ClipboardDefinition => GUIUtility.systemCopyBuffer,
                DefinitionSource.UserAvatar          => await _userAvatarDefinitionSource.GetDefinitionAsync(),
                _ => throw new ArgumentOutOfRangeException()
            };

            return definition;
        }

        public enum DefinitionSource
        {
            UnifiedDefault = 0,
            DollDefault = 1,
            Embedded = 2,
            ClipboardDefinition = 3,
            UserAvatar = 6,
            [Obsolete("Looks API is obsolete. You should use the UserAvatar option instead")]
            LatestUserLook = 4,
            [Obsolete("Looks API is obsolete. You should use the UserAvatar option instead")]
            LatestUserDraft = 5,
        }
    }
}
