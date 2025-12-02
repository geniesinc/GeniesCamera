using System;
using System.Threading.Tasks;
using Genies.Login.AuthMessages;

namespace Genies.Login.EmailOtp
{
    public interface IEmailOtpLoginFlowController : IDisposable
    {
        Task<GeniesAuthStartEmailOtpResponse> SubmitEmailAsync(string email);
        Task<GeniesAuthVerifyMagicLinkResponse> SubmitCodeAsync(string email, string code);
        Task<GeniesAuthResendMagicLinkResponse> ResendCodeAsync(string email);
        Task<GeniesAuthUpgradeV1Response> UpgradeUserV1Async(string email);            
        Task<GeniesAuthSignUpV2Response> SignUp(string email);
    }
}