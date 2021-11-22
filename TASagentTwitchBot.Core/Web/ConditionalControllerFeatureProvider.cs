using System.Reflection;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace TASagentTwitchBot.Core.Web;

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
            foreach (ConditionalFeatureAttribute conditionalFeatureAttribute in controllerType.GetCustomAttributes<ConditionalFeatureAttribute>())
            {
                if (disabledFeatures.Contains(conditionalFeatureAttribute.FeatureSet))
                {
                    feature.Controllers.Remove(controllerType);
                    break;
                }
            }
        }
    }
}
