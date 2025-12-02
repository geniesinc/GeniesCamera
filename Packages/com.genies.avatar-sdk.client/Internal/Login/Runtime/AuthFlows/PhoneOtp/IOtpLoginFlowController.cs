using System;
using System.Threading.Tasks;
using Genies.Login.AuthMessages;

namespace Genies.Login.Otp
{
    /// <summary>
    /// Defines the contract for managing OTP (One-Time Password) login flow operations.
    /// This interface provides methods for phone number verification and OTP code submission.
    /// </summary>
    public interface IOtpLoginFlowController : IDisposable
    {
        /// <summary>
        /// Submits a phone number to initiate the OTP login process.
        /// </summary>
        /// <param name="phoneNumber">The phone number to send the OTP code to.</param>
        /// <returns>A task that completes with the OTP initiation response.</returns>
        Task<GeniesAuthInitiateOtpSignInResponse> SubmitPhoneNumberAsync(string phoneNumber);
        
        /// <summary>
        /// Submits the OTP code received via SMS to complete the authentication process.
        /// </summary>
        /// <param name="code">The OTP code received by the user.</param>
        /// <returns>A task that completes with the OTP authentication response.</returns>
        Task<GeniesAuthSendOtpResponse> SubmitOtpCodeAsync(string code);
        
        /// <summary>
        /// Requests a new OTP code to be sent to the previously submitted phone number.
        /// </summary>
        /// <returns>A task that completes with the OTP refresh response.</returns>
        Task<GeniesAuthOtpRefreshResponse> ResendOtpCodeAsync();
    }
}
