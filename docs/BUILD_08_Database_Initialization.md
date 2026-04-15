# BUILD_08 - Database Initialization

> 📚 [Quay lại Mục lục](BUILD_INDEX.md)  
> 📋 **Prerequisites:** BUILD_07 (Logging Setup) đã hoàn thành  

Tài liệu này hướng dẫn setup database initialization, migrations tự động, và seeding dữ liệu nền tảng.

---

## 1. Overview

**Làm gì:** Tự động tạo và migrate database, tạo dữ liệu gốc khi ứng dụng khởi động.

**Tại sao cần:**
- **Auto-migration:** Database schema luôn sync với trạng thái entity code ngay khi chạy ứng dụng.
- **Dữ liệu nền tảng (Seeding):** Có sẵn Admin, Roles, Actions và Functions phục vụ hệ thống phân quyền (Authorization) từ cơ sở.
- **Custom Seeding:** Cung cấp pattern `ICustomSeeder` để cắm thêm data mẫu định kỳ cho từng module (Notification, Identity, Catalog...).

**Kiến trúc:**
`Host Program.cs` -> `IDatabaseInitializer` -> `DatabaseInitializer` -> `ApplicationDbInitializer` -> `ApplicationDbSeeder` -> `CustomSeederRunner` -> Tìm các `ICustomSeeder` con.

---

## 2. Domain Entities cho Permission System

Trước khi setup database initialization, cần tạo các entity hỗ trợ hệ thống phân quyền.

### Bước 2.1: Action Entity

**File:** `src/Domain/Identity/Action.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace {ProjectName}.Domain.Identity;

/// <summary>
/// Đại diện một hành động trong hệ thống phân quyền (View, Create, Update, Delete...).
/// Dữ liệu được seed tự động từ Shared/Authorization/AppAction constants.
/// </summary>
public class Action
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = default!;
}
```

### Bước 2.2: Function Entity

**File:** `src/Domain/Identity/Function.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace {ProjectName}.Domain.Identity;

/// <summary>
/// Đại diện một module/feature trong hệ thống phân quyền (Dashboard, Users, Roles...).
/// Dữ liệu được seed tự động từ Shared/Authorization/AppFunction constants.
/// </summary>
public class Function
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = default!;
}
```

### Bước 2.3: ActionInFunction Entity (Junction Table)

**File:** `src/Domain/Identity/ActionInFunction.cs`

```csharp
namespace {ProjectName}.Domain.Identity;

/// <summary>
/// Bảng giao (junction) giữa Action và Function.
/// Mỗi Function có thể chứa nhiều Actions (VD: Users module có View, Create, Update, Delete).
/// Sử dụng composite key (ActionId, FunctionId).
/// </summary>
public class ActionInFunction
{
    public Guid ActionId { get; set; }
    public Guid FunctionId { get; set; }

    public virtual Action Action { get; set; } = default!;
    public virtual Function Function { get; set; } = default!;

    public ActionInFunction() { }

    public ActionInFunction(Guid actionId, Guid functionId)
    {
        ActionId = actionId;
        FunctionId = functionId;
    }
}
```

### Bước 2.4: Permission Entity

**File:** `src/Domain/Identity/Permission.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace {ProjectName}.Domain.Identity;

/// <summary>
/// Phân quyền: Gắn một Action trong một Function cho một Role cụ thể.
/// VD: Role "Admin" có quyền "Delete" trong module "Users" -> Permission(AdminRoleId, UsersId, DeleteId).
/// </summary>
public class Permission
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string RoleId { get; set; } = default!;
    public Guid FunctionId { get; set; }
    public Guid ActionId { get; set; }

    public virtual ApplicationRole Role { get; set; } = default!;
    public virtual Function Function { get; set; } = default!;
    public virtual Action Action { get; set; } = default!;

    public Permission() { }

    public Permission(string roleId, Guid functionId, Guid actionId)
    {
        RoleId = roleId;
        FunctionId = functionId;
        ActionId = actionId;
    }
}
```

---

## 3. EF Core Configuration cho Permission Entities

Thêm cấu hình Fluent API vào file Identity configuration hiện tại.

**File:** `src/Infrastructure/Persistence/Configuration/Identity.cs` (thêm vào cuối file)

