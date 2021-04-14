using System;

namespace TASagentTwitchBot.Core.Web.Middleware
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class WebSubMethodAttribute : Attribute
    {
        public WebSubMethodAttribute()
        {
        }
    }
}
