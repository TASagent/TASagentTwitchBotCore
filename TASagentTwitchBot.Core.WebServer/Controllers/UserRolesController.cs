using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

using TASagentTwitchBot.Core.WebServer.Models;

namespace TASagentTwitchBot.Core.WebServer.Controllers
{
    public class UserRolesController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public UserRolesController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            List<ApplicationUser> users = await _userManager.Users.ToListAsync();
            List<UserRolesViewModel> userRolesViewModel = new List<UserRolesViewModel>();

            foreach (ApplicationUser user in users)
            {
                userRolesViewModel.Add(new UserRolesViewModel()
                {
                    UserId = user.Id,
                    Email = user.Email,
                    TwitchBroadcasterId = user.TwitchBroadcasterId,
                    TwitchBroadcasterName = user.TwitchBroadcasterName,
                    Roles = await GetUserRoles(user)
                });
            }

            return View(userRolesViewModel);
        }

        private async Task<List<string>> GetUserRoles(ApplicationUser user)
        {
            return new List<string>(await _userManager.GetRolesAsync(user));
        }

        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> Manage(string userId)
        {
            ViewBag.userId = userId;
            ApplicationUser user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                ViewBag.ErrorMessage = $"User with Id = {userId} cannot be found";
                return View("NotFound");
            }

            ViewBag.UserName = user.UserName;
            List<ManageUserRolesViewModel> model = new List<ManageUserRolesViewModel>();
            foreach (IdentityRole role in _roleManager.Roles)
            {
                ManageUserRolesViewModel userRolesViewModel = new ManageUserRolesViewModel
                {
                    RoleId = role.Id,
                    RoleName = role.Name
                };

                if (await _userManager.IsInRoleAsync(user, role.Name))
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
        public async Task<IActionResult> Manage(List<ManageUserRolesViewModel> model, string userId)
        {
            ApplicationUser user = await _userManager.FindByIdAsync(userId);

            if (user is null)
            {
                return View();
            }

            IList<string> roles = await _userManager.GetRolesAsync(user);
            IdentityResult result = await _userManager.RemoveFromRolesAsync(user, roles);

            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot remove user existing roles");
                return View(model);
            }

            result = await _userManager.AddToRolesAsync(user, model.Where(x => x.Selected).Select(y => y.RoleName));
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Cannot add selected roles to user");
                return View(model);
            }

            return RedirectToAction("Index");
        }
    }
}
