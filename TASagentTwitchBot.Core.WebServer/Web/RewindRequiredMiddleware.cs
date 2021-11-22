namespace TASagentTwitchBot.Core.WebServer.Web.Middleware;

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
        if (context.GetEndpoint()?.Metadata?.GetMetadata<RewindRequiredAttribute>() is not null)
        {
            context.Request.EnableBuffering();
        }

        await _next(context);
    }
}
