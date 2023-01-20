using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TASagentTwitchBot.Core.WebServer.Areas.Identity.Pages.Account.Manage;

public partial class TokenModel : PageModel
{
    private readonly UserManager<Models.ApplicationUser> userManager;
    private readonly ILogger<DownloadPersonalDataModel> logger;

    public string? Username { get; set; } = null;

    [Display(Name = "Bot Token")]
    public string? BotToken { get; set; } = null;

    [TempData]
    public string? StatusMessage { get; set; } = null;

    public TokenModel(
        UserManager<Models.ApplicationUser> userManager,
        ILogger<DownloadPersonalDataModel> logger)
    {
        this.userManager = userManager;
        this.logger = logger;
    }

    private async Task LoadAsync(Models.ApplicationUser user)
    {
        string? botToken = await userManager.GetAuthenticationTokenAsync(user, "Self", "BotToken");
        if (string.IsNullOrEmpty(botToken))
        {
            botToken = "None";
        }

        BotToken = botToken;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        Models.ApplicationUser? user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        await LoadAsync(user);
        return Page();
    }

    public async Task<IActionResult> OnPostRegenerateBotTokenAsync()
    {
        Models.ApplicationUser? user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        logger.LogInformation("User with ID '{UserId}' Regenerating BotToken.", userManager.GetUserId(User));

        await userManager.SetAuthenticationTokenAsync(user, "Self", "BotToken", Guid.NewGuid().ToString());
        await userManager.UpdateAsync(user);

        //Want to disconnect oldToken users

        StatusMessage = "Your BotToken has been updated";
        return RedirectToPage();
    }
}
