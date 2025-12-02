using System.Threading.Tasks;
using Genies.Login.AuthMessages;
using Genies.Login.Otp;

namespace Genies.NativeAPI
{
/// <summary>
    /// Native OTP flow controller, returned when we start the flow and user will need to explicitly
    /// call the login methods.
    /// </summary>
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class GeniesNativeAPIOTPFlowController : IOtpLoginFlowController
#else
    public sealed class GeniesNativeAPIOTPFlowController : IOtpLoginFlowController
#endif
    {
        private string _phoneNumber;
        private bool _didSubmitPhoneNumber;
        private bool _didSubmitCode;

        private GeniesNativeAPIAuth _auth;

        internal GeniesNativeAPIOTPFlowController(GeniesNativeAPIAuth auth)
        {
            _auth = auth;
        }

        // --- Phone Number Handling ---
        public async Task<GeniesAuthInitiateOtpSignInResponse> SubmitPhoneNumberAsync(string phoneNumber)
        {
            var userId = await _auth.GetUserId();

            //Logout if we already have a user signed in.
            if (!string.IsNullOrEmpty(userId))
            {
                await _auth.LogOutAsync();
            }

            _didSubmitPhoneNumber = false;
            _didSubmitCode = false;

            if (string.IsNullOrEmpty(phoneNumber))
            {
                return new GeniesAuthInitiateOtpSignInResponse()
                {
                    Status = "error",
                    ResponseStatusCode = GeniesAuthInitiateOtpSignInResponse.StatusCode.None,
                    Message = "Invalid phone number provided",
                };
            }

            var result = await _auth.BeginOtpLoginAsync(phoneNumber);
            _didSubmitPhoneNumber = result.IsSuccessful;
            _phoneNumber = result.IsSuccessful ? phoneNumber : string.Empty;

            return result;
        }

        // --- OTP Handling ---
        public async Task<GeniesAuthSendOtpResponse> SubmitOtpCodeAsync(string code)
        {
            //Check if we already signed in first.
            if (!_didSubmitPhoneNumber)
            {
                return new GeniesAuthSendOtpResponse()
                {
                    Status = "error",
                    ResponseStatusCode = GeniesAuthSendOtpResponse.StatusCode.SendOtpError,
                    Message = $"User is not signed in yet. Make sure to call {nameof(SubmitPhoneNumberAsync)}.",
                };
            }

            if (_didSubmitCode)
            {

                return new GeniesAuthSendOtpResponse()
                {
                    Status = "error",
                    ResponseStatusCode = GeniesAuthSendOtpResponse.StatusCode.SendOtpError,
                    Message = $"User already logged in. To restart login, call {nameof(SubmitPhoneNumberAsync)}",
                };
            }


            var result = await _auth.RespondToOtpAsync(code);
            _didSubmitCode = result.IsSuccessful;

            // Get the user attributes... they are cached in the plugin and can be accessed via getters
            await _auth.GetUserAttributesAsync();

            if (result.IsSuccessful)
            {
                // We did succeed in logging in, but this account needs to be upgraded.
                result.ResponseStatusCode = GeniesAuthSendOtpResponse.StatusCode.AccountUpgradeRequired;
                _auth.StartTokenExpiryTimer();
                _auth.InvokeOnUserLoggedIn();
            }

            return result;
        }

        public async Task<GeniesAuthOtpRefreshResponse> ResendOtpCodeAsync()
        {
            //Check if we already signed in first.
            if (!_didSubmitPhoneNumber)
            {
                return new GeniesAuthOtpRefreshResponse()
                {
                    Status = "error",
                    ResponseStatusCode = GeniesAuthOtpRefreshResponse.StatusCode.NoActiveSession,
                    Message = $"User is not signed in yet. Make sure to call {nameof(SubmitPhoneNumberAsync)}.",
                };
            }

            if (_didSubmitCode)
            {
                return new GeniesAuthOtpRefreshResponse()
                {

                    Status = "error",
                    ResponseStatusCode = GeniesAuthOtpRefreshResponse.StatusCode.None,
                    Message = $"User already logged in. To restart login, call {nameof(SubmitPhoneNumberAsync)}",
                };
            }

            var result = await _auth.ResendOTPAsync(_phoneNumber);

            return result;
        }

        public void Dispose()
        {
            // What do we dispose?
        }
    }
}
