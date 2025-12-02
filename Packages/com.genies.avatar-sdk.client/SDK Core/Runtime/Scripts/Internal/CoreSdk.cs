using Cysharp.Threading.Tasks;
using Genies.Avatars.Sdk;

namespace Genies.Sdk
{
    internal sealed partial class CoreSdk
    {
        private Avatar _avatar;
        private Login _login;
        private LoginOtp _loginOtp;
        private LoginPassword _loginPassword;

        /// <summary>
        /// Gets the Avatar API for loading and managing avatars.
        /// </summary>
        public Avatar AvatarApi => _avatar ??= new Avatar(this);

        /// <summary>
        /// Gets the Login API for authentication operations.
        /// </summary>
        public Login LoginApi => _login ??= new Login(this);

        /// <summary>
        /// Gets the LoginOtp API for email OTP authentication.
        /// </summary>
        public LoginOtp LoginOtpApi => _loginOtp ??= new LoginOtp(this);

        /// <summary>
        /// Gets the LoginPassword API for password authentication.
        /// </summary>
        public LoginPassword LoginPasswordApi => _loginPassword ??= new LoginPassword(this);

        /// <summary>
        /// Initializes the Genies Avatar SDK.
        /// Calling is optional as all operations will initialize the SDK if it is not already initialized.
        /// </summary>
        /// <returns>True if initialization succeeded, false otherwise.</returns>
        public async UniTask<bool> InitializeAsync() => await GeniesAvatarsSdk.InitializeAsync();
    }
}
