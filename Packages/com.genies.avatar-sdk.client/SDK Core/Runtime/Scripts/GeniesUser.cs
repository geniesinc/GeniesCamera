using System.Collections.Generic;

namespace Genies.Sdk
{
    /// <summary>
    /// Interface for accessing Genies user profile information.
    /// </summary>
    public interface IGeniesUser
    {
        /// <summary>
        /// Unique identifier of the user.
        /// </summary>
        string UserId { get; }

        /// <summary>
        /// User's biography or description.
        /// </summary>
        string Bio { get; }

        /// <summary>
        /// User's birthday.
        /// </summary>
        string Birthday { get; }

        /// <summary>
        /// User's username.
        /// </summary>
        string Username { get; }

        /// <summary>
        /// Timestamp when the user account was created.
        /// </summary>
        int CreatedAt { get; }

        /// <summary>
        /// Unique Avatar identifier for the user.
        /// </summary>
        string AvatarId { get; }

        /// <summary>
        /// Timestamp when the profile was last updated.
        /// </summary>
        int UpdatedAt { get; }

        /// <summary>
        /// URL of the user's profile image.
        /// </summary>
        string ProfileImageUrl { get; }
    }

    /// <summary>
    /// Represents the currently logged-in Genies user with read-only profile information.
    /// </summary>
    public class GeniesUser : IGeniesUser
    {
        /// <summary>
        /// Unique identifier of the user.
        /// </summary>
        public string UserId { get; }

        /// <summary>
        /// User's biography or description.
        /// </summary>
        public string Bio { get; }

        /// <summary>
        /// User's birthday.
        /// </summary>
        public string Birthday { get; }

        /// <summary>
        /// User's username.
        /// </summary>
        public string Username { get; }

        /// <summary>
        /// Timestamp when the user account was created.
        /// </summary>
        public int CreatedAt { get; }

        /// <summary>
        /// Unique Avatar identifier for the user.
        /// </summary>
        public string AvatarId { get; }

        /// <summary>
        /// Timestamp when the profile was last updated.
        /// </summary>
        public int UpdatedAt { get; }

        /// <summary>
        /// URL of the user's profile image.
        /// </summary>
        public string ProfileImageUrl { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeniesUser"/> class from user attributes.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="userAttributes">Dictionary containing user profile attributes from the API.</param>
        internal GeniesUser(string userId, Dictionary<string, string> userAttributes)
        {
            // TODO: Map dictionary keys to properties once actual key-value pairs are known
            UserId = userId;
            Bio = string.Empty;
            Birthday = string.Empty;
            Username = string.Empty;
            CreatedAt = 0;
            AvatarId = string.Empty;
            UpdatedAt = 0;
            ProfileImageUrl = string.Empty;
        }
    }
}