```csharp
public class ActionConfig : IEntityTypeConfiguration<Domain.Identity.Action>
{
    public void Configure(EntityTypeBuilder<Domain.Identity.Action> builder)
    {
        builder.ToTable("Actions", SchemaNames.Identity);
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(a => a.Name).IsUnique();
    }
}

public class FunctionConfig : IEntityTypeConfiguration<Function>
{
    public void Configure(EntityTypeBuilder<Function> builder)
    {
        builder.ToTable("Functions", SchemaNames.Identity);
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Name).IsRequired().HasMaxLength(100);
        builder.HasIndex(f => f.Name).IsUnique();
    }
}

public class ActionInFunctionConfig : IEntityTypeConfiguration<ActionInFunction>
{
    public void Configure(EntityTypeBuilder<ActionInFunction> builder)
    {
        builder.ToTable("ActionInFunctions", SchemaNames.Identity);

        // Composite key: mỗi cặp (Action, Function) chỉ xuất hiện duy nhất 1 lần
        builder.HasKey(aif => new { aif.ActionId, aif.FunctionId });

        builder.HasOne(aif => aif.Action)
            .WithMany()
            .HasForeignKey(aif => aif.ActionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(aif => aif.Function)
            .WithMany()
            .HasForeignKey(aif => aif.FunctionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PermissionConfig : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions", SchemaNames.Identity);
        builder.HasKey(p => p.Id);

        // Tránh duplicate: một Role chỉ có 1 permission cho 1 cặp (Function, Action)
        builder.HasIndex(p => new { p.RoleId, p.FunctionId, p.ActionId }).IsUnique();

        builder.HasOne(p => p.Role)
            .WithMany()
            .HasForeignKey(p => p.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(p => p.Function)
            .WithMany()
            .HasForeignKey(p => p.FunctionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Action)
            .WithMany()
            .HasForeignKey(p => p.ActionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### Bước 3.2: Update ApplicationDbContext

Thêm DbSet cho các permission entities.

**File:** `src/Infrastructure/Persistence/Context/ApplicationDbContext.cs`

```csharp
using {ProjectName}.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace {ProjectName}.Infrastructure.Persistence.Context;

public class ApplicationDbContext : BaseDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Permission system DbSets
    public DbSet<Domain.Identity.Action> Actions => Set<Domain.Identity.Action>();
    public DbSet<Function> Functions => Set<Function>();
    public DbSet<ActionInFunction> ActionInFunctions => Set<ActionInFunction>();
    public DbSet<Permission> Permissions => Set<Permission>();
}
```

> **Lưu ý:** Dùng `Domain.Identity.Action` (fully qualified) để tránh xung đột với `System.Action`.

---

## 4. Setup Migrators Project

Bóc tách riêng project để chuyên chứa Migration code, tách biệt ra khỏi Infrastructure.

### Bước 4.1: Cấu hình Migrators.MSSQL

**File:** `src/Migrators.MSSQL/Migrators.MSSQL.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>{ProjectName}.Migrators.MSSQL</RootNamespace>
    <AssemblyName>{ProjectName}.Migrators.MSSQL</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />
  </ItemGroup>

</Project>
```

> **Quan trọng:** `AssemblyName` phải khớp với giá trị trong `MigrationsAssembly()` ở Persistence Startup.

### Bước 4.2: Update Host.csproj

Thêm reference tới Migrators và EF Core Design package (cần cho `dotnet ef` CLI).

**File:** `src/Host/Host.csproj` (thêm vào ItemGroup)

```xml
<ItemGroup>
  <ProjectReference Include="..\Migrators.MSSQL\Migrators.MSSQL.csproj" />
</ItemGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.*">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

---

## 5. Interfaces & Runners

Sử dụng Pattern Runner để tự động đăng ký và chạy mọi lớp Seeder nằm rải rác mà không cần hard-code liên tục vào `DatabaseInitializer`.

### Bước 5.1: Giao diện `IDatabaseInitializer` và `ICustomSeeder`

**File:** `src/Infrastructure/Persistence/Initialization/IDatabaseInitializer.cs`

```csharp
namespace {ProjectName}.Infrastructure.Persistence.Initialization;

internal interface IDatabaseInitializer
{
    Task InitializeDatabasesAsync(CancellationToken cancellationToken);
}
```

**File:** `src/Infrastructure/Persistence/Initialization/ICustomSeeder.cs`

```csharp
namespace {ProjectName}.Infrastructure.Persistence.Initialization;

/// <summary>
/// Marker interface cho các custom seeder.
/// Mọi class implement ICustomSeeder sẽ được tự động phát hiện qua DI và chạy sau khi seed dữ liệu nền tảng.
/// </summary>
public interface ICustomSeeder
{
    Task InitializeAsync(CancellationToken cancellationToken);
}
```

### Bước 5.2: CustomSeederRunner

Chạy tuần tự toàn bộ các custom seeder trong hệ thống được đăng ký thông qua cơ chế Dependency Injection.

**File:** `src/Infrastructure/Persistence/Initialization/CustomSeederRunner.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace {ProjectName}.Infrastructure.Persistence.Initialization;

internal class CustomSeederRunner
{
    private readonly ICustomSeeder[] _seeders;

    public CustomSeederRunner(IServiceProvider serviceProvider) =>
        _seeders = serviceProvider.GetServices<ICustomSeeder>().ToArray();

    public async Task RunSeedersAsync(CancellationToken cancellationToken)
    {
        foreach (var seeder in _seeders)
        {
            await seeder.InitializeAsync(cancellationToken);
        }
    }
}
```

