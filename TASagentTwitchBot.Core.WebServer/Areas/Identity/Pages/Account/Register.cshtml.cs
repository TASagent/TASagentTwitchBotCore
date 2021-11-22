using System.ComponentModel.DataAnnotations;
using System.Net.Mail;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TASagentTwitchBot.Core.WebServer.Areas.Identity.Pages.Account;

[AllowAnonymous]
public class RegisterModel : PageModel
{
    private readonly SignInManager<Models.ApplicationUser> signInManager;
    private readonly UserManager<Models.ApplicationUser> userManager;
    private readonly ILogger<RegisterModel> logger;

    [BindProperty]
    public InputModel Input { get; set; } = null!;

    public string? ReturnUrl { get; set; }
    public IList<AuthenticationScheme>? ExternalLogins { get; set; }

    public RegisterModel(
        UserManager<Models.ApplicationUser> userManager,
        SignInManager<Models.ApplicationUser> signInManager,
        ILogger<RegisterModel> logger)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.logger = logger;
    }

    public class InputModel
    {
        [Required]
        [Display(Name = "Twitch Broadcaster Name")]
        public string TwitchBroadcasterName { get; set; } = null!;

        [Required]
        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; } = null!;

        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = null!;

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; } = null!;
    }

    public async Task OnGetAsync(string? returnUrl = null)
    {
        ReturnUrl = returnUrl;
        ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
    }

    public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
    {
        returnUrl ??= Url.Content("~/");
        ExternalLogins = (await signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

        MailAddress m = new MailAddress(Input.Email);

        if (ModelState.IsValid)
        {
            Models.ApplicationUser user = new Models.ApplicationUser
            {
                TwitchBroadcasterName = Input.TwitchBroadcasterName,
                UserName = m.User,
                Email = Input.Email
            };

            IdentityResult result = await userManager.CreateAsync(user, Input.Password);
            if (result.Succeeded)
            {
                logger.LogInformation("User created a new account with password.");
                await userManager.AddToRoleAsync(user, Enums.Roles.Basic.ToString());

                //Set BotToken
                await userManager.SetAuthenticationTokenAsync(user, "Self", "BotToken", Guid.NewGuid().ToString());
                await signInManager.SignInAsync(user, isPersistent: false);
                return LocalRedirect(returnUrl);
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        // If we got this far, something failed, redisplay form
        return Page();
    }
}
