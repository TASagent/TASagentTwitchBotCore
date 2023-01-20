using Microsoft.AspNetCore.Mvc.Filters;

namespace TASagentTwitchBot.Core.WebServer.Web;

public class AllowCrossSiteAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext filterContext)
    {
        filterContext.HttpContext.Response.Headers.Add("Access-Control-Allow-Origin", "https://tas.wtf");
        filterContext.HttpContext.Response.Headers.Add("Access-Control-Allow-Headers", "*");
        filterContext.HttpContext.Response.Headers.Add("Access-Control-Allow-Credentials", "true");

        base.OnActionExecuting(filterContext);
    }
}
