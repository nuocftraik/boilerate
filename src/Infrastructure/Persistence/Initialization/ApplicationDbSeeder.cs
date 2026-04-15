using System.Reflection;
using Boilerate.Application.Common.Interfaces;
using Boilerate.Domain.Identity;
using Boilerate.Infrastructure.Persistence.Context;
using Boilerate.Shared.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Boilerate.Infrastructure.Persistence.Initialization;

internal class ApplicationDbSeeder : ITransientService
{
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly CustomSeederRunner _seederRunner;
    private readonly ILogger<ApplicationDbSeeder> _logger;

    public ApplicationDbSeeder(
        RoleManager<ApplicationRole> roleManager,
        UserManager<ApplicationUser> userManager,
        CustomSeederRunner seederRunner,
        ILogger<ApplicationDbSeeder> logger)
    {
        _roleManager = roleManager;
        _userManager = userManager;
        _seederRunner = seederRunner;
        _logger = logger;
    }

    public async Task SeedDatabaseAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken)
    {
        await SeedActionsAndFunctionsAsync(dbContext);
        await SeedRolesAsync(dbContext);
        await SeedAdminUserAsync();
        await _seederRunner.RunSeedersAsync(cancellationToken);
    }

    private async Task SeedActionsAndFunctionsAsync(ApplicationDbContext dbContext)
    {
        // 1. Reflect và Seed Actions từ AppAction constants
        var actions = typeof(AppAction)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly)
            .Select(field => field.GetValue(null)?.ToString())
            .Where(value => value != null)
            .ToList();

        foreach (var action in actions)
        {
            if (!await dbContext.Actions.AnyAsync(x => x.Name == action))
            {
                _logger.LogInformation("Seeding action {Action}.", action);
                dbContext.Actions.Add(new Domain.Identity.Action { Name = action! });
                await dbContext.SaveChangesAsync();
            }
        }

        // 2. Reflect và Seed Functions từ AppFunction constants
        var functions = typeof(AppFunction)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly)
            .Select(field => field.GetValue(null)?.ToString())
            .Where(value => value != null)
            .ToList();

        foreach (var functionName in functions)
        {
            if (!await dbContext.Functions.AnyAsync(f => f.Name == functionName))
            {
                _logger.LogInformation("Seeding function {Function}.", functionName);
                dbContext.Functions.Add(new Function { Name = functionName! });
                await dbContext.SaveChangesAsync();
            }
        }

        // 3. Map cross: mỗi Function gắn với tất cả Actions
        foreach (var functionName in functions)
        {
            var function = await dbContext.Functions.SingleAsync(f => f.Name == functionName);
            foreach (var actionName in actions)
            {
                var action = await dbContext.Actions.SingleAsync(a => a.Name == actionName);
                if (!await dbContext.ActionInFunctions.AnyAsync(aif => aif.FunctionId == function.Id && aif.ActionId == action.Id))
                {
                    _logger.LogInformation("Seeding action {Action} in function {Function}.", actionName, functionName);
                    dbContext.ActionInFunctions.Add(new ActionInFunction(action.Id, function.Id));
                    await dbContext.SaveChangesAsync();
                }
            }
        }
    }

    private async Task SeedRolesAsync(ApplicationDbContext dbContext)
    {
        foreach (string roleName in AppRoles.DefaultRoles)
        {
            if (await _roleManager.Roles.SingleOrDefaultAsync(r => r.Name == roleName)
                is not ApplicationRole role)
            {
                _logger.LogInformation("Seeding {Role} Role for system.", roleName);
                role = new ApplicationRole(roleName, $"{roleName} Role");
                await _roleManager.CreateAsync(role);
            }

            if (roleName == AppRoles.Basic)
            {
                await AssignPermissionsToRoleAsync(dbContext, role, isBasic: true);
            }
            else if (roleName == AppRoles.Admin)
            {
                await AssignPermissionsToRoleAsync(dbContext, role, isBasic: false);
            }
        }
    }

    private async Task AssignPermissionsToRoleAsync(ApplicationDbContext dbContext, ApplicationRole role, bool isBasic)
    {
        var currentPermissions = await dbContext.Permissions
            .Where(x => x.RoleId == role.Id)
            .ToListAsync();
        var functions = await dbContext.Functions.ToListAsync();

        foreach (var function in functions)
        {
            var actionsInFunction = await dbContext.ActionInFunctions
                .Include(x => x.Action)
                .Where(a => a.FunctionId == function.Id)
                .ToListAsync();

            foreach (var actionInFunction in actionsInFunction)
            {
                // Basic role chỉ có permission xem/tìm kiếm một số module cơ bản
                if (isBasic && !IsBasicPermission(actionInFunction.Action.Name, function.Name))
                {
                    continue;
                }

                string permissionName = $"{function.Name}.{actionInFunction.Action.Name}";
                if (!currentPermissions.Any(p => p.FunctionId == function.Id && p.ActionId == actionInFunction.ActionId))
                {
                    _logger.LogInformation("Seeding {Role} Permission '{Permission}'.", role.Name, permissionName);
                    dbContext.Permissions.Add(new Permission(role.Id, function.Id, actionInFunction.ActionId));
                }
            }
        }

        await dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Xác định danh sách quyền cơ bản cho Basic role.
    /// Chỉ được xem Dashboard, Categories và Products.
    /// </summary>
    private static bool IsBasicPermission(string actionName, string functionName)
    {
        var basicPermissions = new List<string>
        {
            $"{AppAction.View}.{AppFunction.Dashboard}",
            $"{AppAction.View}.{AppFunction.Categories}",
            $"{AppAction.Search}.{AppFunction.Categories}",
            $"{AppAction.View}.{AppFunction.Products}",
            $"{AppAction.Search}.{AppFunction.Products}",
        };

        return basicPermissions.Contains($"{actionName}.{functionName}");
    }

    private async Task SeedAdminUserAsync()
    {
        if (await _userManager.Users.FirstOrDefaultAsync(u => u.Email == "admin@gmail.com")
            is not ApplicationUser adminUser)
        {
            string adminUserName = $"system.{AppRoles.Admin}".ToLowerInvariant();
            adminUser = new ApplicationUser
            {
                FirstName = "Admin",
                LastName = AppRoles.Admin,
                Email = "admin@gmail.com",
                UserName = adminUserName,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                NormalizedEmail = "ADMIN@GMAIL.COM",
                NormalizedUserName = adminUserName.ToUpperInvariant(),
                IsActive = true,
            };

            _logger.LogInformation("Seeding Default Admin User for application");
            var password = new PasswordHasher<ApplicationUser>();
            adminUser.PasswordHash = password.HashPassword(adminUser, "Abcd@1234");
            await _userManager.CreateAsync(adminUser);
        }

        if (!await _userManager.IsInRoleAsync(adminUser, AppRoles.Admin))
        {
            _logger.LogInformation("Assigning Admin Role to Admin User");
            await _userManager.AddToRoleAsync(adminUser, AppRoles.Admin);
        }
    }
}
