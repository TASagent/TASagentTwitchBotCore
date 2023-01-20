using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TASagentTwitchBot.Core.WebServer.Areas.Identity.Pages.Account.Manage;

public partial class IndexModel : PageModel
{
    private readonly UserManager<Models.ApplicationUser> userManager;
    private readonly SignInManager<Models.ApplicationUser> signInManager;

    public string? Username { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = null!;

    public IndexModel(
        UserManager<Models.ApplicationUser> userManager,
        SignInManager<Models.ApplicationUser> signInManager)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
    }

    public class InputModel
    {
        [Display(Name = "Twitch Broadcaster Name")]
        public string TwitchBroadcasterName { get; set; } = null!;
        [Display(Name = "Username")]
        public string Username { get; set; } = null!;
    }

    private async Task LoadAsync(Models.ApplicationUser user)
    {
        string? userName = await userManager.GetUserNameAsync(user);
        string? twitchBroadcasterName = user.TwitchBroadcasterName;
        Username = userName;
        Input = new InputModel
        {
            Username = userName!,
            TwitchBroadcasterName = twitchBroadcasterName!,
        };
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

    public async Task<IActionResult> OnPostAsync()
    {
        Models.ApplicationUser? user = await userManager.GetUserAsync(User);
        if (user is null)
        {
            return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(user);
            return Page();
        }

        //var twitchBroadcasterName = user.TwitchBroadcasterName;
        //if (Input.TwitchBroadcasterName != twitchBroadcasterName)
        //{
        //    user.TwitchBroadcasterName = Input.TwitchBroadcasterName;
        //    await userManager.UpdateAsync(user);
        //}

        await signInManager.RefreshSignInAsync(user);
        StatusMessage = "Your profile has been updated";
        return RedirectToPage();
    }
}
