using System;

namespace TASagentTwitchBot.Core.WebServer.Web.Middleware
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RewindRequiredAttribute : Attribute
    {
        public RewindRequiredAttribute() { }
    }
}
