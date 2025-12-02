using System.Net.Mail;
using System.Threading.Tasks;
using Genies.Login;
using Genies.Login.AuthMessages;
using Genies.Login.EmailOtp;

namespace Genies.NativeAPI
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class GeniesNativeApiEmailOtpLoginFlowController : IEmailOtpLoginFlowController
#else
    public sealed class GeniesNativeApiEmailOtpLoginFlowController : IEmailOtpLoginFlowController
#endif
    {
        private bool _didStart;
        private bool _didVerify;
        private GeniesNativeAPIAuth _auth;

        internal GeniesNativeApiEmailOtpLoginFlowController(GeniesNativeAPIAuth auth) => _auth = auth;

        public async Task<GeniesAuthStartEmailOtpResponse> SubmitEmailAsync(string email)
        {
            email = email.Trim();
            var userId = await _auth.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await _auth.LogOutAsync();
            }

            _didStart = false;
            _didVerify = false;

            if (!GeniesLoginUtils.IsValidEmail(email))
            {
                return new GeniesAuthStartEmailOtpResponse
                {
                    Status = "error",
                    Message = "Invalid email"
                };
            }

            var res = await _auth.StartMagicAuthAsync(email);
            _didStart = res.IsSuccessful;
            return res;
        }

        public async Task<GeniesAuthVerifyMagicLinkResponse> SubmitCodeAsync(string email, string code)
        {
            if (!_didStart)
            {
                return new GeniesAuthVerifyMagicLinkResponse { Status = "error", Message = $"Must call {nameof(SubmitEmailAsync)} first." };
            }

            if (_didVerify)
            {
                return new GeniesAuthVerifyMagicLinkResponse() { Status = "error", Message = "Already verified. Restart with SubmitEmailAsync." };
            }

            var res = await _auth.VerifyMagicAuthAsync(email, code);
            _didVerify = res.IsSuccessful;

            // On success, hydrate + start timers just like OTP
            await _auth.GetUserAttributesAsync();
            if (res.IsSuccessful)
            {
                _auth.InvokeOnUserLoggedIn();
                _auth.StartTokenExpiryTimer();
            }

            return res;
        }

        public Task<GeniesAuthResendMagicLinkResponse> ResendCodeAsync(string email)
        {
            if (!_didStart)
            {
                return Task.FromResult(new GeniesAuthResendMagicLinkResponse { Status = "error", Message = $"Must call {nameof(SubmitEmailAsync)} first." });
            }

            if (_didVerify)
            {
                return Task.FromResult(new GeniesAuthResendMagicLinkResponse { Status = "error", Message = "Already verified. Restart with SubmitEmailAsync." });
            }

            return _auth.ResendMagicAuthAsync(email);
        }

        public async Task<GeniesAuthUpgradeV1Response> UpgradeUserV1Async(string email)
        {
            if (!_auth.IsInitialized)
            {
                return new GeniesAuthUpgradeV1Response
                {
                    Status = "error", Message = "Not initialized", ErrorMessage = "Not initialized",
                    StatusCodeString = "ClientNotInitialized",
                    ResponseStatusCode = GeniesAuthUpgradeV1Response.StatusCode.ClientNotInitialized
                };
            }

            return await _auth.UpgradeUserV1Async(email);
        }

        public async Task<GeniesAuthSignUpV2Response> SignUp(string email)
        {
            if (!_auth.IsInitialized)
            {
                return new GeniesAuthSignUpV2Response { Status = "error", Message = "Not initialized", ErrorMessage = "Not initialized", StatusCodeString = "ClientNotInitialized", ResponseStatusCode = GeniesAuthSignUpV2Response.StatusCode.ClientNotInitialized};
            }
            return await _auth.SignUpV2Async(email);
        }

        public void Dispose()
        {
            // What to do?
        }
    }
}
