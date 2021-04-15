using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace TASagentTwitchBot.Core.Web.Middleware
{
    public class WebSubHandlerMiddleware
    {
        public const int WEBSUB_PORT = 9000;

        private readonly RequestDelegate _next;
        private readonly WebSub.WebSubHandler webSubHandler;
        private readonly ICommunication communication;

        public WebSubHandlerMiddleware(
            RequestDelegate next,
            ICommunication communication,
            WebSub.WebSubHandler webSubHandler)
        {
            _next = next;
            this.webSubHandler = webSubHandler;
            this.communication = communication;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.ToString().StartsWith("/TASagentBotAPI/WebSub/"))
            {
                if (context.Request.Method == "GET" && context.Request.Query.ContainsKey("hub.challenge"))
                {
                    if (!context.Request.Headers.ContainsKey("Connection"))
                    {
                        communication.SendErrorMessage($"Expected WebSub Challenges to contain connection string in header.");
                        communication.SendDebugMessage($"  Current Challenge Headers:");
                        foreach (var header in context.Request.Headers)
                        {
                            communication.SendDebugMessage($"    {header.Key}: {header.Value}");
                        }
                    }

                    if (context.Request.Headers["Connection"] == "close")
                    {
                        //Closing connection
                        webSubHandler.CloseConnection(context.Request.Path);
                    }
                    else if (context.Request.Headers["Connection"] == "Keep-Alive")
                    {
                        if (!webSubHandler.VerifyConnection(context.Request.Path))
                        {
                            context.Response.StatusCode = 401;
                            communication.SendDebugMessage("Rejecting unsolicited WebSub request");
                            await context.Response.WriteAsync("Unsolicited Request");
                            return;
                        }
                    }
                    else
                    {
                        communication.SendWarningMessage($"Unexpected connection string: {context.Request.Headers["Connection"]}");
                    }

                    //Request to initiate
                    string hubChallenge = context.Request.Query["hub.challenge"];

                    if (string.IsNullOrEmpty(hubChallenge))
                    {
                        communication.SendErrorMessage($"Failed to subscribe to Follows: {context.Request.Query["hub.reason"]}. Aborting.");
                        await Task.Delay(2000);
                        throw new Exception($"Failed to subscribe to Follows: {context.Request.Query["hub.reason"]}. Aborting.");
                    }

                    byte[] buffer = Encoding.UTF8.GetBytes(hubChallenge);

                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "text/plain";
                    context.Response.ContentLength = buffer.Length;
                    await context.Response.Body.WriteAsync(buffer, 0, buffer.Length);
                    context.Response.Body.Close();

                    return;
                }

                if (context.GetEndpoint()?.Metadata?.GetMetadata<WebSubMethodAttribute>() is not null)
                {
                    if (!context.Request.Headers.ContainsKey("X-Hub-Signature"))
                    {
                        //Hub Signature Missing
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Hub Signature is missing");
                        communication.SendDebugMessage($"Hub Signature is missing in request to {context.Request.Path}");
                        return;
                    }

                    context.Request.EnableBuffering();

                    bool verified = await webSubHandler.VerifyHubMessage(
                        route: context.Request.Path.ToString(),
                        signature: context.Request.Headers["X-Hub-Signature"],
                        request: context.Request);

                    if (!verified)
                    {
                        //Hub Signature Wrong
                        context.Response.StatusCode = 401;
                        await context.Response.WriteAsync("Hub Signature is incorrect");
                        communication.SendWarningMessage($"Hub Signature is incorrect in request to {context.Request.Path}");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}
