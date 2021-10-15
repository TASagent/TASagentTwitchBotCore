using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Principal;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using TASagentTwitchBot.Core.WebServer.Models;

namespace TASagentTwitchBot.Core.WebServer.Tokens
{
    public class TokenAuthenticationOptions : AuthenticationSchemeOptions
    {
    }


    public class TokenAuthenticationHandler : AuthenticationHandler<TokenAuthenticationOptions>
    {
        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly API.Twitch.HelixServerHelper helixServerHelper;

        private const string TOKEN_HEADER = "Authorization";
        private const string TOKEN_IDENTIFIER = "Bearer";

        private const string USER_ID_HEADER = "User-Id";

        public const string SCHEME_NAME = "Token";

        public TokenAuthenticationHandler(
            IOptionsMonitor<TokenAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            API.Twitch.HelixServerHelper helixServerHelper)
            : base(options, logger, encoder, clock)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.helixServerHelper = helixServerHelper;
        }

        protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey(TOKEN_HEADER))
            {
                return AuthenticateResult.Fail($"No Authorization {TOKEN_IDENTIFIER} Token included");
            }

            string authHeader = Request.Headers[TOKEN_HEADER];
            if (string.IsNullOrEmpty(authHeader))
            {
                return AuthenticateResult.Fail($"No Authorization {TOKEN_IDENTIFIER} Token included");
            }

            if (!authHeader.StartsWith(TOKEN_IDENTIFIER, StringComparison.OrdinalIgnoreCase))
            {
                return AuthenticateResult.Fail($"Authorization token does not start with {TOKEN_IDENTIFIER}");
            }

            string token = authHeader[TOKEN_IDENTIFIER.Length..].Trim();
            if (string.IsNullOrEmpty(token))
            {
                return AuthenticateResult.Fail($"No Authorization {TOKEN_IDENTIFIER} Token included");
            }

            string userId = Request.Headers[USER_ID_HEADER];
            if (string.IsNullOrEmpty(userId))
            {
                return AuthenticateResult.Fail("No User-Id");
            }

            try
            {
                ApplicationUser matchingUser = await userManager.FindByNameAsync(userId);

                if (matchingUser is null)
                {
                    //User Not Found
                    return AuthenticateResult.Fail("Unauthorized");
                }

                string botToken = await userManager.GetAuthenticationTokenAsync(matchingUser, "Self", "BotToken");

                if (string.IsNullOrEmpty(botToken))
                {
                    //No Bot Token Set
                    return AuthenticateResult.Fail("Unauthorized");
                }

                if (!string.Equals(botToken, token))
                {
                    //Token is wrong
                    return AuthenticateResult.Fail("Unauthorized");
                }

                List<Claim> claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, matchingUser.UserName),
                    new Claim(ClaimTypes.NameIdentifier, matchingUser.Id)
                };

                foreach (string role in await userManager.GetRolesAsync(matchingUser))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                //Found user, and the tokens matched
                ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(
                    identity: new ClaimsIdentity(
                        claims: claims,
                        authenticationType: Scheme.Name));

                AuthenticationTicket ticket = new AuthenticationTicket(
                    principal: claimsPrincipal,
                    authenticationScheme: Scheme.Name);

                return AuthenticateResult.Success(ticket);
            }
            catch (Exception ex)
            {
                return AuthenticateResult.Fail(ex.Message);
            }
        }
    }
}
