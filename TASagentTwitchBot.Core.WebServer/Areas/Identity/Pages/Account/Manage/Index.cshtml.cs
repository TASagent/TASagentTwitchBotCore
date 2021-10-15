using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TASagentTwitchBot.Core.WebServer.Areas.Identity.Pages.Account.Manage
{
    public partial class IndexModel : PageModel
    {
        private readonly UserManager<Models.ApplicationUser> userManager;
        private readonly SignInManager<Models.ApplicationUser> signInManager;

        public IndexModel(
            UserManager<Models.ApplicationUser> userManager,
            SignInManager<Models.ApplicationUser> signInManager)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Display(Name = "Twitch Broadcaster Name")]
            public string TwitchBroadcasterName { get; set; }
            [Display(Name = "Username")]
            public string Username { get; set; }
        }

        private async Task LoadAsync(Models.ApplicationUser user)
        {
            var userName = await userManager.GetUserNameAsync(user);
            var twitchBroadcasterName = user.TwitchBroadcasterName;
            Username = userName;
            Input = new InputModel
            {
                Username = userName,
                TwitchBroadcasterName = twitchBroadcasterName,
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null)
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
}
