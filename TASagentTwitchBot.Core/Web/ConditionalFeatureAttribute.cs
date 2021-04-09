using System;

namespace TASagentTwitchBot.Core.Web
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ConditionalFeatureAttribute : Attribute
    {
        public string FeatureSet { get; }

        public ConditionalFeatureAttribute(string featureSet)
        {
            FeatureSet = featureSet;
        }
    }
}
