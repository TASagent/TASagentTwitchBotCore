using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;

using TASagentTwitchBot.Core.WebServer.Models;

namespace TASagentTwitchBot.Core.WebServer.Database
{
    public static class ContextSeed
    {
        public static async Task SeedRolesAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            //Seed Roles
            for (Enums.Roles role = 0; role < Enums.Roles.MAX; role++)
            {
                if (!await roleManager.RoleExistsAsync(role.ToString()))
                {
                    await roleManager.CreateAsync(new IdentityRole(role.ToString()));
                }
            }
        }

        public static async Task SeedSuperAdminAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            //Check for default admin
            ApplicationUser admin = await userManager.FindByNameAsync("admin");

            if (admin is null)
            {
                admin = new ApplicationUser
                {
                    UserName = "admin",
                    Email = "admin@dev.null",
                    EmailConfirmed = true
                };

                await userManager.CreateAsync(admin, "DefaultPassword123.$");
                await userManager.AddToRoleAsync(admin, Enums.Roles.Basic.ToString());
                await userManager.AddToRoleAsync(admin, Enums.Roles.Moderator.ToString());
                await userManager.AddToRoleAsync(admin, Enums.Roles.Admin.ToString());
                await userManager.AddToRoleAsync(admin, Enums.Roles.SuperAdmin.ToString());

                await userManager.SetAuthenticationTokenAsync(admin, "Self", "BotToken", Guid.NewGuid().ToString());
            }
        }
    }
}
