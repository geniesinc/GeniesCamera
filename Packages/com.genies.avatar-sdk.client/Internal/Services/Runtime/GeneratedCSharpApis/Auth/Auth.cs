using Genies.Services.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Genies.Services.Auth
{
    public class Auth
    {
        public class Challenge
        {
            protected Auth client;
            protected string username;
            protected string session;

            public Challenge(Auth client, string username, string session)
            {
                this.client = client;
                this.username = username;
                this.session = session;
            }
        }

        public class NewPasswordRequiredChallenge : Challenge
        {
            public NewPasswordRequiredChallenge(Auth client, string username, string session) : base(client, username, session) { }

            public async Task<Challenge> NewPassword(string password)
            {
                var request = new RespondToAuthChallengeRequest
                {
                    ClientId = client.clientId,
                    ChallengeName = "NEW_PASSWORD_REQUIRED",
                    ChallengeResponses = new Dictionary<string, string>
                    {
                        { "USERNAME", username },
                        { "NEW_PASSWORD", password }
                    },
                    Session = session
                };
                var response = await client.idp.RespondToAuthChallenge(request);
                return client.HandleInitiateAuthResponse(response, username);
            }
        }

        public enum ConfirmationDeliveryMedium
        {
            Unknown,
            Sms,
            Email
        };

        public string clientId;
        public string clientSecret;
        public CognitoIDP idp = new CognitoIDP("us-west-2");
        private Timer refreshTimer;

        public string AccessToken { get; set; }
        public string IdToken { get; set; }
        public DateTime Expires { get; set; }
        public string RefreshToken { get; set; }
        public string RefreshSub { get; set; }

        public Auth(string clientId, string clientSecret = null, string refreshToken = null)
        {
            this.clientId = clientId;
            this.clientSecret = clientSecret;
            RefreshToken = refreshToken;
        }

        public void StartAutoTokenRefresh()
        {
            CancelAutoTokenRefresh();
            
            if (Expires > DateTime.Now)
            {
                var timeSpan = Expires - DateTime.Now.AddMinutes(5);
                refreshTimer = new Timer(delegate {
                    var task = RefreshTokens();
                    task.Wait();
                }, null, timeSpan, Timeout.InfiniteTimeSpan);
            }
        }

        private void CancelAutoTokenRefresh()
        {
            if (refreshTimer != null)
            {
                refreshTimer.Dispose();
                refreshTimer = null;
            }
        }

        private Challenge HandleInitiateAuthResponse(InitiateAuthResponse response, string username)
        {
            if (response.AuthenticationResult != null)
            {
                AccessToken = response.AuthenticationResult.AccessToken;
                IdToken = response.AuthenticationResult.IdToken;
                Expires = DateTime.Now.AddSeconds(response.AuthenticationResult.ExpiresIn);

                // Refresh token might be missing if this from a refresh token call
                if (response.AuthenticationResult.RefreshToken != null)
                {
                    RefreshToken = response.AuthenticationResult.RefreshToken;
                }

                // Update the access token used by the APIs
                var defaultClient = Configuration.Default;
                defaultClient.AccessToken = IdToken;

                StartAutoTokenRefresh();

                return null;
            }
            else if (response.ChallengeName != null)
            {
                switch (response.ChallengeName)
                {
                    case "NEW_PASSWORD_REQUIRED":
                        return new NewPasswordRequiredChallenge(this, username, response.Session);
                }
            }
            return null;
        }

        private ConfirmationDeliveryMedium HandleCodeDeliveryDetailsResponse(ICodeDeliveryDetailsResponse response)
        {
            switch (response.CodeDeliveryDetails.DeliveryMedium)
            {
                case "SMS":
                    return ConfirmationDeliveryMedium.Sms;
                case "EMAIL":
                    return ConfirmationDeliveryMedium.Email;
                default:
                    return ConfirmationDeliveryMedium.Unknown;
            }
        }

        public async Task<Challenge> SignIn(string username, string password)
        {
            CancelAutoTokenRefresh();
            var request = new InitiateAuthRequest
            {
                AuthFlow = "USER_PASSWORD_AUTH",
                ClientId = clientId,
                AuthParameters = new AuthParameters
                {
                    Username = username,
                    Password = password
                }
            };
            var response = await idp.InitiateAuth(request);
            return HandleInitiateAuthResponse(response, username);
        }

        public async Task<InitiateAuthResponse> RefreshTokens(string refreshToken = null)
        {
            CancelAutoTokenRefresh();
            if (refreshToken != null)
            {
                RefreshToken = refreshToken;
            }
            var request = new InitiateAuthRequest
            {
                AuthFlow = "REFRESH_TOKEN_AUTH",
                ClientId = clientId,
                AuthParameters = new AuthParameters
                {
                    RefreshToken = RefreshToken
                }
            };
            InitiateAuthResponse response = await idp.InitiateAuth(request);

            // Ignoring any challenges at this point
            var challenge = HandleInitiateAuthResponse(response, RefreshSub);

            return response;
        }

        public void SignOut()
        {
            CancelAutoTokenRefresh();
            AccessToken = null;
            IdToken = null;
            RefreshToken = null;
            RefreshSub = null;
        }

        public async Task<ConfirmationDeliveryMedium> SignUp(string username, string password, Dictionary<string, string> attributes)
        {
            var userAttributes = attributes.Select(i => new Attribute { Name= i.Key, Value = i.Value }).ToArray();
            var request = new SignUpRequest
            {
                ClientId = clientId,
                Username = username,
                Password = password,
                UserAttributes = userAttributes
            };
            var response = await idp.SignUp(request);
            return HandleCodeDeliveryDetailsResponse(response);
        }

        public async Task ConfirmSignUp(string username, string confirmationCode)
        {
            var request = new ConfirmSignUpRequest
            {
                ClientId = clientId,
                Username = username,
                ConfirmationCode = confirmationCode
            };
            await idp.ConfirmSignUp(request);
        }

        public async Task ChangePassword(string previousPassword, string proposedPassword)
        {
            var request = new ChangePasswordRequest
            {
                AccessToken = AccessToken,
                PreviousPassword = previousPassword,
                ProposedPassword = proposedPassword
            };
            await idp.ChangePassword(request);
       }

        public async Task<ConfirmationDeliveryMedium> ForgotPassword(string username)
        {
            var request = new ForgotPasswordRequest
            {
                ClientId = clientId,
                Username = username
            };
            var response = await idp.ForgotPassword(request);
            return HandleCodeDeliveryDetailsResponse(response);
        }

        public async Task ConfirmForgotPassword(string username, string password, string confirmationCode)
        {
            var request = new ConfirmForgotPasswordRequest
            {
                ClientId = clientId,
                Username = username,
                Password = password,
                ConfirmationCode = confirmationCode
            };
            await idp.ConfirmForgotPassword(request);
        }

        public async Task<ConfirmationDeliveryMedium> ResendConfirmationCode(string username)
        {
            var request = new ResendConfirmationCodeRequest
            {
                ClientId = clientId,
                Username = username
            };
            var response = await idp.ResendConfirmationCode(request);
            return HandleCodeDeliveryDetailsResponse(response);
        }

        public async Task<Dictionary<string, string>> GetUserAttributes()
        {
            var request = new GetUserRequest
            {
                AccessToken = AccessToken
            };
            var response = await idp.GetUser(request);
            var attributes = response.UserAttributes.ToDictionary(o => o.Name, o => o.Value);
            return attributes;
        }

        public async Task<Dictionary<string, ConfirmationDeliveryMedium>> UpdateUserAttributes(Dictionary<string, string> attributes)
        {
            var userAttributes = attributes.Select(i => new Attribute { Name= i.Key, Value = i.Value }).ToArray();
            var request = new UpdateUserAttributesRequest
            {
                AccessToken = AccessToken,
                UserAttributes = userAttributes
            };
            var response = await idp.UpdateUserAttributes(request);
            if (response.CodeDeliveryDetailsList != null)
            {
                return response.CodeDeliveryDetailsList.ToDictionary(
                    o => o.AttributeName,
                    o => (ConfirmationDeliveryMedium)Enum.Parse(typeof(ConfirmationDeliveryMedium), o.DeliveryMedium, true));
            }
            else
            {
                return new Dictionary<string, ConfirmationDeliveryMedium>();
            }
        }

        public async Task DeleteUserAttributes(string[] attributeNames)
        {
            var request = new DeleteUserAttributesRequest
            {
                AccessToken = AccessToken,
                UserAttributeNames = attributeNames
            };
            await idp.DeleteUserAttributes(request);
        }
    }
}
