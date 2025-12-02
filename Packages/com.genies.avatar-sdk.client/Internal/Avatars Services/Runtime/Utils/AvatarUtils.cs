using Genies.Services.Model;

namespace Genies.Avatars.Services
{
    /// <summary>
    /// Utility class providing helper methods for avatar-related operations.
    /// Contains common conversion and utility functions used throughout the avatar services.
    /// </summary>
    public static class AvatarUtils
    {
        /// <summary>
        /// Converts a string representation of gender to the corresponding Avatar.GenderEnum value.
        /// </summary>
        /// <param name="gender">The gender string to convert (e.g., "male", "female").</param>
        /// <returns>The corresponding Avatar.GenderEnum value. Defaults to Female if the input is not recognized.</returns>
        public static Avatar.GenderEnum ConvertFromStringToAvatarGender(string gender)
        {
            if (gender == "female")
            {
                return Avatar.GenderEnum.Female;
            }
            else if (gender == "male")
            {
                return Avatar.GenderEnum.Male;
            }
            else
            {
                return Avatar.GenderEnum.Female;
            }
        }

    }
}