---

## 6. Main Seeders & Initializers (Tầng Infrastructure)

### Bước 6.1: DatabaseInitializer

Entry point của pipeline. Tạo scope biệt lập để resolve các service Scoped (DbContext, UserManager, RoleManager).

**File:** `src/Infrastructure/Persistence/Initialization/DatabaseInitializer.cs`

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace {ProjectName}.Infrastructure.Persistence.Initialization;

internal class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializer> _logger;

    public DatabaseInitializer(IServiceProvider serviceProvider, ILogger<DatabaseInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task InitializeDatabasesAsync(CancellationToken cancellationToken)
    {
        // Tạo scope biệt lập để resolve các service Scoped (DbContext, Identity managers)
        using var scope = _serviceProvider.CreateScope();

        await scope.ServiceProvider.GetRequiredService<ApplicationDbInitializer>()
            .InitializeAsync(cancellationToken);
    }
}
```

### Bước 6.2: ApplicationDbInitializer

Apply pending migrations và gọi seeder.

**File:** `src/Infrastructure/Persistence/Initialization/ApplicationDbInitializer.cs`

```csharp
using {ProjectName}.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace {ProjectName}.Infrastructure.Persistence.Initialization;

internal class ApplicationDbInitializer
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ApplicationDbInitializer> _logger;
    private readonly ApplicationDbSeeder _dbSeeder;

    public ApplicationDbInitializer(
        ApplicationDbContext dbContext,
        ILogger<ApplicationDbInitializer> logger,
        ApplicationDbSeeder dbSeeder)
    {
        _dbContext = dbContext;
        _logger = logger;
        _dbSeeder = dbSeeder;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (_dbContext.Database.GetMigrations().Any())
        {
            if ((await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
            {
                _logger.LogInformation("Applying Migrations for system");
                await _dbContext.Database.MigrateAsync(cancellationToken);
            }

            if (await _dbContext.Database.CanConnectAsync(cancellationToken))
            {
                _logger.LogInformation("Connection to system's Database Succeeded.");
                await _dbSeeder.SeedDatabaseAsync(_dbContext, cancellationToken);
            }
        }
    }
}
```

### Bước 6.3: ApplicationDbSeeder

Xử lý seed Action, Function, phân bố quyền cho Roles cơ bản (Admin / Basic) và tạo tài khoản Admin Default.

**File:** `src/Infrastructure/Persistence/Initialization/ApplicationDbSeeder.cs`

```csharp
using System.Reflection;
using {ProjectName}.Domain.Identity;
using {ProjectName}.Infrastructure.Persistence.Context;
using {ProjectName}.Shared.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace {ProjectName}.Infrastructure.Persistence.Initialization;

internal class ApplicationDbSeeder
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
```

---

## 7. Register Services & Tích hợp

### Bước 7.1: Đăng ký DI trong Persistence Startup

**File:** `src/Infrastructure/Persistence/Startup.cs`

```csharp
using {ProjectName}.Domain.Identity;
using {ProjectName}.Infrastructure.Persistence.Context;
using {ProjectName}.Infrastructure.Persistence.Initialization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Serilog;

namespace {ProjectName}.Infrastructure.Persistence;

internal static class Startup
{
    private static readonly ILogger _logger = Log.ForContext(typeof(Startup));

    internal static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        services.AddOptions<DatabaseSettings>()
            .BindConfiguration(nameof(DatabaseSettings))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        return services
            .AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                var dbSettings = sp.GetRequiredService<IOptions<DatabaseSettings>>().Value;
                _logger.Information("DB Provider: {provider}", dbSettings.DBProvider);
                options.UseDatabase(dbSettings.DBProvider, dbSettings.ConnectionString);
            })

            // Identity setup
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders()
            .Services

            // Database initialization pipeline
            .AddTransient<IDatabaseInitializer, DatabaseInitializer>()
            .AddTransient<ApplicationDbInitializer>()
            .AddTransient<ApplicationDbSeeder>()
            .AddTransient<CustomSeederRunner>();
    }

    internal static DbContextOptionsBuilder UseDatabase(
        this DbContextOptionsBuilder builder,
        string provider,
        string connectionString)
    {
        return provider.ToLowerInvariant() switch
        {
            "mssql" => builder.UseSqlServer(connectionString,
                e => e.MigrationsAssembly("{ProjectName}.Migrators.MSSQL")),
            _ => throw new InvalidOperationException($"DB Provider '{provider}' not supported")
        };
    }
}
```

> **Lưu ý:** `MigrationsAssembly` phải khớp với `AssemblyName` trong Migrators.MSSQL.csproj.

### Bước 7.2: Thêm InitializeDatabasesAsync vào Infrastructure Startup

**File:** `src/Infrastructure/Startup.cs`

```csharp
using {ProjectName}.Infrastructure.Persistence;
using {ProjectName}.Infrastructure.Persistence.Initialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {ProjectName}.Infrastructure;

