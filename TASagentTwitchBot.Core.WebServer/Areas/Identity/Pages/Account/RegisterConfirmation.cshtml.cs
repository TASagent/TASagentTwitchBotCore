using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace TASagentTwitchBot.Core.WebServer.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterConfirmationModel : PageModel
{
    private readonly UserManager<Models.ApplicationUser> _userManager;

    public bool DisplayConfirmAccountLink { get; set; }
    public string? Email { get; set; }
    public string? EmailConfirmationUrl { get; set; }

    public RegisterConfirmationModel(
        UserManager<Models.ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<IActionResult> OnGetAsync(
        string? email,
        string? returnUrl = null)
    {
        if (email is null)
        {
            return RedirectToPage("/Index");
        }

        Models.ApplicationUser? user = await _userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return NotFound($"Unable to load user with email '{email}'.");
        }

        Email = email;
        // Once you add a real email sender, you should remove this code that lets you confirm the account
        DisplayConfirmAccountLink = true;
        if (DisplayConfirmAccountLink)
        {
            string? userId = await _userManager.GetUserIdAsync(user);
            string? code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            EmailConfirmationUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = userId, code = code, returnUrl = returnUrl },
                protocol: Request.Scheme)!;
        }

        return Page();
    }
}
