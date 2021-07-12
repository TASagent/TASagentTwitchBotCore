using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace TASagentTwitchBot.Core.API.OAuth
{
    public interface IOAuthHandler
    {
        Task<TokenRequest> GetToken(string authCode, string redirectURI);
        Task<TokenRefreshRequest> RefreshToken(string refreshToken);
        Task<TokenValidationRequest> ValidateToken(string accessToken);
        Task<bool> ExpireToken(string token);
    }
}
