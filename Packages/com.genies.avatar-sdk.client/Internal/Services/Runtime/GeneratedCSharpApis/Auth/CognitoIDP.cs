using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using UnityEngine.Scripting;

namespace Genies.Services.Auth
{
    [Preserve]
    public class Attribute
    {
        public string Name { get; set; }
        public string Value { get; set; }
    }

    // Requests

    [Preserve]
    public class ChangePasswordRequest
    {
        public string AccessToken { get; set; }
        public string PreviousPassword { get; set; }
        public string ProposedPassword { get; set; }
    }

    [Preserve]
    public class ConfirmForgotPasswordRequest
    {
        public string ClientId { get; set; }
        public Dictionary<string, string> ClientMetadata { get; set; }
        public string ConfirmationCode { get; set; }
        public string Password { get; set; }
        public string SecretHash { get; set; }
        public Attribute[] UserAttributes { get; set; }
        public string Username { get; set; }
    }

    [Preserve]
    public class ConfirmSignUpRequest
    {
        public string ClientId { get; set; }
        public string ClientMetadata { get; set; }
        public string ConfirmationCode { get; set; }
        public bool ForceAliasCreation { get; set; }
        public string SecretHash { get; set; }
        public Attribute[] UserAttributes { get; set; }
        public string Username { get; set; }
    }

    [Preserve]
    public class DeleteUserRequest
    {
        public string AccessToken { get; set; }
    }

    [Preserve]
    public class DeleteUserAttributesRequest
    {
        public string AccessToken { get; set; }
        public string[] UserAttributeNames { get; set; }
    }

    [Preserve]
    public class ForgotPasswordRequest
    {
        public string ClientId { get; set; }
        public Dictionary<string, string> ClientMetadata { get; set; }
        public string SecretHash { get; set; }
        public string Username { get; set; }
    }

    [Preserve]
    public class GetUserRequest
    {
        public string AccessToken { get; set; }
    }

    [Preserve]
    public class GlobalSignOutRequest
    {
        public string AccessToken { get; set; }
    }

    [Preserve]
    public class AuthParameters
    {
        [JsonProperty("USERNAME")]
        public string Username { get; set; }
        [JsonProperty("PASSWORD")]
        public string Password { get; set; }
        [JsonProperty("REFRESH_TOKEN")]
        public string RefreshToken { get; set; }
        [JsonProperty("SECRET_HASH")]
        public string SecretHash { get; set; }
    }

    [Preserve]
    public class InitiateAuthRequest
    {
        public string AuthFlow { get; set; }
        public string ClientId { get; set; }
        public AuthParameters AuthParameters { get; set; }
    }

    [Preserve]
    public class ResendConfirmationCodeRequest
    {
        public string ClientId { get; set; }
        public Dictionary<string, string> ClientMetadata { get; set; }
        public string SecretHash { get; set; }
        public string Username { get; set; }
    }

    [Preserve]
    public class RespondToAuthChallengeRequest
    {
        public string ChallengeName { get; set; }
        public Dictionary<string, string> ChallengeResponses { get; set; }
        public string ClientId { get; set; }
        public Dictionary<string, string> ClientMetadata { get; set; }
        public string Session { get; set; }
    }

    [Preserve]
    public class SignUpRequest
    {
        public string ClientId { get; set; }
        public Dictionary<string, string> ClientMetadata { get; set; }
        public string Password { get; set; }
        public string SecretHash { get; set; }
        public Attribute[] UserAttributes { get; set; }
        public string Username { get; set; }
        public Attribute[] ValidationData { get; set; }
    }

    [Preserve]
    public class UpdateUserAttributesRequest
    {
        public string AccessToken { get; set; }
        public Dictionary<string, string> ClientMetadata { get; set; }
        public Attribute[] UserAttributes { get; set; }
    }

    // Responses

    [Preserve]
    public class EmptyResponse
    {
    }

    [Preserve]
    public class AuthenticationResult
    {
        public string AccessToken { get; set; }
        public int ExpiresIn { get; set; }
        public string IdToken { get; set; }
        public string RefreshToken { get; set; }
        public string TokenType { get; set; }
    }

    [Preserve]
    public class CodeDeliveryDetails
    {
        public string AttributeName { get; set; }
        public string DeliveryMedium { get; set; }
        public string Destination { get; set; }
    }

    [Preserve]
    public interface ICodeDeliveryDetailsResponse
    {
        CodeDeliveryDetails CodeDeliveryDetails { get; set; }
    }

    [Preserve]
    public class ForgotPasswordResponse : ICodeDeliveryDetailsResponse
    {
        public CodeDeliveryDetails CodeDeliveryDetails { get; set; }
    }

    [Preserve]
    public class GetUserResponse
    {
        public string PreferredMFASetting { get; set; }
        public Attribute[] UserAttributes { get; set; }
        public string[] UserMFASettingList { get; set; }
        public string Username { get; set; }
    }

    [Preserve]
    public class InitiateAuthResponse
    {
        public AuthenticationResult AuthenticationResult { get; set; }
        public string ChallengeName { get; set; }
        public Dictionary<string, string> ChallengeParameters { get; set; }
        public string Session { get; set; }
    }

    [Preserve]
    public class ResendConfirmationCodeResponse : ICodeDeliveryDetailsResponse
    {
        public CodeDeliveryDetails CodeDeliveryDetails { get; set; }
    }

    [Preserve]
    public class SignUpResponse : ICodeDeliveryDetailsResponse
    {
        public CodeDeliveryDetails CodeDeliveryDetails { get; set; }
        public bool UserConfirmed { get; set; }
        public string UserSub { get; set; }
    }

