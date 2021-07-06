using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TASagentTwitchBot.Core.Web.Middleware
{
    public class AuthCheckerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Config.BotConfiguration botConfig;
        private readonly ICommunication communication;

        public AuthCheckerMiddleware(
            RequestDelegate next,
            Config.BotConfiguration botConfig,
            ICommunication communication)
        {
            _next = next;
            this.botConfig = botConfig;
            this.communication = communication;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.GetEndpoint()?.Metadata?.GetMetadata<AuthRequiredAttribute>() is AuthRequiredAttribute authReq)
            {
                if (!context.Request.Headers.ContainsKey("Authorization"))
                {
                    //No Auth Key
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Auth key is missing");
                    communication.SendDebugMessage($"Auth key is missing in request to {context.Request.Path}");
                    return;
                }

                AuthDegree authDegree = botConfig.AuthConfiguration.CheckAuthString(context.Request.Headers["Authorization"]);

                if (authDegree == AuthDegree.None)
                {
                    //Unauthorized
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Auth key is incorrect");
                    communication.SendDebugMessage($"Auth key is wrong in request to {context.Request.Path}");
                    return;
                }

                if (authDegree < authReq.authDegree)
                {
                    //Unauthorized
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsync("Auth is insufficient");
                    communication.SendDebugMessage($"{authDegree} Auth is insufficient for request to {context.Request.Path}");
                    return;
                }
            }

            await _next(context);
        }
    }
}