public static class Startup
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration config)
    {
        return services
            .AddPersistence()
            .AddRouting(options => options.LowercaseUrls = true);
    }

    public static IApplicationBuilder UseInfrastructure(
        this IApplicationBuilder builder,
        IConfiguration config)
    {
        return builder
            .UseRouting()
            .UseAuthentication()
            .UseAuthorization();
    }

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapControllers();
        return builder;
    }

    /// <summary>
    /// Khởi tạo Database: apply migrations và seed dữ liệu nền tảng.
    /// Gọi từ Program.cs sau khi build app.
    /// </summary>
    public static async Task InitializeDatabasesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();

        await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>()
            .InitializeDatabasesAsync(cancellationToken);
    }
}
```

### Bước 7.3: Tích hợp vào Program.cs

**File:** `src/Host/Program.cs`

```csharp
var app = builder.Build();

// ... middleware setup ...

app.UseInfrastructure(builder.Configuration);
app.MapEndpoints();

// Khởi tạo Database: apply migrations + seed data
await app.Services.InitializeDatabasesAsync();

Log.Information("Application Starting...");
await app.RunAsync();
```

---

## 8. How to run Entity Framework Migration

Sử dụng trực tiếp Host project cho design time. Database kết nối dựa trên `Configurations/database.json`.

**Bước 1: Tạo Migration (từ thư mục root của solution):**

```powershell
dotnet ef migrations add "InitialCreate" --project src/Migrators.MSSQL/Migrators.MSSQL.csproj --startup-project src/Host/Host.csproj --output-dir Migrations/Application
```

**Bước 2: Chạy app để tự động apply migration và seed data:**

```powershell
dotnet run --project src/Host/Host.csproj
```

> **Lưu ý:** App sẽ tự động detect pending migrations và apply chúng khi khởi động.
> Không cần chạy `dotnet ef database update` thủ công — `ApplicationDbInitializer` đã xử lý.

---

## 9. Summary

### ✅ Đã hoàn thành:
- Tạo 4 Domain entities cho Permission system (Action, Function, ActionInFunction, Permission)
- Cấu hình EF Core Fluent API với composite keys và unique indexes
- Set up tầng mồi cơ sở dữ liệu với `ApplicationDbInitializer` và `ApplicationDbSeeder`
- Đăng ký ASP.NET Core Identity (UserManager, RoleManager)
- Áp dụng pattern `ICustomSeeder` hỗ trợ DI, linh hoạt mở rộng Seeding logic
- Chuyển kiến trúc Design Migration sang Host project

### 📁 File Structure:
```text
src/
├── Domain/
│   └── Identity/
│       ├── Action.cs                « Hành động (View, Create, Update...)
│       ├── Function.cs              « Module (Dashboard, Users, Roles...)
│       ├── ActionInFunction.cs      « Junction: Action <-> Function
│       └── Permission.cs            « Phân quyền: Role + Function + Action
├── Migrators.MSSQL/
│   ├── Migrators.MSSQL.csproj
│   └── Migrations/Application/     « Chỉ chứa các file Generated Migrations
├── Infrastructure/
│   ├── Persistence/
│   │   ├── Configuration/
│   │   │   └── Identity.cs          « EF config cho permission entities
│   │   ├── Context/
│   │   │   └── ApplicationDbContext.cs  « Thêm DbSet cho permission entities
│   │   ├── Initialization/
│   │   │   ├── IDatabaseInitializer.cs
│   │   │   ├── ICustomSeeder.cs
│   │   │   ├── CustomSeederRunner.cs
│   │   │   ├── DatabaseInitializer.cs
│   │   │   ├── ApplicationDbInitializer.cs
│   │   │   └── ApplicationDbSeeder.cs
│   │   └── Startup.cs              « DI registration (Identity + Init pipeline)
│   └── Startup.cs                   « InitializeDatabasesAsync extension
└── Host/
    ├── Host.csproj                  « Reference Migrators + EF Design package
    └── Program.cs                   « Gọi InitializeDatabasesAsync()
```

### 🔑 Default Admin Account:
- **Email:** admin@gmail.com
- **Password:** Abcd@1234
- **Role:** Admin (full permissions)

---

## 10. Next Steps

**Tiếp theo:** [BUILD_09 - Domain Base Entities](BUILD_09_Domain_Base_Entities.md)

---

**Quay lại:** [Mục lục](BUILD_INDEX.md)
