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
- **Custom Seeding:** Cung cấp pattern `ICustomSeeder` để cắm thêm data mẫu định kỳ cho từng Micro-Service/Sub-module (Notification, Identity, Catalog...).

**Kiến trúc:**
`Host Program.cs` ->  `IDatabaseInitializer` -> `DatabaseInitializer` -> `ApplicationDbInitializer` -> `ApplicationDbSeeder` -> `CustomSeederRunner` -> Tìm các `ICustomSeeder` con.

---

## 2. Setup Migrators Project

Ta bóc tách riêng project để chuyên chứa Migration code, tách biệt ra khỏi Infrastructure.

### Bước 2.1: Tạo project Migrators.MSSQL

**File:** `src/Migrators.MSSQL/Migrators.MSSQL.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

	<PropertyGroup>
		<TargetFramework>net8.0</TargetFramework>
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

### Bước 2.2: Add package cho Host

Đảm bảo project Host chứa package EF Tools để run CLI và được trỏ reference tới project Migration.

**File:** `src/Host/Host.csproj`

```xml
<ItemGroup>
  <ProjectReference Include="..\Migrators.MSSQL\Migrators.MSSQL.csproj" />
</ItemGroup>

<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

---

## 3. Interfaces & Runners

Chúng ta sử dụng Pattern Runner để tự động đăng ký và chạy mọi lớp Seeder nằm rải rác mà không cần hard-code liên tục vào `DatabaseInitializer`.

### Bước 3.1: Giao diện `IDatabaseInitializer` và `ICustomSeeder`

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

public interface ICustomSeeder
{
    Task InitializeAsync(CancellationToken cancellationToken);
}
```

### Bước 3.2: CustomSeederRunner

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

## 4. Main Seeders & Initializers (Tầng Infrastructure)

### Bước 4.1: DatabaseInitializer & ApplicationDbInitializer

Hai lớp này bọc nhau và đứng ra tạo scope biệt lập (nhằm tự fetch các DBContext được Inject Scoped), chạy migrations hoàn toàn tự động, và sau đó triệu gọi class mồi dữ liệu cơ bản.

**File:** `src/Infrastructure/Persistence/Initialization/DatabaseInitializer.cs`

```csharp
using {ProjectName}.Infrastructure.Persistence.Context;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace {ProjectName}.Infrastructure.Persistence.Initialization;

