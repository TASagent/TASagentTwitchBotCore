using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace TASagentTwitchBot.Core.Web
{
    public class ConditionalControllerFeatureProvider : IApplicationFeatureProvider<ControllerFeature>
    {
        private readonly HashSet<string> disabledFeatures;

        public ConditionalControllerFeatureProvider(string[] disabledFeatures)
        {
            this.disabledFeatures = new HashSet<string>(disabledFeatures);
        }

        public void PopulateFeature(
            IEnumerable<ApplicationPart> parts,
            ControllerFeature feature)
        {
            foreach (TypeInfo controllerType in feature.Controllers.ToArray())
            {
                if (controllerType.GetCustomAttribute<ConditionalFeatureAttribute>() is ConditionalFeatureAttribute conditionalFeatureAttribute &&
                    disabledFeatures.Contains(conditionalFeatureAttribute.FeatureSet))
                {
                    feature.Controllers.Remove(controllerType);
                }
            }
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ConditionalFeatureAttribute : Attribute
    {
        public string FeatureSet { get; }

        public ConditionalFeatureAttribute(string featureSet)
        {
            FeatureSet = featureSet;
        }
    }
}
