using Microsoft.AspNetCore.Identity;
using WebApp.Models;

namespace WebApp.Data
{
public class SeedData
    {
        public static async Task SeedRolesAndUsers(UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            // Rolleri oluştur
            string[] roleNames = { "Admin", "User" };

            foreach (var roleName in roleNames)
            {
                var roleExist = await roleManager.RoleExistsAsync(roleName);
                if (!roleExist)
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName)); // Role ekleme işlemi
                }
            }

        }
    }

}
