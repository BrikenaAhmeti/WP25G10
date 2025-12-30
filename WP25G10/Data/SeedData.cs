using Microsoft.AspNetCore.Identity;

namespace WP25G10.Data
{
    public class SeedData
    {
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<IdentityUser> _userManager;

        public SeedData(RoleManager<IdentityRole> roleManager, UserManager<IdentityUser> userManager)
        {
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public async Task SeedAsync()
        {
            var roles = new[] { "Admin", "Staff" };
            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            var adminEmail = "admin@airport.local";
            var admin = await _userManager.FindByEmailAsync(adminEmail);
            if (admin == null)
            {
                admin = new IdentityUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };
                await _userManager.CreateAsync(admin, "Admin123!");
                await _userManager.AddToRoleAsync(admin, "Admin");
            }
        }
    }
}
