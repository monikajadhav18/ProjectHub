using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectHub.Application.Common.Constants;

namespace ProjectHub.Infrastructure.Identity;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeeder");
        var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();

        foreach (var roleName in new[] { Roles.Admin, Roles.Employee })
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var roleResult = await roleManager.CreateAsync(new ApplicationRole { Name = roleName });
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create role '{roleName}': {Describe(roleResult)}");
            }
        }

        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogInformation("SeedAdmin settings were not found. Skipping admin user creation.");
            return;
        }

        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FullName = configuration["SeedAdmin:FullName"] ?? "System Administrator"
        };

        var createResult = await userManager.CreateAsync(admin, password);
        if (!createResult.Succeeded)
        {
            throw new InvalidOperationException($"Could not create the seed admin: {Describe(createResult)}");
        }

        var addToRoleResult = await userManager.AddToRoleAsync(admin, Roles.Admin);
        if (!addToRoleResult.Succeeded)
        {
            throw new InvalidOperationException(
                $"Could not assign the Admin role: {Describe(addToRoleResult)}");
        }

        logger.LogInformation("Seed admin user created.");
    }

    private static string Describe(IdentityResult result)
        => string.Join("; ", result.Errors.Select(e => e.Description));
}