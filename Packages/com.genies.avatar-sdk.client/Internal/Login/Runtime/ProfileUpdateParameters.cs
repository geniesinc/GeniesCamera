using UnityEngine;

namespace Genies.Login
{
    /// <summary>
    /// Contains parameters for updating user profile information in the Genies platform.
    /// This struct encapsulates all the profile fields that can be modified through the login system.
    /// </summary>
    public struct ProfileUpdateParameters
    {
        /// <summary>
        /// Gets or sets the user's email address.
        /// </summary>
        public string Email { get; set; }
        
        /// <summary>
        /// Gets or sets the user's phone number.
        /// </summary>
        public string PhoneNumber { get; set; }
        
        /// <summary>
        /// Gets or sets the user's biography or description.
        /// </summary>
        public string Bio { get; set; }
        
        /// <summary>
        /// Gets or sets the user's birthday.
        /// </summary>
        public string Birthday { get; set; }
        
        /// <summary>
        /// Gets or sets the user's username.
        /// </summary>
        public string Username { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when the user account was created.
        /// </summary>
        public int CreatedAt { get; set; }
        
        /// <summary>
        /// Gets or sets the unique Genies identifier for the user.
        /// </summary>
        public string GeniesId { get; set; }
        
        /// <summary>
        /// Gets or sets the timestamp when the profile was last updated.
        /// </summary>
        public int UpdatedAt { get; set; }
        
        /// <summary>
        /// Gets or sets the URL of the user's profile image.
        /// </summary>
        public string ProfileImageUrl { get; set; }
        
        /// <summary>
        /// Gets or sets the user's first name.
        /// </summary>
        public string FirstName { get; set; }
        
        /// <summary>
        /// Gets or sets the user's last name.
        /// </summary>
        public string LastName { get; set; }
        
        /// <summary>
        /// Gets or sets the user's Dapper identifier for blockchain-related functionality.
        /// </summary>
        public string DapperId { get; set; }
        
        /// <summary>
        /// Gets or sets the user's Flow blockchain address.
        /// </summary>
        public string FlowAddress { get; set; }
        
        /// <summary>
        /// Gets or sets the user's legal first name.
        /// </summary>
        public string LegalFirstName { get; set; }
        
        /// <summary>
        /// Gets or sets the user's legal last name.
        /// </summary>
        public string LegalLastName { get; set; }
        
        /// <summary>
        /// Gets or sets the name of the user's virtual doll/avatar.
        /// </summary>
        public string DollName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileUpdateParameters"/> struct with the specified values.
        /// All parameters are optional and will use empty strings or default values if not provided.
        /// </summary>
        /// <param name="email">The user's email address.</param>
        /// <param name="phoneNumber">The user's phone number.</param>
        /// <param name="bio">The user's biography.</param>
        /// <param name="birthday">The user's birthday.</param>
        /// <param name="username">The user's username.</param>
        /// <param name="createdAt">The account creation timestamp.</param>
        /// <param name="geniesId">The unique Genies identifier.</param>
        /// <param name="updatedAt">The last update timestamp.</param>
        /// <param name="profileImageUrl">The profile image URL.</param>
        /// <param name="firstName">The user's first name.</param>
        /// <param name="lastName">The user's last name.</param>
        /// <param name="dapperId">The Dapper identifier.</param>
        /// <param name="flowAddress">The Flow blockchain address.</param>
        /// <param name="legalFirstName">The user's legal first name.</param>
        /// <param name="legalLastName">The user's legal last name.</param>
        /// <param name="dollName">The virtual doll/avatar name.</param>
        public ProfileUpdateParameters(
            string email = "",
            string phoneNumber = "",
            string bio = "",
            string birthday = "",
            string username = "",
            int createdAt = 0,
            string geniesId = "",
            int updatedAt = 0,
            string profileImageUrl = "",
            string firstName = "",
            string lastName = "",
            string dapperId = "",
            string flowAddress = "",
            string legalFirstName = "",
            string legalLastName = "",
            string dollName = "")
        {
            Email = email;
            PhoneNumber = phoneNumber;
            Bio = bio;
            Birthday = birthday;
            Username = username;
            CreatedAt = createdAt;
            GeniesId = geniesId;
            UpdatedAt = updatedAt;
            ProfileImageUrl = profileImageUrl;
            FirstName = firstName;
            LastName = lastName;
            DapperId = dapperId;
            FlowAddress = flowAddress;
            LegalFirstName = legalFirstName;
            LegalLastName = legalLastName;
            DollName = dollName;
        }
    }
}
