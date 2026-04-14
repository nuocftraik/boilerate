# BUILD_05 - Infrastructure Layer

> 📚 [Quay lại Mục lục](BUILD_INDEX.md)  
> 📋 **Prerequisites:** BUILD_04 (Application Layer) đã hoàn thành  
> ⏱️ **Thời gian:** Khoảng 15 phút

---

## 1. Xóa file template

```powershell
Remove-Item src\Infrastructure\Class1.cs -ErrorAction SilentlyContinue
```

---

## 2. Add Required Packages

**File:** `src/Infrastructure/Infrastructure.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>{ProjectName}.Infrastructure</RootNamespace>
    <AssemblyName>{ProjectName}.Infrastructure</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Application\Application.csproj" />
    <ProjectReference Include="..\Domain\Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="8.0.0">
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Ardalis.Specification.EntityFrameworkCore" Version="8.0.0" />
    <PackageReference Include="Serilog" Version="3.1.1" />
  </ItemGroup>
</Project>
```

---

## 3. Tạo Database Context

### Bước 3.1: DatabaseSettings

```powershell
New-Item -ItemType Directory -Path "src\Infrastructure\Persistence" -Force
```

**File:** `src/Infrastructure/Persistence/DatabaseSettings.cs`

```csharp
using System.ComponentModel.DataAnnotations;

namespace {ProjectName}.Infrastructure.Persistence;

public class DatabaseSettings
{
    [Required]
    public string DBProvider { get; set; } = string.Empty;

    [Required]
    public string ConnectionString { get; set; } = string.Empty;
}
```

---

### Bước 3.2: BaseDbContext

```powershell
New-Item -ItemType Directory -Path "src\Infrastructure\Persistence\Context" -Force
```

**File:** `src/Infrastructure/Persistence/Context/BaseDbContext.cs`

```csharp
using {ProjectName}.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace {ProjectName}.Infrastructure.Persistence.Context;

public abstract class BaseDbContext : IdentityDbContext<
    ApplicationUser, 
    ApplicationRole, 
    string, 
    IdentityUserClaim<string>, 
    IdentityUserRole<string>, 
    IdentityUserLogin<string>, 
    ApplicationRoleClaim, 
    IdentityUserToken<string>>
{
    protected BaseDbContext(DbContextOptions options) : base(options)
    {
    }

 protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
 {
        optionsBuilder.EnableSensitiveDataLogging();
    }
}
```

---

### Bước 3.3: ApplicationDbContext

**File:** `src/Infrastructure/Persistence/Context/ApplicationDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;

namespace {ProjectName}.Infrastructure.Persistence.Context;

public class ApplicationDbContext : BaseDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
}
```

---

### Bước 3.4: EF Core Configurations

```powershell
New-Item -ItemType Directory -Path "src\Infrastructure\Persistence\Configuration" -Force
```

**File:** `src/Infrastructure/Persistence/Configuration/SchemaNames.cs`

```csharp
namespace {ProjectName}.Infrastructure.Persistence.Configuration;

internal static class SchemaNames
{
    public const string Identity = nameof(Identity);
    public const string Catalog = nameof(Catalog);
  public const string Auditing = nameof(Auditing);
}
```

**File:** `src/Infrastructure/Persistence/Configuration/Identity.cs`

```csharp
using {ProjectName}.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace {ProjectName}.Infrastructure.Persistence.Configuration;

public class ApplicationUserConfig : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("Users", SchemaNames.Identity);
        builder.Property(u => u.ObjectId).HasMaxLength(256);
    }
}

public class ApplicationRoleConfig : IEntityTypeConfiguration<ApplicationRole>
{
    public void Configure(EntityTypeBuilder<ApplicationRole> builder)
    {
        builder.ToTable("Roles", SchemaNames.Identity);
    }
}

public class ApplicationRoleClaimConfig : IEntityTypeConfiguration<ApplicationRoleClaim>
{
    public void Configure(EntityTypeBuilder<ApplicationRoleClaim> builder)
    {
        builder.ToTable("RoleClaims", SchemaNames.Identity);
    }
}

public class IdentityUserRoleConfig : IEntityTypeConfiguration<IdentityUserRole<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserRole<string>> builder)
    {
        builder.ToTable("UserRoles", SchemaNames.Identity);
    }
}

public class IdentityUserClaimConfig : IEntityTypeConfiguration<IdentityUserClaim<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserClaim<string>> builder)
    {
     builder.ToTable("UserClaims", SchemaNames.Identity);
 }
}

public class IdentityUserLoginConfig : IEntityTypeConfiguration<IdentityUserLogin<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserLogin<string>> builder)
    {
        builder.ToTable("UserLogins", SchemaNames.Identity);
    }
}

public class IdentityUserTokenConfig : IEntityTypeConfiguration<IdentityUserToken<string>>
{
    public void Configure(EntityTypeBuilder<IdentityUserToken<string>> builder)
    {
        builder.ToTable("UserTokens", SchemaNames.Identity);
    }
}
```

---

## 4. Setup Persistence Module

**File:** `src/Infrastructure/Persistence/Startup.cs`

```csharp
using {ProjectName}.Infrastructure.Persistence.Context;
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

        return services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
         var dbSettings = sp.GetRequiredService<IOptions<DatabaseSettings>>().Value;
            _logger.Information("DB Provider: {provider}", dbSettings.DBProvider);
     options.UseDatabase(dbSettings.DBProvider, dbSettings.ConnectionString);
        });
    }

    internal static DbContextOptionsBuilder UseDatabase(
        this DbContextOptionsBuilder builder, 
      string provider, 
    string connectionString)
    {
   return provider.ToLowerInvariant() switch
        {
            "mssql" => builder.UseSqlServer(connectionString, 
  e => e.MigrationsAssembly("Migrators.MSSQL")),
            _ => throw new InvalidOperationException($"DB Provider '{provider}' not supported")
        };
    }
}
```

---

## 5. Main Infrastructure Startup

**File:** `src/Infrastructure/Startup.cs`

```csharp
using {ProjectName}.Infrastructure.Persistence;
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
    .UseHttpsRedirection();
    }

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapControllers();
    return builder;
    }
}
```

---

## 6. Verify

```powershell
dotnet build src\Infrastructure\Infrastructure.csproj
```

---

## 7. Cấu trúc thư mục

```
src\Infrastructure\
├── Infrastructure.csproj
├── Startup.cs
├── Persistence\
│   ├── Context\
│   │   ├── BaseDbContext.cs
│   │   └── ApplicationDbContext.cs
│   ├── Configuration\
│   │   ├── SchemaNames.cs
│   │   └── Identity.cs
│   ├── DatabaseSettings.cs
│ └── Startup.cs
└── obj\
```

---

## 8. Bước tiếp theo

**Tiếp theo:** [BUILD_06 - Host Layer](BUILD_06_Host_Layer.md)

---

**Quay lại:** [Mục lục](BUILD_INDEX.md)
