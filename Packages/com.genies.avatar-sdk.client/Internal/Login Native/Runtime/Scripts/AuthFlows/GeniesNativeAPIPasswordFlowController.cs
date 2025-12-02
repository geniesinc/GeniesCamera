using System.Threading.Tasks;
using Genies.Login.AuthMessages;
using Genies.Login.Password;

namespace Genies.NativeAPI
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class GeniesNativeAPIPasswordFlowController : IPasswordLoginFlowController
#else
    public sealed class GeniesNativeAPIPasswordFlowController : IPasswordLoginFlowController
#endif
    {
        private GeniesNativeAPIAuth _auth;

        internal GeniesNativeAPIPasswordFlowController(GeniesNativeAPIAuth auth) => _auth = auth;

        public async Task<GeniesAuthSignInV2Response> SignInAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return new GeniesAuthSignInV2Response { Status = "error", Message = "Missing email or password" };
            }

            var res = await _auth.SignInV2Async(email, password);

            // On success, hydrate + start timers
            if (res.IsSuccessful)
            {
                await _auth.GetUserAttributesAsync();
                _auth.StartTokenExpiryTimer();
                _auth.InvokeOnUserLoggedIn();
            }

            return res;
        }

        public async Task<GeniesAuthVerifyEmailV2Response> VerifyEmailAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return new GeniesAuthVerifyEmailV2Response { Status = "error", Message = "Missing code" };
            }

            var verification = await _auth.VerifyEmailV2Async(code);
            if (verification.IsSuccessful)
            {
                await _auth.GetUserAttributesAsync();
                _auth.StartTokenExpiryTimer();
                _auth.InvokeOnUserLoggedIn();
            }

            return verification;
        }

        public async Task<GeniesAuthSignUpV2Response> SignUp(string email, string password)
        {
            return await _auth.SignUpV2Async(email, password);
        }

        public void Dispose() { _auth = null; }
    }
}
