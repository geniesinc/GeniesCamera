using System.Net.Mail;
using System.Threading.Tasks;
using Genies.Login;
using Genies.Login.AuthMessages;
using Genies.Login.Otp;
using UnityEngine;

namespace Genies.NativeAPI
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class GeniesNativeAPIHybirdOtpLoginFlowController : IHybridOtpLoginFlowController
#else
    public sealed class GeniesNativeAPIHybirdOtpLoginFlowController : IHybridOtpLoginFlowController
#endif
    {
        private enum ContactMode
        {
            None,
            Email,
            Phone
        }

        private bool _didStart;
        private bool _didVerify;
        private ContactMode _mode = ContactMode.None;
        private readonly GeniesNativeAPIAuth _auth;

        internal GeniesNativeAPIHybirdOtpLoginFlowController(GeniesNativeAPIAuth auth) => _auth = auth;
        
        public async Task<GeniesAuthStartHybridOtpResponse> SubmitCredentialAsync(string identifier)
        {
            identifier = identifier?.Trim() ?? string.Empty;

            var userId = await _auth.GetUserId();
            if (!string.IsNullOrEmpty(userId))
            {
                await _auth.LogOutAsync();
            }

            _didStart = false;
            _didVerify = false;
            _mode = ContactMode.None;

            // Decide mode based on identifier
            if (GeniesLoginUtils.IsValidEmail(identifier))
            {
                _mode = ContactMode.Email;
            }
            else if (GeniesLoginUtils.IsLikelyPhone(identifier))
            {
                _mode = ContactMode.Phone;
            }
            else
            {
                return new GeniesAuthStartHybridOtpResponse
                {
                    Status = "error",
                    Message = "Invalid email or phone number"
                };
            }

            GeniesAuthStartHybridOtpResponse res;
            if (_mode == ContactMode.Email)
            {
                var response = await _auth.StartMagicAuthAsync(identifier);
                var item = JsonUtility.ToJson(response);
                res = JsonUtility.FromJson<GeniesAuthStartHybridOtpResponse>(item); 
            }
            else // Phone
            {
                res = await _auth.StartMagicAuthWithPhoneAsync(identifier);
            }

            _didStart = res.IsSuccessful;
            return res;
        }

        public async Task<GeniesAuthVerifyMagicLinkResponse> SubmitCodeAsync(string identifier, string code)
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

            identifier = identifier?.Trim() ?? string.Empty;

            // If somehow mode is still None (should not happen), infer again
            if (_mode == ContactMode.None)
            {
                if (GeniesLoginUtils.IsValidEmail(identifier))
                {
                    _mode = ContactMode.Email;
                }
                else if (GeniesLoginUtils.IsLikelyPhone(identifier))
                {
                    _mode = ContactMode.Phone;
                }
            }

            GeniesAuthVerifyMagicLinkResponse res;

            if (_mode == ContactMode.Email)
            {
                res = await _auth.VerifyMagicAuthAsync(identifier, code);
            }
            else if (_mode == ContactMode.Phone)
            {
                res = await _auth.VerifyMagicAuthWithPhoneAsync(identifier, code);
            }
            else
            {
                return new GeniesAuthVerifyMagicLinkResponse
                {
                    Status = "error",
                    Message = "Unable to determine contact mode (email or phone)."
                };
            }

            _didVerify = res.IsSuccessful;

            await _auth.GetUserAttributesAsync();
            if (res.IsSuccessful)
            {
                _auth.InvokeOnUserLoggedIn();
                _auth.StartTokenExpiryTimer();
            }

            return res;
        }

        public Task<GeniesAuthResendMagicLinkResponse> ResendCodeAsync(string identifier)
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

            identifier = identifier?.Trim() ?? string.Empty;

            if (_mode == ContactMode.Email)
            {
                return _auth.ResendMagicAuthAsync(identifier);
            }

            if (_mode == ContactMode.Phone)
            {
                return _auth.ResendMagicAuthWithPhoneAsync(identifier);
            }

            // Fallback: try to infer again
            if (GeniesLoginUtils.IsValidEmail(identifier))
            {
                _mode = ContactMode.Email;
                return _auth.ResendMagicAuthAsync(identifier);
            }

            if (GeniesLoginUtils.IsLikelyPhone(identifier))
            {
                _mode = ContactMode.Phone;
                return _auth.ResendMagicAuthWithPhoneAsync(identifier);
            }

            return Task.FromResult(new GeniesAuthResendMagicLinkResponse
            {
                Status = "error",
                Message = "Invalid email or phone number."
            });
        }

        public async Task<GeniesAuthUpgradeV1Response> UpgradeUserV1Async(string email)
        {
            // Upgrade remains email-based
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

        public async Task<GeniesAuthSignUpV2Response> SignUp(string identifier)
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

            identifier = identifier?.Trim() ?? string.Empty;

            if (GeniesLoginUtils.IsValidEmail(identifier))
            {
                return await _auth.SignUpV2Async(identifier);
            }

            if (GeniesLoginUtils.IsLikelyPhone(identifier))
            {
                return await _auth.SignUpV2WithPhoneAsync(identifier);
            }

            return new GeniesAuthSignUpV2Response
            {
                Status = "error",
                Message = "Invalid email or phone number",
                ErrorMessage = "Invalid email or phone number",
                StatusCodeString = "InvalidIdentifier",
                ResponseStatusCode = GeniesAuthSignUpV2Response.StatusCode.ApiException
            };
        }

        public void Dispose()
        {
            // nothing to dispose for now
        }
    }
}
