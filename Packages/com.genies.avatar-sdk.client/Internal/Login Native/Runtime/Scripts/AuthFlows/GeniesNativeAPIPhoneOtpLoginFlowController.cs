using System.Threading.Tasks;
using Genies.Login.AuthMessages;
using Genies.Login.EmailOtp;
using Genies.Login.Otp;

namespace Genies.NativeAPI
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class GeniesNativeAPIPhoneOtpLoginFlowController : IHybridOtpLoginFlowController
#else
    public sealed class GeniesNativeAPIPhoneOtpLoginFlowController : IHybridOtpLoginFlowController
#endif
    {
        private bool _didStart;
        private bool _didVerify;
        private readonly GeniesNativeAPIAuth _auth;

        internal GeniesNativeAPIPhoneOtpLoginFlowController(GeniesNativeAPIAuth auth) => _auth = auth;

        private static bool IsLikelyPhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                return false;
            }

            phone = phone.Trim();

            // Simple heuristic: must contain at least one digit, and only digits/space/+/-/() allowed
            bool hasDigit = false;
            foreach (var ch in phone)
            {
                if (char.IsDigit(ch))
                {
                    hasDigit = true;
                }
                else if (ch != ' ' && ch != '+' && ch != '-' && ch != '(' && ch != ')')
                {
                    return false;
                }
            }

            return hasDigit;
        }

        public async Task<GeniesAuthStartHybridOtpResponse> SubmitCredentialAsync(string phone)
        {
            phone = phone?.Trim() ?? string.Empty;
            
            var userId = await _auth.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await _auth.LogOutAsync();
            }

            _didStart = false;
            _didVerify = false;

            if (!IsLikelyPhone(phone))
            {
                return new GeniesAuthStartHybridOtpResponse
                {
                    Status = "error",
                    Message = "Invalid phone number"
                };
            }

            var res = await _auth.StartMagicAuthWithPhoneAsync(phone);
            _didStart = res.IsSuccessful;
            return res;
        }

        public async Task<GeniesAuthVerifyMagicLinkResponse> SubmitCodeAsync(string phone, string code)
        {
            if (!_didStart)
            {
                return new GeniesAuthVerifyMagicLinkResponse
                {
                    Status = "error",
                    Message = $"Must call {nameof(SubmitCredentialAsync)} first."
                };
            }

            if (_didVerify)
            {
                return new GeniesAuthVerifyMagicLinkResponse
                {
                    Status = "error",
                    Message = "Already verified. Restart with SubmitEmailAsync."
                };
            }

            var res = await _auth.VerifyMagicAuthWithPhoneAsync(phone?.Trim() ?? string.Empty, code);
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

        public Task<GeniesAuthResendMagicLinkResponse> ResendCodeAsync(string phone)
        {
            if (!_didStart)
            {
                return Task.FromResult(new GeniesAuthResendMagicLinkResponse
                {
                    Status = "error",
                    Message = $"Must call {nameof(SubmitCredentialAsync)} first."
                });
            }

            if (_didVerify)
            {
                return Task.FromResult(new GeniesAuthResendMagicLinkResponse
                {
                    Status = "error",
                    Message = "Already verified. Restart with SubmitEmailAsync."
                });
            }

            return _auth.ResendMagicAuthWithPhoneAsync(phone?.Trim() ?? string.Empty);
        }

        public async Task<GeniesAuthUpgradeV1Response> UpgradeUserV1Async(string email)
        {
            // Upgrade remains email-based (v1 Cognito upgrade semantics)
            if (!_auth.IsInitialized)
            {
                return new GeniesAuthUpgradeV1Response
                {
                    Status = "error",
                    Message = "Not initialized",
                    ErrorMessage = "Not initialized",
                    StatusCodeString = "ClientNotInitialized",
                    ResponseStatusCode = GeniesAuthUpgradeV1Response.StatusCode.ClientNotInitialized
                };
            }

            return await _auth.UpgradeUserV1Async(email);
        }

        public async Task<GeniesAuthSignUpV2Response> SignUp(string phone)
        {
            if (!_auth.IsInitialized)
            {
                return new GeniesAuthSignUpV2Response
                {
                    Status = "error",
                    Message = "Not initialized",
                    ErrorMessage = "Not initialized",
                    StatusCodeString = "ClientNotInitialized",
                    ResponseStatusCode = GeniesAuthSignUpV2Response.StatusCode.ClientNotInitialized
                };
            }

            return await _auth.SignUpV2WithPhoneAsync(phone?.Trim() ?? string.Empty);
        }

        public void Dispose()
        {
            // nothing to dispose for now
        }
    }
}
