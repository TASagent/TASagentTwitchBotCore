using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TASagentTwitchBot.Core.WebServer.Web.Middleware
{
    public class RewindRequiredMiddleware
    {
        private readonly RequestDelegate _next;

        public RewindRequiredMiddleware(
            RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.GetEndpoint()?.Metadata?.GetMetadata<RewindRequiredAttribute>() is RewindRequiredAttribute)
            {
                context.Request.EnableBuffering();
            }

            await _next(context);
        }
    }
}
