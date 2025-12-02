using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Genies.CrashReporting;
using Genies.Events;
using Genies.Login.Native;
using Genies.Services.Api;
using Genies.Services.Client;
using Genies.Services.Configs;
using Genies.Utilities;
using UnityEngine;
using Avatar = Genies.Services.Model.Avatar;
using Newtonsoft.Json;
namespace Genies.Avatars.Services
{
    /// <summary>
    /// Default implementation of <see cref="IAvatarService"/> that provides avatar management functionality through remote APIs.
    /// This service handles avatar creation, retrieval, updates, and synchronization with backend systems.
    /// </summary>
    public class AvatarService : IAvatarService
    {
        private readonly IAvatarApi _avatarClient;
        private readonly AvatarServiceApiPathResolver _avatarServiceApiPathResolver = new();
        private UniTaskCompletionSource _apiInitializationSource;
        private readonly JsonSerializerSettings _serializerSettings = new() { Formatting = Formatting.Indented, };
        private UniTaskCompletionSource<Genies.Naf.AvatarDefinition> _avatarLoadingTaskCompletionSource;
        private readonly IDefinitionConverter _definitionManager = new NafAvatarDefinitionConverter();
        private Genies.Naf.AvatarDefinition LoadedDefinition { get; set; }

        private Avatar _loadedAvatar;
        
        /// <summary>
        /// Gets or sets the currently loaded avatar, automatically handling definition deserialization and error recovery.
        /// </summary>
        public Avatar LoadedAvatar
        {
            get => _loadedAvatar;
            private set
            {
                _loadedAvatar = value;
                if (_loadedAvatar == null || string.IsNullOrEmpty(_loadedAvatar.Definition))
                {
                    return;
                }

                try
                {
                    LoadedDefinition = JsonConvert.DeserializeObject<Genies.Naf.AvatarDefinition>(_loadedAvatar.Definition);
                }
                catch (JsonSerializationException jse)
                {
                    CrashReporter.Log($"Error deserializing Avatar Definition: {jse.Message}. Generate default.",
                        LogSeverity.Error);
                    CrashReporter.LogHandledException(jse);
                    // probably a legacy definition, load default
                    LoadedDefinition = NafAvatarExtensions.DefaultDefinition();
                }
                catch (AggregateException ae)
                {
                    CrashReporter.LogHandledException(ae);
                    LoadedDefinition = NafAvatarExtensions.DefaultDefinition();
                }
                catch (Exception exception)
                {
                    CrashReporter.LogHandledException(exception);
                    LoadedDefinition = NafAvatarExtensions.DefaultDefinition();
                }
            }
        }

        private EventBus EventBus => EventBusSingleton.EventBus;

