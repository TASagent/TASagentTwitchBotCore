using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TASagentTwitchBot.Core.WebServer.Areas.Identity.Pages.Account.Manage;

public class PersonalDataModel : PageModel
{
    private readonly UserManager<Models.ApplicationUser> userManager;

    public PersonalDataModel(
        UserManager<Models.ApplicationUser> userManager)
    {
        this.userManager = userManager;
    }

    public async Task<IActionResult> OnGet()
    {
        Models.ApplicationUser? user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        return Page();
    }
}
