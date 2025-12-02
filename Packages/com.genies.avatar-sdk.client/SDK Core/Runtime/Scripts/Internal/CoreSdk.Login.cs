using System;
using Cysharp.Threading.Tasks;
using Genies.Login;
using Genies.Login.Native;

namespace Genies.Sdk
{
    internal partial class CoreSdk
    {
        public class Login
        {
            private CoreSdk Parent { get; }

            private Login() { }

            internal Login(CoreSdk parent)
            {
                Parent = parent;
            }
            /// <summary>
            /// Invoked when a user successfully logs in.
            /// </summary>
            public event Action UserLoggedIn
            {
                add => GeniesLoginSdk.UserLoggedIn += value;
                remove => GeniesLoginSdk.UserLoggedIn -= value;
            }

            /// <summary>
            /// Invoked when a user logs out.
            /// </summary>
            public event Action UserLoggedOut
            {
                add => GeniesLoginSdk.UserLoggedOut += value;
                remove => GeniesLoginSdk.UserLoggedOut -= value;
            }

            /// <summary>
            /// Gets whether a user is currently logged in.
            /// </summary>
            public bool IsLoggedIn => GeniesLoginSdk.IsUserSignedIn();

            /// <summary>
            /// The URL for the Genies Hub authentication and sign-up page for new users to create an account.
            /// </summary>
            public string UrlGeniesHubSignUp => GeniesLoginSdk.UrlGeniesHubSignUp;

            /// <summary>
            /// Gets the username of the currently logged in user.
            /// </summary>
            /// <returns>The username, or null if not logged in.</returns>
            public async UniTask<string> GetUserNameAsync()
            {
                if (await Parent.InitializeAsync() is false)
                {
                    return null;
                }

                return await GeniesLoginSdk.GetUsernameAsync();
            }

            /// <summary>
            /// Gets the unique identifier of the currently logged in user.
            /// </summary>
            /// <returns>The user ID, or null if not logged in.</returns>
            public async UniTask<string> GetUserIdAsync()
            {
                if (await Parent.InitializeAsync() is false)
                {
                    return null;
                }

                return await GeniesLoginSdk.GetUserIdAsync();
            }

            /// <summary>
            /// Gets the complete profile information of the currently logged in user.
            /// </summary>
            /// <returns>A GeniesUser object containing the user's profile information, or null if not logged in.</returns>
            public async UniTask<IGeniesUser> GetLoggedInUserAsync()
            {
                if (await Parent.InitializeAsync() is false)
                {
                    return null;
                }

                if (!IsLoggedIn)
                {
                    return null;
                }

                var userId = await GeniesLoginSdk.GetUserIdAsync();
                var username = await GeniesLoginSdk.GetUsernameAsync();

                return await GetUserAsync(userId, username);
            }

            /// <summary>
            /// Gets the complete profile information for a specific user.
            /// </summary>
            /// <param name="userId">The unique identifier of the user.</param>
            /// <param name="username">The username of the user.</param>
            /// <returns>A GeniesUser object containing the user's profile information, or null if retrieval fails.</returns>
            public async UniTask<IGeniesUser> GetUserAsync(string userId, string username)
            {
                if (await Parent.InitializeAsync() is false)
                {
                    return null;
                }

                var userAttributes = await GeniesLoginSdk.GetUserAttributesAsync(userId, username);

                return new GeniesUser(userId, userAttributes);
            }

            /// <summary>
            /// Attempts to automatically log in using stored credentials.
            /// </summary>
            /// <returns>A tuple indicating success and the username if successful.</returns>
            public async UniTask<(bool succeeded, string username)> TryInstantLoginAsync()
            {
                if (await Parent.InitializeAsync() is false)
                {
                    return (false, null);
                }

                await GeniesLoginSdk.TryInstantLoginAsync();

                if (IsLoggedIn)
                {
                    var username = await GeniesLoginSdk.GetUsernameAsync();
                    return (true, username);
                }

                return (false, null);
            }

            /// <summary>
            /// Logs out the current user.
            /// </summary>
            public async UniTask LogOutAsync()
            {
                if (await Parent.InitializeAsync() is false)
                {
                    return;
                }

                if (IsLoggedIn)
                {
                    await GeniesLoginSdk.LogOutAsync();
                }
            }
        }
    }
}
