using System.Threading.Tasks;
using Genies.Login.Anonymous;
using Genies.Login.AuthMessages;

namespace Genies.NativeAPI
{
#if GENIES_SDK && !GENIES_INTERNAL
    internal sealed class GeniesNativeAPIAnonymousFlowController : IAnonymousLoginFlowController
#else
    public sealed class GeniesNativeAPIAnonymousFlowController : IAnonymousLoginFlowController
#endif
    {
        private GeniesNativeAPIAuth _auth;

        internal GeniesNativeAPIAnonymousFlowController(GeniesNativeAPIAuth auth) => _auth = auth;

        public async Task<GeniesAuthAnonymousResponse> SignInAnonymouslyAsync(string applicationId)
        {
            if (string.IsNullOrWhiteSpace(applicationId))
            {
                return new GeniesAuthAnonymousResponse { Status = "error", Message = "Missing applicationId" };
            }

            var res = await _auth.AnonymousSignUpAsync(applicationId);

            // Anonymous session is still a session—hydrate + schedule
            if (res.IsSuccessful)
            {
                await _auth.GetUserAttributesAsync();
                _auth.StartTokenExpiryTimer();
                _auth.InvokeOnUserLoggedIn();
            }

            return res;
        }

        public async Task<GeniesAuthAnonymousResponse> UpgradeAsync(string email, string birthday = "", string firstName = "", string lastName = "")
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return new GeniesAuthAnonymousResponse { Status = "error", Message = "Missing email" };
            }

            return await _auth.AnonymousUpgradeAsync(email, birthday, firstName, lastName);
        }

        public void Dispose() { _auth = null; }
    }
}
