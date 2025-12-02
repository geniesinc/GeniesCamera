using System;
using System.Collections.Generic;
using Genies.SDKServices.Model;
using Newtonsoft.Json;
using UnityEngine;

namespace Genies.Scripts.Gears
{
    /// <summary>
    /// Data model representing a gear item with all its properties and metadata.
    /// This serializable class contains comprehensive information about gear items including versioning,
    /// status, creation details, and rendering information.
    /// </summary>
    [Serializable]
    public class GearData
    {
        [JsonProperty("id")]
        [SerializeField] private string _id;
        /// <summary>
        /// Gets or sets the unique identifier of the gear item.
        /// </summary>
        public string Id
        {
            get => _id;
            set => _id = value;
        }

        [JsonProperty("gearVersionId")]
        [SerializeField] private string _gearVersionId;
        /// <summary>
        /// Gets or sets the version identifier of the gear item, used for versioning and updates.
        /// </summary>
        public string GearVersionId
        {
            get => _gearVersionId;
            set => _gearVersionId = value;
        }

        [JsonProperty("iconUrls")]
        [SerializeField] private List<string> _iconUrls;
        /// <summary>
        /// Gets or sets the list of URLs for icon images representing this gear item.
        /// </summary>
        public List<string> IconUrls
        {
            get => _iconUrls;
            set => _iconUrls = value;
        }

        [JsonProperty("status")]
        [SerializeField] private string _status;
        /// <summary>
        /// Gets or sets the current status of the gear item (e.g., active, inactive, pending).
        /// </summary>
        public string Status
        {
            get => _status;
            set => _status = value;
        }

        [JsonProperty("sdkVersion")]
        [SerializeField] private float? _sdkVersion;
        /// <summary>
        /// Gets or sets the SDK version number that this gear item is compatible with.
        /// </summary>
        public float? SdkVersion
        {
            get => _sdkVersion;
            set => _sdkVersion = value;
        }

        [JsonProperty("name")]
        [SerializeField] private string _name;
        /// <summary>
        /// Gets or sets the display name of the gear item.
        /// </summary>
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        [JsonProperty("description")]
        [SerializeField] private string _description;
        /// <summary>
        /// Gets or sets the description of the gear item providing details about its purpose and features.
        /// </summary>
        public string Description
        {
            get => _description;
            set => _description = value;
        }

        [JsonProperty("cmsId")]
        [SerializeField] private string _cmsId;
        /// <summary>
        /// Gets or sets the Content Management System identifier for this gear item.
        /// </summary>
        public string CmsId
        {
            get => _cmsId;
            set => _cmsId = value;
        }

        [JsonProperty("reviewerId")]
        [SerializeField] private string _reviewerId;
        /// <summary>
        /// Gets or sets the identifier of the reviewer who approved or reviewed this gear item.
        /// </summary>
        public string ReviewerId
        {
            get => _reviewerId;
            set => _reviewerId = value;
        }

        [JsonProperty("reviewerComment")]
        [SerializeField] private string _reviewerComment;
        /// <summary>
        /// Gets or sets the comment or notes left by the reviewer regarding this gear item.
        /// </summary>
        public string ReviewerComment
        {
            get => _reviewerComment;
            set => _reviewerComment = value;
        }

        [JsonProperty("s3Key")]
        [SerializeField] private string _s3Key;
        /// <summary>
        /// Gets or sets the Amazon S3 storage key where the gear assets are stored.
        /// </summary>
        public string S3Key
        {
            get => _s3Key;
            set => _s3Key = value;
        }

        [JsonProperty("fullSdkVersion")]
        [SerializeField] private string _fullSdkVersion;
        /// <summary>
        /// Gets or sets the full SDK version string that this gear item is compatible with.
        /// </summary>
        public string FullSdkVersion
        {
            get => _fullSdkVersion;
            set => _fullSdkVersion = value;
        }

        [JsonProperty("createdAt")]
        [SerializeField] private float? _createdAt;
        /// <summary>
        /// Gets or sets the timestamp when this gear item was created.
        /// </summary>
        public float? CreatedAt
        {
            get => _createdAt;
            set => _createdAt = value;
        }

        [JsonProperty("lastModifiedAt")]
        [SerializeField] private float? _lastModifiedAt;
        /// <summary>
        /// Gets or sets the timestamp when this gear item was last modified.
        /// </summary>
        public float? LastModifiedAt
        {
            get => _lastModifiedAt;
            set => _lastModifiedAt = value;
        }

        [JsonProperty("contentVersion")]
        [SerializeField] private float? _contentVersion;
        /// <summary>
        /// Gets or sets the version number of the content for this gear item.
        /// </summary>
        public float? ContentVersion
        {
            get => _contentVersion;
            set => _contentVersion = value;
        }

        [JsonProperty("category")]
        [SerializeField] private string _category;
        /// <summary>
        /// Gets or sets the category classification of this gear item (e.g., clothing, accessory, etc.).
        /// </summary>
        public string Category
        {
            get => _category;
            set => _category = value;
        }

        [JsonProperty("buildStatus")]
        [SerializeField] private string _buildStatus;
        /// <summary>
        /// Gets or sets the build status of the gear item indicating the current build state.
        /// </summary>
        public string BuildStatus
        {
            get => _buildStatus;
            set => _buildStatus = value;
        }

        [JsonProperty("errorMessage")]
        [SerializeField] private string _errorMessage;
        /// <summary>
        /// Gets or sets any error message associated with this gear item if build or processing failed.
        /// </summary>
        public string ErrorMessage
        {
            get => _errorMessage;
            set => _errorMessage = value;
        }

        [JsonProperty("renderingUrls")]
        [SerializeField] private List<RenderingUrl> _renderingUrls;
        /// <summary>
        /// Gets or sets the list of rendering URLs for visual representations of this gear item.
        /// </summary>
        public List<RenderingUrl> RenderingUrls
        {
            get => _renderingUrls;
            set => _renderingUrls = value;
        }

        [JsonProperty("nonConformed")]
        [SerializeField] private bool? _nonConformed;
        /// <summary>
        /// Gets or sets a value indicating whether this gear item does not conform to standard specifications.
        /// </summary>
        public bool? NonConformed
        {
            get => _nonConformed;
            set => _nonConformed = value;
        }

        [JsonProperty("contentError")]
        [SerializeField] private string _contentError;
        /// <summary>
        /// Gets or sets any content-related error message for this gear item.
        /// </summary>
        public string ContentError
        {
            get => _contentError;
            set => _contentError = value;
        }

        [JsonProperty("contentBuildStatus")]
        [SerializeField] private string _contentBuildStatus;
        /// <summary>
        /// Gets or sets the build status specifically for the content of this gear item.
        /// </summary>
        public string ContentBuildStatus
        {
            get => _contentBuildStatus;
            set => _contentBuildStatus = value;
        }
    }
}