    [Preserve]
    public class UpdateUserAttributesResponse
    {
        public CodeDeliveryDetails[] CodeDeliveryDetailsList { get; set; }
    }

    public class CognitoIDP
    {
        class AmzTarget
        {
            public const string ChangePassword = "AWSCognitoIdentityProviderService.ChangePassword";
            public const string ConfirmForgotPassword = "AWSCognitoIdentityProviderService.ConfirmForgotPassword";
            public const string ConfirmSignUp = "AWSCognitoIdentityProviderService.ConfirmSignUp";
            public const string DeleteUser = "AWSCognitoIdentityProviderService.DeleteUser";
            public const string DeleteUserAttributes = "AWSCognitoIdentityProviderService.DeleteUserAttributes";
            public const string ForgotPassword = "AWSCognitoIdentityProviderService.ForgotPassword";
            public const string GetUser = "AWSCognitoIdentityProviderService.GetUser";
            public const string GlobalSignOut = "AWSCognitoIdentityProviderService.GlobalSignOut";
            public const string ListDevices = "AWSCognitoIdentityProviderService.ListDevices";
            public const string InitiateAuth = "AWSCognitoIdentityProviderService.InitiateAuth";
            public const string ResendConfirmationCode = "AWSCognitoIdentityProviderService.ResendConfirmationCode";
            public const string RespondToAuthChallenge = "AWSCognitoIdentityProviderService.RespondToAuthChallenge";
            public const string SignUp = "AWSCognitoIdentityProviderService.SignUp";
            public const string UpdateUserAttributes = "AWSCognitoIdentityProviderService.UpdateUserAttributes";
        }

        private HttpClient client;
        private Uri endpoint;

        public CognitoIDP(string region)
        {
            endpoint = new Uri($"https://cognito-idp.{region}.amazonaws.com/");
            client = new HttpClient();
        }

        public async Task ChangePassword(ChangePasswordRequest request)
        {
            await SendRequest<ChangePasswordRequest, EmptyResponse>(AmzTarget.ChangePassword, request);
        }

        public async Task ConfirmForgotPassword(ConfirmForgotPasswordRequest request)
        {
            await SendRequest<ConfirmForgotPasswordRequest, EmptyResponse>(AmzTarget.ConfirmForgotPassword, request);
        }

        public async Task ConfirmSignUp(ConfirmSignUpRequest request)
        {
            await SendRequest<ConfirmSignUpRequest, EmptyResponse>(AmzTarget.ConfirmSignUp, request);
        }

        public async Task DeleteUser(DeleteUserRequest request)
        {
            await SendRequest<DeleteUserRequest, EmptyResponse>(AmzTarget.DeleteUser, request);
        }

        public async Task DeleteUserAttributes(DeleteUserAttributesRequest request)
        {
            await SendRequest<DeleteUserAttributesRequest, EmptyResponse>(AmzTarget.DeleteUserAttributes, request);
        }

        public async Task<ForgotPasswordResponse> ForgotPassword(ForgotPasswordRequest request)
        {
            return await SendRequest<ForgotPasswordRequest, ForgotPasswordResponse>(AmzTarget.ForgotPassword, request);
        }

        public async Task<GetUserResponse> GetUser(GetUserRequest request)
        {
            return await SendRequest<GetUserRequest, GetUserResponse>(AmzTarget.GetUser, request);
        }

        public async Task GlobalSignOut(GlobalSignOutRequest request)
        {
            await SendRequest<GlobalSignOutRequest, EmptyResponse>(AmzTarget.GlobalSignOut, request);
        }

        public async Task<InitiateAuthResponse> InitiateAuth(InitiateAuthRequest request)
        {
            return await SendRequest<InitiateAuthRequest, InitiateAuthResponse>(AmzTarget.InitiateAuth, request);
        }

        public async Task<InitiateAuthResponse> RespondToAuthChallenge(RespondToAuthChallengeRequest request)
        {
            return await SendRequest<RespondToAuthChallengeRequest, InitiateAuthResponse>(AmzTarget.RespondToAuthChallenge, request);
        }

        public async Task<ResendConfirmationCodeResponse> ResendConfirmationCode(ResendConfirmationCodeRequest request)
        {
            return await SendRequest<ResendConfirmationCodeRequest, ResendConfirmationCodeResponse>(AmzTarget.ResendConfirmationCode, request);
        }

        public async Task<SignUpResponse> SignUp(SignUpRequest request)
        {
            return await SendRequest<SignUpRequest, SignUpResponse>(AmzTarget.SignUp, request);
        }

        public async Task<UpdateUserAttributesResponse> UpdateUserAttributes(UpdateUserAttributesRequest request)
        {
            return await SendRequest<UpdateUserAttributesRequest, UpdateUserAttributesResponse>(AmzTarget.UpdateUserAttributes, request);
        }

        public async Task<Res> SendRequest<Req, Res>(string AmzTarget, Req request)
        {
            var jsonContent = JsonConvert.SerializeObject(request);
            var content = new StringContent(jsonContent);
            content.Headers.Add("X-Amz-Target", AmzTarget);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/x-amz-json-1.1");
            var response = await client.PostAsync(endpoint, content);
            var body = await response.Content.ReadAsStringAsync();
            
            if (response.StatusCode == HttpStatusCode.OK)
            {
                return JsonConvert.DeserializeObject<Res>(body);
            }
            else
            {
                var error = JsonConvert.DeserializeObject<ErrorResponse>(body);
                throw Exception.ExceptionForErrorResponse(error);
            }
        }
    }
}
