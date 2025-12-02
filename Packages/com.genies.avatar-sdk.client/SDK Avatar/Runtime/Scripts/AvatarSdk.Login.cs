using System;
using Cysharp.Threading.Tasks;

namespace Genies.Sdk
{
    public sealed partial class AvatarSdk
    {
        /// <summary>
        /// Gets whether a user is currently logged in.
        /// </summary>
        public static bool IsLoggedIn => Instance.CoreSdk.LoginApi.IsLoggedIn;

        /// <summary>
        /// The URL for the Genies Hub authentication and sign-up page for new users to create an account.
        /// </summary>
        public static string UrlGeniesHubSignUp => Instance.CoreSdk.LoginApi.UrlGeniesHubSignUp;

        public static partial class Events
        {
            /// <summary>
            /// Invoked when a user successfully logs in.
            /// </summary>
            public static event Action UserLoggedIn
            {
                add => Instance.CoreSdk.LoginApi.UserLoggedIn += value;
                remove => Instance.CoreSdk.LoginApi.UserLoggedIn -= value;
            }

            /// <summary>
            /// Invoked when a user logs out.
            /// </summary>
            public static event Action UserLoggedOut
            {
                add => Instance.CoreSdk.LoginApi.UserLoggedOut += value;
                remove => Instance.CoreSdk.LoginApi.UserLoggedOut -= value;
            }
        }

        /// <summary>
        /// Gets the username of the currently logged in user.
        /// </summary>
        /// <returns>The username, or null if not logged in.</returns>
        public static async UniTask<string> GetUserNameAsync()
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.LoginApi.GetUserNameAsync();
        }

        /// <summary>
        /// Gets the unique identifier of the currently logged in user.
        /// </summary>
        /// <returns>The user ID, or null if not logged in.</returns>
        public static async UniTask<string> GetUserIdAsync()
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.LoginApi.GetUserIdAsync();
        }

        /// <summary>
        /// Gets the complete profile information of the currently logged in user.
        /// </summary>
        /// <returns>A GeniesUser object containing the user's profile information, or null if not logged in.</returns>
        public static async UniTask<IGeniesUser> GetLoggedInUserAsync()
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.LoginApi.GetLoggedInUserAsync();
        }

        /// <summary>
        /// Gets the complete profile information for a specific user.
        /// </summary>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="username">The username of the user.</param>
        /// <returns>A GeniesUser object containing the user's profile information, or null if retrieval fails.</returns>
        public static async UniTask<IGeniesUser> GetUserAsync(string userId, string username)
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.LoginApi.GetUserAsync(userId, username);
        }

        /// <summary>
        /// Attempts to automatically log in using stored credentials.
        /// </summary>
        /// <returns>A tuple indicating success and the username if successful.</returns>
        public static async UniTask<(bool isLoggedIn, string username)> TryInstantLoginAsync()
        {
            await Instance.InitializeInternalAsync();
            return await Instance.CoreSdk.LoginApi.TryInstantLoginAsync();
        }

        /// <summary>
        /// Logs out the current user.
        /// </summary>
        public static async UniTask LogOutAsync()
        {
            await Instance.InitializeInternalAsync();
            await Instance.CoreSdk.LoginApi.LogOutAsync();
        }
    }
}
