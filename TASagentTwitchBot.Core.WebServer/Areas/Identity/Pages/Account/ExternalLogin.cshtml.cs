using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using TASagentTwitchBot.Core.WebServer.Models;

namespace TASagentTwitchBot.Core.WebServer.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class ExternalLoginModel : PageModel
{
    private readonly SignInManager<ApplicationUser> signInManager;
    private readonly UserManager<ApplicationUser> userManager;
    private readonly ILogger<ExternalLoginModel> logger;

    public string? ProviderDisplayName { get; set; }

    public string? ReturnUrl { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public ExternalLoginModel(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<ExternalLoginModel> logger)
    {
        this.signInManager = signInManager;
        this.userManager = userManager;
        this.logger = logger;
    }

    public IActionResult OnGetAsync()
    {
        return RedirectToPage("./Login");
    }

    public IActionResult OnPost(
        string provider,
        string? returnUrl = null)
    {
        // Request a redirect to the external login provider.
        string redirectUrl = Url.Page("./ExternalLogin", pageHandler: "Callback", values: new { returnUrl })!;
        AuthenticationProperties properties = signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
        return new ChallengeResult(provider, properties);
    }

    public async Task<IActionResult> OnGetCallbackAsync(
        string? returnUrl = null,
        string? remoteError = null)
    {
        returnUrl ??= Url.Content("~/");
        if (remoteError is not null)
        {
            ErrorMessage = $"Error from external provider: {remoteError}";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        ExternalLoginInfo info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            ErrorMessage = "Error loading external login information.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        // Sign in the user with this external login provider if the user already has a login.
        var result = await signInManager.ExternalLoginSignInAsync(
            loginProvider: info.LoginProvider,
            providerKey: info.ProviderKey,
            isPersistent: false,
            bypassTwoFactor: true);

        if (result.Succeeded)
        {
            logger.LogInformation("{Name} logged in with {LoginProvider} provider.", info.Principal!.Identity!.Name, info.LoginProvider);
            return LocalRedirect(returnUrl);
        }

        if (result.IsLockedOut)
        {
            return RedirectToPage("./Lockout");
        }

        // If the user does not have an account, then ask the user to create an account.
        ReturnUrl = returnUrl;
        ProviderDisplayName = info.ProviderDisplayName;

        return Page();
    }

    public async Task<IActionResult> OnPostConfirmationAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");

        // Get the information about the user from the external login provider
        ExternalLoginInfo info = await signInManager.GetExternalLoginInfoAsync();
        if (info is null)
        {
            ErrorMessage = "Error loading external login information during confirmation.";
            return RedirectToPage("./Login", new { ReturnUrl = returnUrl });
        }

        if (ModelState.IsValid)
        {
            string displayName = info.Principal.FindFirstValue("urn:twitch:displayname");
            string email = info.Principal.FindFirstValue(ClaimTypes.Email);

            ApplicationUser user = new ApplicationUser
            {
                UserName = displayName,
                Email = email,
                TwitchBroadcasterId = info.ProviderKey,
                TwitchBroadcasterName = displayName,
                EmailConfirmed = true,
                SubscriptionSecret = Guid.NewGuid().ToString("N")
            };

            IdentityResult result = await userManager.CreateAsync(user);
            if (result.Succeeded)
            {
                result = await userManager.AddLoginAsync(user, info);

                if (result.Succeeded)
                {
                    logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                    await userManager.AddToRoleAsync(user, Enums.Roles.Basic.ToString());

                    //Set BotToken
                    await userManager.SetAuthenticationTokenAsync(user, "Self", "BotToken", Guid.NewGuid().ToString());

                    ////Stash Refresh_Token
                    //await userManager.SetAuthenticationTokenAsync(
                    //    user: user,
                    //    loginProvider: "Twitch",
                    //    tokenName: "refresh_token",
                    //    tokenValue: info.AuthenticationTokens.First(x => x.Name == "refresh_token").Value);

                    await signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);

                    return LocalRedirect(returnUrl);
                }
            }

            foreach (IdentityError error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        ProviderDisplayName = info.ProviderDisplayName;
        ReturnUrl = returnUrl;
        return Page();
    }
}
