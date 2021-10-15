using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Encodings.Web;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;

namespace TASagentTwitchBot.Core.WebServer.Areas.Identity.Pages.Account.Manage
{
    public partial class TokenModel : PageModel
    {
        private readonly UserManager<Models.ApplicationUser> userManager;
        private readonly SignInManager<Models.ApplicationUser> signInManager;
        private readonly ILogger<DownloadPersonalDataModel> logger;
        private readonly IHubContext<Web.Hubs.BotHub> botHub;

        public TokenModel(
            UserManager<Models.ApplicationUser> userManager,
            SignInManager<Models.ApplicationUser> signInManager,
            ILogger<DownloadPersonalDataModel> logger,
            IHubContext<Web.Hubs.BotHub> botHub)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.logger = logger;
            this.botHub = botHub;
        }

        public string Username { get; set; }

        [Display(Name = "Bot Token")]
        public string BotToken { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        private async Task LoadAsync(Models.ApplicationUser user)
        {
            string botToken = await userManager.GetAuthenticationTokenAsync(user, "Self", "BotToken");
            if (string.IsNullOrEmpty(botToken))
            {
                botToken = "None";
            }

            BotToken = botToken;
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

        public async Task<IActionResult> OnPostRegenerateBotTokenAsync()
        {
            Models.ApplicationUser user = await userManager.GetUserAsync(User);
            if (user == null)
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
}