internal class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DatabaseInitializer> _logger;
    private readonly ApplicationDbContext _context;

    public DatabaseInitializer(ApplicationDbContext context, IServiceProvider serviceProvider, ILogger<DatabaseInitializer> logger)
    {
        _context = context;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task InitializeDatabasesAsync(CancellationToken cancellationToken)
    {
        // Khởi tạo cơ sở dữ liệu trong một scope biệt lập
        using var scope = _serviceProvider.CreateScope();
        
        // Gọi Initializer thực sự bên trong scope vừa tạo
        await scope.ServiceProvider.GetRequiredService<ApplicationDbInitializer>()
            .InitializeAsync(cancellationToken);
    }
}
```

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

    public ApplicationDbInitializer(ApplicationDbContext dbContext, ILogger<ApplicationDbInitializer> logger, ApplicationDbSeeder dbSeeder)
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

### Bước 4.2: ApplicationDbSeeder

Xử lý seed Action (Hành động), Function (Bảng Module), phân bố quyền cho Roles cơ bản (Admin / Basic) và tạo tài khoản Admin Default.

**File:** `src/Infrastructure/Persistence/Initialization/ApplicationDbSeeder.cs`
*(File implementation tiêu biểu khá dài)*

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

    public ApplicationDbSeeder(RoleManager<ApplicationRole> roleManager, UserManager<ApplicationUser> userManager, CustomSeederRunner seederRunner, ILogger<ApplicationDbSeeder> logger)
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
        // 1. Phản chiếu và Seed Actions từ Shared/Authorization
        var actions = typeof({ProjectName}Action)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly) 
            .Select(field => field.GetValue(null)?.ToString())
            .Where(value => value != null)
            .ToList();

        foreach (var action in actions)
        {
            if (!await dbContext.Actions.AnyAsync(x => x.Name == action))
            {
                _logger.LogInformation($"Seeding action {action}.");
                dbContext.Actions.Add(new Domain.Identity.Action { Name = action });
                await dbContext.SaveChangesAsync();
            }
        }

        // 2. Phản chiếu Lọc Functions
        var functions = typeof({ProjectName}Function)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
            .Where(field => field.IsLiteral && !field.IsInitOnly) 
            .Select(field => field.GetValue(null)?.ToString())
            .Where(value => value != null) 
            .ToList();

        foreach (var functionName in functions)
        {
            if (!await dbContext.Functions.AnyAsync(f => f.Name == functionName))
            {
                _logger.LogInformation($"Seeding function {functionName}.");
                dbContext.Functions.Add(new Function { Name = functionName });
                await dbContext.SaveChangesAsync();
            }
        }

        // 3. Map cross
        foreach (var functionName in functions)
        {
            var function = await dbContext.Functions.SingleAsync(f => f.Name == functionName);
            foreach (var actionName in actions)
            {
                var action = await dbContext.Actions.SingleAsync(a => a.Name == actionName);
                if (!await dbContext.ActionInFunctions.AnyAsync(aif => aif.FunctionId == function.Id && aif.ActionId == action.Id))
                {
                    _logger.LogInformation($"Seeding action {actionName} in function {functionName}.");
                    dbContext.ActionInFunctions.Add(new ActionInFunction(action.Id,function.Id));
                    await dbContext.SaveChangesAsync();
                }
            }
        }
    }

    private async Task SeedRolesAsync(ApplicationDbContext dbContext)
    {
        foreach (string roleName in {ProjectName}Roles.DefaultRoles)
        {
            if (await _roleManager.Roles.SingleOrDefaultAsync(r => r.Name == roleName) is not ApplicationRole role)
            {
                _logger.LogInformation("Seeding {role} Role for system.", roleName);
                role = new ApplicationRole(roleName, $"{roleName} Role ");
                await _roleManager.CreateAsync(role);
            }

            if (roleName == {ProjectName}Roles.Basic)
                await AssignPermissionsToRoleAsync(dbContext, role ,true);
            else if (roleName == {ProjectName}Roles.Admin)
                await AssignPermissionsToRoleAsync(dbContext, role ,false);
        }
    }

    private async Task AssignPermissionsToRoleAsync(ApplicationDbContext dbContext, ApplicationRole role, bool isBasic)
    {
        var currentPermissions = await dbContext.Permissions.Where(x => x.RoleId == role.Id).ToListAsync();
        var functions = await dbContext.Functions.ToListAsync(); 

        foreach (var function in functions)
        {
            var actionsInFunction = await dbContext.ActionInFunctions.Include(x => x.Action).Where(a => a.FunctionId == function.Id).ToListAsync();
            foreach (var actionInFunction in actionsInFunction)
            {
                if (isBasic && !IsBasicPermission(actionInFunction.Action.Name, function.Name))
                {
                    continue; // Bỏ qua nếu basic không có quyển basic
                }

                var permissionName = $"{function.Name}.{actionInFunction.Action.Name}";
                if (!currentPermissions.Any(p => p.FunctionId == function.Id && p.ActionId == actionInFunction.ActionId))
                {
                    _logger.LogInformation("Seeding {role} Permission '{permissionName}'.", role.Name, permissionName);
                    dbContext.Permissions.Add(new Permission(role.Id,function.Id,actionInFunction.ActionId));
                }
            }
        }
        await dbContext.SaveChangesAsync();
    }

    private bool IsBasicPermission(string actionName, string functionName)
    {
        var basicPermissions = new List<string>
        {   
            $"{ {ProjectName}Action.View}.{ {ProjectName}Function.Dashboard}",
            $"{ {ProjectName}Action.View}.{ {ProjectName}Function.Categories}",
            $"{ {ProjectName}Action.Search}.{ {ProjectName}Function.Categories}",
            $"{ {ProjectName}Action.View}.{ {ProjectName}Function.Products}",
            $"{ {ProjectName}Action.Search}.{ {ProjectName}Function.Products}"
        };

        return basicPermissions.Contains($"{actionName}.{functionName}");
    }

    private async Task SeedAdminUserAsync()
    {
        if (await _userManager.Users.FirstOrDefaultAsync(u => u.Email == "admin@gmail.com") is not ApplicationUser adminUser)
        {
            string adminUserName = $"System.{ {ProjectName}Roles.Admin}".ToLowerInvariant();
            adminUser = new ApplicationUser
            {
                FirstName = "Admin",
                LastName = {ProjectName}Roles.Admin,
                Email = "admin@gmail.com",
                UserName = adminUserName,
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                NormalizedEmail = "ADMIN@GMAIL.COM",
                NormalizedUserName = adminUserName.ToUpperInvariant(),
                IsActive = true
            };

            _logger.LogInformation("Seeding Default Admin User for application");
            var password = new PasswordHasher<ApplicationUser>();
            adminUser.PasswordHash = password.HashPassword(adminUser, "Abcd@1234");
            await _userManager.CreateAsync(adminUser);
        }

        if (!await _userManager.IsInRoleAsync(adminUser, {ProjectName}Roles.Admin))
        {
            _logger.LogInformation("Assigning Admin Role to Admin User");
            await _userManager.AddToRoleAsync(adminUser, {ProjectName}Roles.Admin);
        }
    }
}
```

