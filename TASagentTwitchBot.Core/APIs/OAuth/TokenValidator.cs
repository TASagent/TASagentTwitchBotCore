using System;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace TASagentTwitchBot.Core.API.OAuth
{
    public interface ITokenValidator
    {
        void SetCode(string code, string state);
        Task<bool> TryToConnect();
        void RunValidator();
    }

    public abstract class TokenValidator : ITokenValidator
    {
        protected readonly ICommunication communication;
        protected readonly IOAuthHandler oauthHandler;

        /// <summary>
        /// How frequently we check to see if it's time to rerun validation
        /// </summary>
        private readonly TimeSpan validationCheckInterval = new TimeSpan(hours: 0, minutes: 5, seconds: 0);

        /// <summary>
        /// How frequently we rerun validation
        /// </summary>
        private readonly TimeSpan validationInterval = new TimeSpan(hours: 0, minutes: 30, seconds: 0);

        private readonly TimeSpan tokenRefreshRange = new TimeSpan(hours: 1, minutes: 0, seconds: 0);

        private DateTime nextValidateTime;
        private TaskCompletionSource<(string code, string state)> codeCallback = null;

        protected abstract string AccessToken { get; set; }
        protected abstract string RefreshToken { get; set; }
        protected abstract string RedirectURI { get; }

        public TokenValidator(
            ICommunication communication,
            IOAuthHandler oauthHandler)
        {
            this.communication = communication;
            this.oauthHandler = oauthHandler;
        }

        public void SetCode(string code, string state)
        {
            if (codeCallback == null)
            {
                communication.SendWarningMessage($"Received OAuth Code when not awaiting one.");
                return;
            }

            codeCallback.SetResult((code, state));
        }

        public async Task<bool> TryToConnect()
        {
            if (await TryExistingToken())
            {
                return true;
            }
            else if (await TryTokenRefresh())
            {
                return true;
            }
            else
            {
                //We require reauthorization
                string authCode = await GetCode();

                if (string.IsNullOrEmpty(authCode))
                {
                    return false;
                }

                //Try to get a new Token
                TokenRequest request = await oauthHandler.GetToken(authCode, RedirectURI);

                //Did we receive a new Access Token?
                if (request == null)
                {
                    //We failed to get a new token
                    return false;
                }

                AccessToken = request.AccessToken;
                RefreshToken = request.RefreshToken;

                //Update saved accessToken
                SaveChanges();

                //Does the token validate?
                return await TryValidateToken();
            }
        }

        private async Task<bool> TryToValidate()
        {
            if (await TryExistingToken())
            {
                return true;
            }
            else if (await TryTokenRefresh())
            {
                return true;
            }

            return false;
        }

        private async Task<bool> TryExistingToken()
        {
            //Do we have an AccessToken?
            if (string.IsNullOrEmpty(AccessToken))
            {
                return false;
            }

            //Does the AccessToken validate?
            return await TryValidateToken();
        }

        private async Task<bool> TryTokenRefresh()
        {
            //Do we have a RefreshToken?
            if (string.IsNullOrEmpty(RefreshToken))
            {
                //We can't try a refresh without a RefreshToken
                return false;
            }

            //Try a refresh
            TokenRefreshRequest request = await oauthHandler.RefreshToken(RefreshToken);

            //Did we receive an Access Token?
            if (request == null)
            {
                //Refresh attempt failed
                return false;
            }

            //Update Tokens
            AccessToken = request.AccessToken;
            RefreshToken = request.RefreshToken;

            //Update saved accessToken
            SaveChanges();

            //Does the token validate?
            return await TryValidateToken();
        }

        private async Task<bool> TryValidateToken()
        {
            //Request validation of our access_token
            TokenValidationRequest validationRequest = await oauthHandler.ValidateToken(AccessToken);

            //Was our token validated?
            if (validationRequest != null)
            {
                //Validated
                //Do we need to refresh the token anyway?
                TimeSpan remainingTime = new TimeSpan(hours: 0, minutes: 0, seconds: validationRequest.ExpiresIn);

                if (remainingTime < tokenRefreshRange)
                {
                    return false;
                }

                //Token is good
                return true;
            }

            return false;
        }

        public void ResetValidator() => nextValidateTime = DateTime.Now + validationInterval;

        public async void RunValidator()
        {
            nextValidateTime = DateTime.Now + validationInterval;

            while (true)
            {
                if (DateTime.Now < nextValidateTime)
                {
                    await Task.Delay(validationCheckInterval);
                }
                else
                {
                    if (await TryToValidate())
                    {
                        nextValidateTime = DateTime.Now + validationInterval;
                    }
                    else
                    {
                        communication.SendErrorMessage($"Error!  Failed to validate!");
                        nextValidateTime = DateTime.Now + validationInterval;
                    }
                }
            }
        }


        private async Task<string> GetCode()
        {
            string code = null;
            string stateString = GenerateRandomStringToken();

            SendCodeRequest(stateString);

            codeCallback = new TaskCompletionSource<(string code, string state)>();

            //Wait up to 10 minutes
            await Task.WhenAny(
                codeCallback.Task,
                Task.Delay(1000 * 60 * 10));

            if (codeCallback.Task.IsCompleted)
            {
                (string code, string state) result = codeCallback.Task.Result;

                if (result.state != stateString)
                {
                    communication.SendWarningMessage($"OAuth state string did not match:  SENT \"{stateString}\"  RECEIVED \"{result.state}\"");
                }
                else
                {
                    code = result.code;
                }
            }

            codeCallback = null;
            return code;
        }

        protected abstract void SendCodeRequest(string stateString);
        protected abstract void SaveChanges();

        private static string GenerateRandomStringToken()
        {
            using RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();

            byte[] data = new byte[30];
            rngCsp.GetBytes(data);
            return Convert.ToBase64String(data);
        }
    }
}
