using System;
using System.Threading.Tasks;
using Genies.Login.AuthMessages;

namespace Genies.Login.Anonymous
{
    public interface IAnonymousLoginFlowController : IDisposable
    {
        Task<GeniesAuthAnonymousResponse>   SignInAnonymouslyAsync(string applicationId);
        Task<GeniesAuthAnonymousResponse>  UpgradeAsync(string email, string birthday = "", string firstName = "", string lastName = "");
    }
}