using Microsoft.AspNetCore.Identity;

namespace SmartStudentManagement.Data;

public static class DbSeeder
{
    public static async Task SeedRolesAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        string[] roles = ["Admin", "Teacher", "Student"];

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                    throw new InvalidOperationException(
                        $"Could not create role {role}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }
}
