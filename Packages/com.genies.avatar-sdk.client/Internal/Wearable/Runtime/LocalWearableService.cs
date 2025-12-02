using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using Genies.S3Service;
using Genies.DataRepositoryFramework;
using Genies.Services.Model;
using Newtonsoft.Json;
using UnityEngine;
using ApiWearable = Genies.Services.Model.Wearable;

namespace Genies.Wearables
{
    /// <summary>
    /// Wearable service that stores wearables on disk.
    /// </summary>
    public class LocalWearableService : IWearableService
    {
        /// <summary>
        /// Similar to <see cref="ApiWearable"/> but exposes private properties.
        /// </summary>
        private class LocalWearable
        {
            [JsonProperty("wearableId")] public string WearableId { get; set; }
            [JsonProperty("created")] public decimal? Created { get; set; }
            [JsonProperty("lastModified")] public decimal? LastModified { get; set; }
            [JsonProperty("fullAssetName")] public string FullAssetName { get; set; }
            [JsonProperty("displayName")] public string DisplayName { get; set; }
            [JsonProperty("baseAssetName")] public string BaseAssetName { get; set; }
            [JsonProperty("category")] public string Category { get; set; }
            [JsonProperty("tags")] public List<string> Tags { get; set; }
            [JsonProperty("iconUrl")] public string IconUrl { get; set; }
            [JsonProperty("description")] public string Description { get; set; }
            [JsonProperty("wearableDefinition")] public string WearableDefinition { get; set; }
            [JsonProperty("createdBy")] public string CreatedBy { get; set; }

            /// <summary>
            /// Hacky way to convert to api wearable, just serialize then deserialize.
            /// </summary>
            public ApiWearable ToApiWearable()
            {
                var toJson = JsonConvert.SerializeObject(this);
                return JsonConvert.DeserializeObject<ApiWearable>(toJson);
            }
        }

        /// <summary>
        /// Local storage repository
        /// </summary>
        private readonly LocalDiskDataRepository<LocalWearable> _wearablesStorage = new LocalDiskDataRepository<LocalWearable>("WearablesStorage", wearable => wearable.WearableId);

        /// <summary>
        /// Local s3 service, for "uploading" wearable icons.
        /// </summary>
        private readonly GeniesLocalS3Service _localS3Service = new GeniesLocalS3Service();

        private async UniTask<string> GenerateValidGuid()
        {
            var guid       = Guid.NewGuid().ToString();
            var currentIds = await _wearablesStorage.GetIdsAsync();

            while (currentIds.Contains(guid))
            {
                guid = Guid.NewGuid().ToString();
            }

            return guid;
        }

        /// <inheritdoc />
        public UniTask<string> CreateWearableAsync(Ugc.Wearable wearable, byte[] icon, string wearableId = null, bool isThriftable = true)
        {
            string wearableJson = JsonConvert.SerializeObject(wearable);
            return CreateWearableAsync(wearable.TemplateId, wearableId, wearableJson, icon);
        }

        /// <summary>
        /// Creates a new wearable in local storage with the specified template, JSON data, and icon.
        /// </summary>
        /// <param name="templateId">The template identifier for the wearable type.</param>
        /// <param name="wearableId">Optional existing wearable ID for updates, null for new creation.</param>
        /// <param name="wearableJson">The wearable configuration data as JSON string.</param>
        /// <param name="icon">The icon image data as a byte array.</param>
        /// <returns>A task that completes with the wearable ID of the created or updated wearable.</returns>
        private async UniTask<string> CreateWearableAsync(string templateId, string wearableId, string wearableJson, byte[] icon)
        {
            wearableId = string.IsNullOrEmpty(wearableId) ? await GenerateValidGuid() : wearableId;
            var iconPath = Path.Combine("Wearables", $"{wearableId}.png");
            var iconUrl  = await _localS3Service.UploadObject(iconPath, icon);

            //Get epoch
            var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            var diff   = DateTime.Now.ToUniversalTime() - origin;
            var epoch  = Math.Floor(diff.TotalSeconds);

            decimal? creationTime = null;
            decimal? lastModified = null;

            var existingRecord = await _wearablesStorage.GetByIdAsync(wearableId);
            if (existingRecord != null)
            {
                creationTime = existingRecord.Created;
                lastModified = (decimal)epoch;
            }
            else
            {
                creationTime = (decimal)epoch;
            }

            var wearable = new LocalWearable
            {
                WearableId = wearableId,
                FullAssetName = templateId,
                DisplayName = templateId,
                BaseAssetName = templateId,
                Category = GetCategoryFromId(templateId),
                Tags = null,
                IconUrl = iconUrl,
                WearableDefinition = wearableJson,
                CreatedBy = "local-user",
                LastModified = lastModified,
                Created = creationTime
            };

            var created = await _wearablesStorage.CreateAsync(wearable);
            return created.WearableId;
        }

        /// <inheritdoc />
        public async UniTask<ApiWearable> GetWearableByIdAsync(string id)
        {
            var record = await _wearablesStorage.GetByIdAsync(id);
            return record.ToApiWearable();
        }

        /// <inheritdoc />
        public async UniTask<List<ApiWearable>> GetWearablesByIdsAsync(List<string> ids)
        {
            var tasks   = ids.Select(_wearablesStorage.GetByIdAsync);
            var records = await UniTask.WhenAll(tasks);
            return records.Select(r => r.ToApiWearable()).ToList();
        }

        /// <inheritdoc />
        public async UniTask<List<ApiWearable>> GetAllOwnedWearablesAsync()
        {
            var records = await _wearablesStorage.GetAllAsync();
            return records.Select(r => r.ToApiWearable()).ToList();
        }

        /// <inheritdoc />
        public async UniTask<List<string>> GetAllOwnedWearableIds()
        {
            return await _wearablesStorage.GetIdsAsync();
        }

        /// <inheritdoc />
        public UniTask<WearableThriftList> GetThriftableWearbles(string userId)
        {
            Debug.LogError("LocalWearableService current does not support GetThriftableWearables");
            return UniTask.FromResult<WearableThriftList>(null);
        }

        private string GetCategoryFromId(string id)
        {
            var splitName = id.Split('-');
            return splitName.Length != 0 ? splitName[0] : null;
        }

        /// <inheritdoc />
        public void ClearCache()
        {}
    }
}