        /// <summary>
        /// Initializes a new instance of the <see cref="AvatarService"/> class with default configuration.
        /// Creates an avatar API client using the configured target environment.
        /// </summary>
        public AvatarService()
        {
            var avatarConfig = new Configuration()
            {
                BasePath = _avatarServiceApiPathResolver.GetApiBaseUrl(GeniesApiConfigManager.TargetEnvironment),
            };
            _avatarClient = new AvatarApi(avatarConfig);

            AwaitApiInitialization().Forget();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AvatarService"/> class with a custom avatar API client.
        /// This constructor is primarily used for testing with mock implementations.
        /// </summary>
        /// <param name="avatarApi">The avatar API client to use for backend communication.</param>
        public AvatarService(IAvatarApi avatarApi)
        {
            _avatarClient = avatarApi;
            AwaitApiInitialization().Forget();
        }

        /// <summary>
        /// Waits for the avatar service to be fully initialized and ready for API calls.
        /// This includes authentication setup and API client configuration.
        /// </summary>
        /// <returns>A task that completes when the service is initialized.</returns>
        public virtual UniTask WaitUntilInitializedAsync()
        {
            if (_apiInitializationSource == null)
            {
                return UniTask.CompletedTask;
            }

            return _apiInitializationSource.Task;
        }

        private async UniTask AwaitApiInitialization()
        {
            if (_apiInitializationSource != null)
            {
                await _apiInitializationSource.Task;
                return;
            }

            _apiInitializationSource = new UniTaskCompletionSource();
            //Await auth access token being set.
            await UniTask.WaitUntil(GeniesLoginSdk.IsUserSignedIn);
            await UniTask.WaitUntil(()=>_avatarClient != null);
            await UniTask.WaitUntil(()=>_avatarClient.Configuration != null);

            _avatarClient.Configuration.AccessToken = GeniesLoginSdk.AuthAccessToken;

            // When the token refreshes, use it.
            GeniesLoginSdk.OnTokenRefresh += (token) =>
            {
                _avatarClient.Configuration.AccessToken = token;
            };
            
            _apiInitializationSource.TrySetResult();
            _apiInitializationSource = null;
        }

        // Note the user has to be signed in order to create an Avatar
        public async UniTask<bool> CreateAvatarAsync(string gender)
        {
            var userId = await GeniesLoginSdk.GetUserIdAsync();
            var status = Avatar.StatusEnum.Visible;
            var avatarGender = AvatarUtils.ConvertFromStringToAvatarGender(gender);
            var definition = AvatarExtensions.GetDefaultDefinitionForGender(gender);
            var definitionJson = JsonConvert.SerializeObject(definition, _serializerSettings);
            try
            {
                LoadedAvatar = await CreateAvatar(userId, status, avatarGender, definition: definitionJson);
            }
            catch (Exception e)
            {
                Debug.LogError($"Create Avatar failed: {e.Message}");
            }

            return (LoadedAvatar != null && !string.IsNullOrEmpty(LoadedAvatar.Definition));
        }

        // Note the user has to be signed in in order to create an Avatar
        public async UniTask<bool> CreateAvatarAsync(Naf.AvatarDefinition avatarDefinition)
        {
            var userId = await GeniesLoginSdk.GetUserIdAsync();
            var status = Avatar.StatusEnum.Visible;

            var avatarGender = AvatarUtils.ConvertFromStringToAvatarGender(avatarDefinition.BinaryGenderStringFromDna());

            try
            {
                LoadedAvatar = await CreateAvatar(userId, status, avatarGender,
                    definition: JsonConvert.SerializeObject(avatarDefinition, _serializerSettings));
            }
            catch (Exception e)
            {
                Debug.LogError($"Create Avatar failed: {e.Message}");
            }

            return (LoadedAvatar != null && !string.IsNullOrEmpty(LoadedAvatar.Definition));
        }

        private async UniTask<Avatar> CreateAvatar(
            string userId,
            Avatar.StatusEnum? status,
            Avatar.GenderEnum? gender,
            string definition)
        {
            var avatar = new Avatar(userId, status, gender, definition: definition);

            try
            {
                avatar = await _avatarClient.CreateAvatarAsync(avatar);
            }
            catch (ApiException e)
            {
                avatar = null;
                CrashReporter.LogHandledException(e);
            }
            catch (Exception e)
            {
                avatar = null;
                CrashReporter.LogHandledException(e);
            }

            if (avatar != null)
            {
                EventBus.Fire(AvatarServiceEvents.SyncAvatarCloudDefinition, avatar.Definition);
            }

            return avatar;
        }

        public async UniTask<Genies.Naf.AvatarDefinition> GetAvatarDefinitionAsync()
        {
            await WaitUntilInitializedAsync();
            if (LoadedDefinition != null)
            {
                return LoadedDefinition;
            }

            if (_avatarLoadingTaskCompletionSource != null)
            {
                return await _avatarLoadingTaskCompletionSource.Task;
            }

            _avatarLoadingTaskCompletionSource = new UniTaskCompletionSource<Genies.Naf.AvatarDefinition>();
            Avatar avatar = null;
            string userId = await GeniesLoginSdk.GetUserIdAsync();
            try
            {
                var avatars = await _avatarClient.GetAvatarsAsync(userId);
                if (avatars.Count > 0)
                {
                    // This will ensure you get the latest avatar
                    avatar = avatars[0];
                    EventBus.Fire(AvatarServiceEvents.SyncAvatarCloudDefinition, avatar.Definition);
                }
            }
            catch (ApiException e)
            {
                CrashReporter.LogHandledException(e);
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
            }

            LoadedAvatar = avatar;

            // before returning the avatar, we check if the definition is up to date
            if (LoadedDefinition != null &&
                !string.Equals(LoadedDefinition.JsonVersion, _definitionManager.TargetVersion))
            {
                var parseSuccessfully = DefinitionToken.TryParse(LoadedAvatar?.Definition,
                    out DefinitionToken oldDefToken, "JsonVersion");

                if (parseSuccessfully)
                {
                    DefinitionToken result =
                        await _definitionManager.ConvertAsync(oldDefToken, _definitionManager.TargetVersion);
                    LoadedDefinition = result.Token.ToObject<Naf.AvatarDefinition>();

                    // Send the converted version for the backend
                    Debug.Log("[AvatarService] Avatar definition converted from: " +
                        LoadedDefinition.JsonVersion + " to: " + _definitionManager.TargetVersion);
                    await UpdateAvatarAsync(LoadedDefinition);
                }
                else
                {
                    CrashReporter.LogError(
                        $"Cannot convert the avatar definition from: {LoadedDefinition.JsonVersion} to {_definitionManager.TargetVersion} ");
                }
            }

            _avatarLoadingTaskCompletionSource.TrySetResult(LoadedDefinition);
            _avatarLoadingTaskCompletionSource = null;

            return LoadedDefinition;
        }

        public async UniTask<Naf.AvatarDefinition> GetUserAvatarDefinitionAsync(string userId)
        {
            await WaitUntilInitializedAsync();

            Avatar avatar = null;
            Naf.AvatarDefinition definition = null;
            try
            {
                var avatars = await _avatarClient.GetAvatarsAsync(userId);
                if (avatars.Count > 0)
                {
                    // This will ensure you get the latest avatar
                    avatar = avatars[0];
                }

                if (avatar == null || string.IsNullOrEmpty(avatar.Definition))
                {
                    return null;
                }

                definition = await NafAvatarExtensions.DeserializeToAvatarDefinitionAsync(avatar.Definition);
            }
            catch (ApiException e)
            {
                CrashReporter.LogHandledException(e);
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
            }
            return definition;
        }

        public async UniTask<List<Avatar>> GetUserAvatarsAsync(string userId)
        {
            List<Avatar> avatars = new();
            try
            {
                avatars = await _avatarClient.GetAvatarsAsync(userId);
            }
            catch (ApiException e)
            {
                CrashReporter.LogHandledException(e);
            }
            catch (Exception e)
            {
                CrashReporter.LogHandledException(e);
            }
            return avatars;
        }

        public async UniTask UpdateAvatarAsync(Genies.Naf.AvatarDefinition avatarDefinition)
        {
            // TODO - Find a better way to fix first time authv2 token issues
            // When a user signs up and logs in for the first time using email OTP
            // the token they get doesnt allow the user to create/update an avatar.
            // These commands will still succeed as far as the client is concerned.
            // Refreshing the token here fixes the issue.
            await GeniesLoginSdk.RefreshTokensAsync();

            if (LoadedAvatar == null)
            {
                var userId = await GeniesLoginSdk.GetUserIdAsync();
                var gender = NafAvatarExtensions.BinaryGenderStringFromDna(avatarDefinition);
                var status = Avatar.StatusEnum.Visible;
                var avatarGender = AvatarUtils.ConvertFromStringToAvatarGender(gender);
                var definitionJson = JsonConvert.SerializeObject(avatarDefinition, _serializerSettings);
                LoadedAvatar = await CreateAvatar(userId, status, avatarGender, definition: definitionJson);
                LoadedDefinition = avatarDefinition;
                LoadedAvatar.Definition = JsonConvert.SerializeObject(avatarDefinition, _serializerSettings);
            }
            else
            {
                LoadedDefinition = avatarDefinition;
                LoadedAvatar.Definition = JsonConvert.SerializeObject(avatarDefinition, _serializerSettings);
            }

            await UpdateAvatarAsync(LoadedAvatar);
        }

        public async UniTask UpdateAvatarAsync(Avatar avatar)
        {
            if (avatar == null)
            {
                return;
            }

            string userId = await GeniesLoginSdk.GetUserIdAsync();
            EventBus.Fire(AvatarServiceEvents.SyncAvatarCloudDefinition, avatar.Definition);
            try
            {
                await _avatarClient.UpdateAvatarAsync(avatar, avatar.AvatarId, userId);
                LoadedAvatar = avatar;
            }
            catch (ApiException e)
            {
                Debug.LogError("UpdateAvatarAsync: " + e.Message);
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        public async UniTask<string> UploadAvatarImageAsync(byte[] imageData, string avatarId)
        {
            if (imageData == null || imageData.Length == 0)
            {
                throw new ArgumentException("Image data cannot be null or empty", nameof(imageData));
            }

            if (string.IsNullOrEmpty(avatarId))
            {
                throw new ArgumentException("Avatar ID cannot be null or empty", nameof(avatarId));
            }

            try
            {
                var response = await _avatarClient.UploadAvatarImageAsync(imageData, avatarId);
                return response.ImageUrl;
            }
            catch (Exception e)
            {
                CrashReporter.LogError($"Failed to upload avatar image: {e.Message}");
                throw;
            }
        }
        
        public async UniTask<Naf.AvatarDefinition> GetOrCreateAvatarAsync(string bodyType)
        {
            if (LoadedAvatar != null && LoadedDefinition != null)
            {
                return LoadedDefinition;
            }

            // the avatar doesn't exists, so we need to create a new avatar
            await CreateAvatarAsync(bodyType);

            if (LoadedAvatar == null)
            {
                Debug.Log("Error creating Avatar");
            }

            if (LoadedAvatar != null)
            {
                EventBus.Fire(AvatarServiceEvents.SyncAvatarCloudDefinition, LoadedDefinition);
            }

            return LoadedDefinition;
        }
        
        
    }
}
