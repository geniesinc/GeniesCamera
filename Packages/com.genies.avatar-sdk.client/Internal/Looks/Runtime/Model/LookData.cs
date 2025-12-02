using System;
using Genies.Services.Model;
using Newtonsoft.Json;

namespace Genies.Looks.Models
{
    /// <summary>
    /// Client representation of look data model. Split from the API's <see cref="Look"/> since we currently
    /// don't support the path for thumbnails or video urls when creating. This also allows us to mock in Unity
    /// using serialized fields.
    /// This struct contains all the necessary data to define and manage an avatar look including appearance, animations, and media.
    /// </summary>
    [Serializable]
    public struct LookData
    {
        /// <summary>
        /// Gets or sets the unique identifier for the look.
        /// </summary>
        [JsonProperty("Id")] public string Id { get; set; }

        /// <summary>
        /// Gets or sets the user ID who owns this look.
        /// </summary>
        [JsonProperty("UserId")] public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the look was created.
        /// </summary>
        [JsonProperty("CreatedAt")] public decimal? CreatedAt { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the look was last modified.
        /// </summary>
        [JsonProperty("LastModified")] public decimal? LastModified { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the look was exported for final use.
        /// </summary>
        [JsonProperty("ExportedAt")] public decimal? ExportedAt { get; set; }

        /// <summary>
        /// Gets or sets the avatar definition JSON string that defines the avatar's appearance.
        /// This contains all the visual configuration data for the avatar.
        /// </summary>
        [JsonProperty("AvatarDefinition")] public string AvatarDefinition { get; set; }

        /// <summary>
        /// Gets or sets the animation ID associated with this look for animation playback.
        /// </summary>
        [JsonProperty("AnimationId")] public string AnimationId { get; set; }

        /// <summary>
        /// Gets or sets the scene ID where this look should be displayed or was captured.
        /// </summary>
        [JsonProperty("SceneId")] public string SceneId { get; set; }

        /// <summary>
        /// Gets or sets the doll definition that may be associated with this look.
        /// </summary>
        [JsonProperty("DollDefinition")] public string DollDefinition { get; set; }

        /// <summary>
        /// Gets or sets the URL to the thumbnail image representing this look.
        /// </summary>
        [JsonProperty("ThumbnailUrl")] public string ThumbnailUrl { get; set; }

        /// <summary>
        /// Gets or sets the URL to the video clip showcasing this look.
        /// </summary>
        [JsonProperty("VideoUrl")] public string VideoUrl { get; set; }

        /// <summary>
        /// Gets or sets the URL to the avatar asset associated with this look.
        /// </summary>
        [JsonProperty("AvatarUrl")] public string AvatarUrl { get; set; }

        /// <summary>
        /// Gets or sets the current status of the look (e.g., Draft, Published, Processing).
        /// </summary>
        [JsonProperty("Status")] public Look.StatusEnum Status { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LookData"/> struct with complete look information.
        /// This constructor is used by JSON deserialization to create look instances from API responses.
        /// </summary>
        /// <param name="id">The unique identifier for the look.</param>
        /// <param name="userId">The user ID who owns this look.</param>
        /// <param name="createdAt">The timestamp when the look was created.</param>
        /// <param name="lastModified">The timestamp when the look was last modified.</param>
        /// <param name="exportedAt">The timestamp when the look was exported.</param>
        /// <param name="avatarDefinition">The avatar definition JSON string.</param>
        /// <param name="animationId">The animation ID associated with this look.</param>
        /// <param name="sceneId">The scene ID where this look should be displayed.</param>
        /// <param name="dollDefinition">The doll definition associated with this look.</param>
        /// <param name="thumbnailUrl">The URL to the thumbnail image.</param>
        /// <param name="videoUrl">The URL to the video clip.</param>
        /// <param name="avatarUrl">The URL to the avatar asset.</param>
        /// <param name="status">The current status of the look.</param>
        [JsonConstructor]
        public LookData(
            string id,
            string userId,
            decimal? createdAt,
            decimal? lastModified,
            decimal? exportedAt,
            string avatarDefinition,
            string animationId,
            string sceneId,
            string dollDefinition,
            string thumbnailUrl,
            string videoUrl,
            string avatarUrl,
            Look.StatusEnum status)
        {
            Id = id;
            UserId = userId;
            CreatedAt = createdAt;
            LastModified = lastModified;
            ExportedAt = exportedAt;
            AvatarDefinition = avatarDefinition;
            AnimationId = animationId;
            SceneId = sceneId;
            DollDefinition = dollDefinition;
            ThumbnailUrl = thumbnailUrl;
            VideoUrl = videoUrl;
            AvatarUrl = avatarUrl;
            Status = status;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LookData"/> struct with only an avatar definition.
        /// This simplified constructor is useful for creating new looks with just the essential avatar appearance data.
        /// All other properties are initialized to null or default values, and status is set to Draft.
        /// </summary>
        /// <param name="avatarDefinition">The avatar definition JSON string that defines the avatar's appearance.</param>
        public LookData(string avatarDefinition)
        {
            AvatarDefinition = avatarDefinition;
            Id = null;
            UserId = null;
            CreatedAt = null;
            LastModified = null;
            ExportedAt = null;
            AnimationId = null;
            SceneId = null;
            DollDefinition = null;
            ThumbnailUrl = null;
            VideoUrl = null;
            AvatarUrl = null;
            Status = Look.StatusEnum.Draft;
        }

        /// <summary>
        /// Explicitly converts a server <see cref="Look"/> object to a client <see cref="LookData"/> struct.
        /// This operator facilitates the transformation of API response objects to client-side data structures.
        /// </summary>
        /// <param name="serverLook">The server Look object to convert from.</param>
        /// <returns>A new LookData instance populated with data from the server Look object.</returns>
        public static explicit operator LookData(Look serverLook)
        {
            return new LookData(
                                serverLook.LookId,
                                serverLook.UserId,
                                serverLook.Created,
                                serverLook.LastModified,
                                serverLook.ExportedAt,
                                serverLook.AvatarDefinition,
                                serverLook.AnimationId, serverLook.SceneId, serverLook.DollDefinition, serverLook.Thumbnail, serverLook.VideoClip, avatarUrl: null, status: serverLook.Status ?? Look.StatusEnum.Draft
                               );
        }
    }
}
