using System.Text.Json;
using System.Reflection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TASagentTwitchBot.Core.WebServer.Areas.Identity.Pages.Account.Manage;

public class DownloadPersonalDataModel : PageModel
{
    private readonly UserManager<Models.ApplicationUser> userManager;
    private readonly ILogger<DownloadPersonalDataModel> logger;

    public DownloadPersonalDataModel(
        UserManager<Models.ApplicationUser> userManager,
        ILogger<DownloadPersonalDataModel> logger)
    {
        this.userManager = userManager;
        this.logger = logger;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Models.ApplicationUser? user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        logger.LogInformation("User with ID '{UserId}' asked for their personal data.", userManager.GetUserId(User));

        // Only include personal data for download
        Dictionary<string, string> personalData = new Dictionary<string, string>();
        IEnumerable<PropertyInfo> personalDataProps = typeof(Models.ApplicationUser).GetProperties().Where(
                        prop => Attribute.IsDefined(prop, typeof(PersonalDataAttribute)));
        foreach (PropertyInfo? p in personalDataProps)
        {
            personalData.Add(p.Name, p.GetValue(user)?.ToString() ?? "null");
        }

        IList<UserLoginInfo> logins = await userManager.GetLoginsAsync(user);
        foreach (UserLoginInfo l in logins)
        {
            personalData.Add($"{l.LoginProvider} external login provider key", l.ProviderKey);
        }

        Response.Headers.Add("Content-Disposition", "attachment; filename=PersonalData.json");
        return new FileContentResult(JsonSerializer.SerializeToUtf8Bytes(personalData), "application/json");
    }
}
