using System;
using System.Threading.Tasks;
using Genies.Login.AuthMessages;

namespace Genies.Login.Password
{
    public interface IPasswordLoginFlowController : IDisposable
    {
        Task<GeniesAuthSignInV2Response> SignInAsync(string email, string password);
        Task<GeniesAuthVerifyEmailV2Response> VerifyEmailAsync(string code);
        Task<GeniesAuthSignUpV2Response> SignUp(string email, string password);
    }
}