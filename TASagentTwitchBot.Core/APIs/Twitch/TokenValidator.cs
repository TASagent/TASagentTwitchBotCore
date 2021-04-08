using System;
using System.Threading.Tasks;

namespace TASagentTwitchBot.Core.API.Twitch
{
    public class TokenValidator
    {
        private readonly Config.IBotConfigContainer botConfigContainer;
        private readonly ICommunication communication;
        private readonly HelixHelper helixHelper;

        private readonly bool useBotToken;

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

        public TokenValidator(
            Config.IBotConfigContainer botConfigContainer,
            ICommunication communication,
            bool useBotToken,
            HelixHelper helixHelper)
        {
            this.communication = communication;
            this.useBotToken = useBotToken;
            this.helixHelper = helixHelper;
            this.botConfigContainer = botConfigContainer;
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
                string authCode;

                if (useBotToken)
                {
                    authCode = await helixHelper.GetBotCode();
                }
                else
                {
                    authCode = await helixHelper.GetBroadcasterCode();
                }

                if (string.IsNullOrEmpty(authCode))
                {
                    return false;
                }

                //Try to get a new Token
                TokenRequest request = await helixHelper.GetToken(authCode);

                //Did we receive a new Access Token?
                if (request == null)
                {
                    //We failed to get a new token
                    return false;
                }

                //Update values
                if (useBotToken)
                {
                    botConfigContainer.BotConfig.BotAccessToken = request.AccessToken;
                    botConfigContainer.BotConfig.BotRefreshToken = request.RefreshToken;
                }
                else
                {
                    botConfigContainer.BotConfig.BroadcasterAccessToken = request.AccessToken;
                    botConfigContainer.BotConfig.BroadcasterRefreshToken = request.RefreshToken;
                }

                //Update saved accessToken
                botConfigContainer.SerializeData();

                //Does the token validate?
                return await TryValidateToken();
            }
        }

        public async Task<bool> TryToValidate()
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
            if (string.IsNullOrEmpty(useBotToken ? botConfigContainer.BotConfig.BotAccessToken : botConfigContainer.BotConfig.BroadcasterAccessToken))
            {
                return false;
            }

            //Does the AccessToken validate?
            return await TryValidateToken();
        }

        private async Task<bool> TryTokenRefresh()
        {
            //Do we have a RefreshToken?
            if (string.IsNullOrEmpty(useBotToken ? botConfigContainer.BotConfig.BotRefreshToken : botConfigContainer.BotConfig.BroadcasterRefreshToken))
            {
                //We can't try a refresh without a RefreshToken
                return false;
            }

            //Try a refresh
            TokenRefreshRequest request;


            if (useBotToken)
            {
                request = await helixHelper.RefreshToken(botConfigContainer.BotConfig.BotRefreshToken);
            }
            else
            {
                request = await helixHelper.RefreshToken(botConfigContainer.BotConfig.BroadcasterRefreshToken);
            }

            //Did we receive an Access Token?
            if (request == null)
            {
                //Refresh attempt failed
                return false;
            }

            //Update Tokens
            if (useBotToken)
            {
                botConfigContainer.BotConfig.BotAccessToken = request.AccessToken;
                botConfigContainer.BotConfig.BotRefreshToken = request.RefreshToken;
            }
            else
            {
                botConfigContainer.BotConfig.BroadcasterAccessToken = request.AccessToken;
                botConfigContainer.BotConfig.BroadcasterRefreshToken = request.RefreshToken;
            }

            //Update saved accessToken
            botConfigContainer.SerializeData();

            //Does the token validate?
            return await TryValidateToken();
        }

        private async Task<bool> TryValidateToken()
        {
            //Request Twitch validate our access_token
            TokenValidationRequest validationRequest = await helixHelper.ValidateToken(useBotToken ?
                botConfigContainer.BotConfig.BotAccessToken : botConfigContainer.BotConfig.BroadcasterAccessToken);

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
    }
}
