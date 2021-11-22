using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

using TASagentTwitchBot.Core.WebServer.Models;

namespace TASagentTwitchBot.Core.WebServer.Controllers;

[Authorize(Roles = "Admin")]
public class UserRolesController : Controller
{
    private readonly UserManager<ApplicationUser> userManager;
    private readonly RoleManager<IdentityRole> roleManager;
    public UserRolesController(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        this.roleManager = roleManager;
        this.userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        List<ApplicationUser> users = await userManager.Users.ToListAsync();
        List<UserRolesViewModel> userRolesViewModel = new List<UserRolesViewModel>();

        foreach (ApplicationUser user in users)
        {
            userRolesViewModel.Add(new UserRolesViewModel()
            {
                UserId = user.Id,
                Email = user.Email,
                TwitchBroadcasterId = user.TwitchBroadcasterId,
                TwitchBroadcasterName = user.TwitchBroadcasterName,
                MonthlyTTSCharacterLimit = user.MonthlyTTSLimit,
                MonthlyTTSCharactersUsed = user.MonthlyTTSUsage,
                Roles = await GetUserRoles(user)
            });
        }

        return View(userRolesViewModel);
    }

    private async Task<List<string>> GetUserRoles(ApplicationUser user)
    {
        return new List<string>(await userManager.GetRolesAsync(user));
    }

    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Manage(string userId)
    {
        ViewBag.userId = userId;
        ApplicationUser user = await userManager.FindByIdAsync(userId);

        if (user is null)
        {
            ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
            return View("NotFound");
        }

        ViewBag.UserName = user.UserName;
        List<ManageUserRolesViewModel> model = new List<ManageUserRolesViewModel>();
        foreach (IdentityRole role in roleManager.Roles)
        {
            ManageUserRolesViewModel userRolesViewModel = new ManageUserRolesViewModel
            {
                RoleId = role.Id,
                RoleName = role.Name
            };

            if (await userManager.IsInRoleAsync(user, role.Name))
            {
                userRolesViewModel.Selected = true;
            }
            else
            {
                userRolesViewModel.Selected = false;
            }

            model.Add(userRolesViewModel);
        }

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> Manage(List<ManageUserRolesViewModel> model, string userId)
    {
        ApplicationUser user = await userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return View();
        }

        IList<string> roles = await userManager.GetRolesAsync(user);
        IdentityResult result = await userManager.RemoveFromRolesAsync(user, roles);

        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Cannot remove user existing roles");
            return View(model);
        }

        result = await userManager.AddToRolesAsync(user, model.Where(x => x.Selected).Select(y => y.RoleName));
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Cannot add selected roles to user");
            return View(model);
        }

        return RedirectToAction("Index");
    }


    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ManageTTS(string userId)
    {
        ViewBag.userId = userId;
        ApplicationUser user = await userManager.FindByIdAsync(userId);

        if (user is null)
        {
            ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
            return View("NotFound");
        }

        ViewBag.UserName = user.UserName;
        ManageUserTTSViewModel model = new ManageUserTTSViewModel
        {
            MonthlyTTSCharacterLimit = user.MonthlyTTSLimit,
            MonthlyTTSCharactersUsed = user.MonthlyTTSUsage
        };

        return View(model);
    }

    [HttpPost]
    [Authorize(Roles = "SuperAdmin")]
    public async Task<IActionResult> ManageTTS(ManageUserTTSViewModel model, string userId)
    {
        ApplicationUser user = await userManager.FindByIdAsync(userId);

        if (user is null)
        {
            return View();
        }

        if (user.MonthlyTTSUsage != model.MonthlyTTSCharactersUsed || user.MonthlyTTSLimit != model.MonthlyTTSCharacterLimit)
        {
            user.MonthlyTTSUsage = model.MonthlyTTSCharactersUsed;
            user.MonthlyTTSLimit = model.MonthlyTTSCharacterLimit;

            IdentityResult result = await userManager.UpdateAsync(user);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot update user TTS Usage");
                return View(model);
            }
        }

        return RedirectToAction("Index");
    }
}
