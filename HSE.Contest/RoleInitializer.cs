using HSE.Contest.ClassLibrary.DbClasses.Administration;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace HSE.Contest
{
    public class RoleInitializer
    {
        public static async Task InitializeAsync(UserManager<User> userManager, RoleManager<IdentityRole> roleManager)
        {          
            if (await roleManager.FindByNameAsync("admin") is null)
            {
                await roleManager.CreateAsync(new IdentityRole("admin"));
            }
            if (await roleManager.FindByNameAsync("student") is null)
            {
                await roleManager.CreateAsync(new IdentityRole("student"));
            }
            if (await roleManager.FindByNameAsync("professor") is null)
            {
                await roleManager.CreateAsync(new IdentityRole("professor"));
            }
            var admins = await userManager.GetUsersInRoleAsync("admin");
            if (admins is null || admins.Count == 0)
            {
                string adminEmail = "admin";
                string password = "admin";
                User admin = new User { Email = adminEmail, UserName = adminEmail };
                IdentityResult result = await userManager.CreateAsync(admin, password);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(admin, "admin");
                    await userManager.AddToRoleAsync(admin, "professor");
                    await userManager.AddToRoleAsync(admin, "student");
                }
            }
        }
    }
}
