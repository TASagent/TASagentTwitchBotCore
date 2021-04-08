using System;
using System.Collections.Generic;
using RestSharp;

namespace TASagentTwitchBot.Core.Web.Extensions
{
    public static class RestExtensions
    {
        public static void AddOptionalParameter(this RestRequest request, string name, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                request.AddParameter(name, value);
            }
        }

        public static void AddOptionalParameter(this RestRequest request, string name, List<string> values)
        {
            if (values != null)
            {
                foreach (string value in values)
                {
                    request.AddParameter(name, value);
                }
            }
        }

        public static void AddOptionalParameter(this RestRequest request, string name, bool? value)
        {
            if (value.HasValue)
            {
                request.AddParameter(name, value.Value);
            }
        }

        public static void AddOptionalParameter(this RestRequest request, string name, int? value)
        {
            if (value.HasValue)
            {
                request.AddParameter(name, value.Value);
            }
        }
    }
}