---

## 5. Register Services & Tích hợp

### Bước 5.1: Đăng ký DI trong Infrastructure

**File:** `src/Infrastructure/Persistence/Startup.cs`

Thêm các file Initialize vào Startup của Persistence logic:

```csharp
    internal static IServiceCollection AddPersistence(this IServiceCollection services)
    {
        // ... (Khai báo config context/database trước đó) ...
        
        return services
            .AddDbContext<ApplicationDbContext>(/* ... */)
            // Khai báo Dependency Injection mồi Start Hệ thống:
            .AddTransient<IDatabaseInitializer, DatabaseInitializer>()
            .AddTransient<ApplicationDbInitializer>()
            .AddTransient<ApplicationDbSeeder>()
            .AddTransient<CustomSeederRunner>()
            // Reflection tự lấy toàn bộ File ICustomSeeder từ assembly và nạp Transient
            .AddServices(typeof(ICustomSeeder), ServiceLifetime.Transient)
            // ... Mấy Interface khác ...
            .AddRepositories();
    }
```

**File:** `src/Infrastructure/Startup.cs`

Tạo Function khởi phát Database riêng biệt chạy bằng CancellationToken:

```csharp
    public static async Task InitializeDatabasesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        
        await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>()
            .InitializeDatabasesAsync(cancellationToken);
    }
```

### Bước 5.2: Phơi bày lên Application Pipeline `Program.cs`

**File:** `src/Host/Program.cs`

```csharp
// Đặt đoạn code này phía dưới khai báo app
var app = builder.Build();

app.UseInfrastructure(builder.Configuration);  // Load middlewares
app.MapEndpoints();

// Khởi chạy Database Setup lúc app run
await app.Services.InitializeDatabasesAsync();

// Bắt đầu nhúng socket listen requests
Log.Information("Application Starting...");
app.Run();
```

---

## 6. How to run Entity Framework Migration

Sử dụng trực tiếp Root Host cho việc design time thay vì design-time factory giả như xưa. Cơ sở dữ liệu sẽ kết nối dựa trên File \`Configurations/database.json\` trong thư mục của Project Host. 

1. Theo Terminal bật ra đường dẫn Context \`Root\` / Hoặc Folder \`src\`.
2. Chạy EF Migration từ Command Line:

```powershell
dotnet ef migrations add "InitialCreate" --project src/Migrators.MSSQL/Migrators.MSSQL.csproj --startup-project src/Host/Host.csproj --output-dir Migrations/Application
```

3. Gõ CMD để kích hoạt tự cập nhật cơ sở dữ liệu trên Server (Migration sẽ apply và Seed user ngay lập tức):
```powershell
dotnet run --project src/Host/Host.csproj
```

---

## 7. Summary

### ✅ Đã hoàn thành:
- Set up tầng mồi cơ sở dữ liệu với `ApplicationDbInitializer` và `ApplicationDbSeeder`.
- Áp dụng pattern `ICustomSeeder` hỗ trợ DI, linh hoạt mở rộng Seeding logic thay vì ném code nguyên cục vào một File duy nhất.
- Chuyển kiến trúc Design Migration sang Host project. Giúp linh động config và cắt giảm code design time cồng kềnh.

### 📁 File Structure:
```text
src/
├── Migrators.MSSQL/
│   └── Migrations/             « (Chỉ chứa các file Generated Migrations)
├── Infrastructure/
│   └── Persistence/
│       ├── Initialization/
│       │   ├── IDatabaseInitializer.cs
│       │   ├── DatabaseInitializer.cs
│       │   ├── ApplicationDbInitializer.cs
│       │   ├── ApplicationDbSeeder.cs
│       │   ├── CustomSeederRunner.cs
│       │   └── ICustomSeeder.cs
│       └── Startup.cs
└── Host/
    └── Program.cs
```

---

## 8. Next Steps

**Tiếp theo:** [BUILD_09 - Domain Base Entities](BUILD_09_Domain_Base_Entities.md)

---

**Quay lại:** [Mục lục](BUILD_INDEX.md)
