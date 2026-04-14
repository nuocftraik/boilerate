# BUILD_06 - Host Layer

> 📚 [Quay lại Mục lục](BUILD_INDEX.md)  
> 📋 **Prerequisites:** BUILD_05 (Infrastructure Layer) đã hoàn thành  
> ⏱️ **Thời gian:** Khoảng 15 phút

---

## 1. Setup Host Project

**File:** `src/Host/Host.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>{ProjectName}.Host</RootNamespace>
    <AssemblyName>{ProjectName}.Host</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Application\Application.csproj" />
    <ProjectReference Include="..\Infrastructure\Infrastructure.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.4.0" />
  </ItemGroup>

  <ItemGroup>
    <Content Update="Configurations\*.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
</Project>
```

---

## 2. Configuration System

```powershell
New-Item -ItemType Directory -Path "src\Host\Configurations" -Force
```

**File:** `src/Host/Configurations/Startup.cs`

```csharp
namespace {ProjectName}.Host.Configurations;

internal static class Startup
{
    internal static WebApplicationBuilder AddConfigurations(
        this WebApplicationBuilder builder)
    {
 const string configurationsDirectory = "Configurations";
        var env = builder.Environment;
   
        builder.Configuration
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
  .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
   .AddJsonFile($"{configurationsDirectory}/database.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"{configurationsDirectory}/database.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
        .AddEnvironmentVariables();
     
     return builder;
    }
}
```

---

**File:** `src/Host/Configurations/database.json`

```json
{
  "DatabaseSettings": {
    "DBProvider": "mssql",
    "ConnectionString": "Server=localhost;Database={ProjectName}Db;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

---

**File:** `src/Host/appsettings.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
    "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

---

**File:** `src/Host/appsettings.Development.json`

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

---

## 3. Program.cs

**File:** `src/Host/Program.cs`

```csharp
using {ProjectName}.Application;
using {ProjectName}.Host.Configurations;
using {ProjectName}.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddConfigurations();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
 Title = "{ProjectName} API",
        Version = "v1"
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseInfrastructure(builder.Configuration);
app.MapEndpoints();

app.Run();
```

---

## 4. Controllers

```powershell
New-Item -ItemType Directory -Path "src\Host\Controllers" -Force
```

**File:** `src/Host/Controllers/BaseApiController.cs`

```csharp
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace {ProjectName}.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
  private ISender? _mediator;
    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();
}
```

---

**File:** `src/Host/Controllers/HealthController.cs`

```csharp
using Microsoft.AspNetCore.Mvc;

namespace {ProjectName}.Host.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
    Status = "Healthy",
 Timestamp = DateTime.UtcNow
        });
    }
}
```

---

## 5. Verify

```powershell
dotnet build
dotnet run --project src\Host\Host.csproj
```

**Test Swagger:** `https://localhost:7001/swagger`

**Test Health:** 
```powershell
curl https://localhost:7001/api/health
```

---

## 6. Cấu trúc thư mục

```
src\Host\
├── Host.csproj
├── Program.cs
├── Configurations\
│   ├── Startup.cs
│   └── database.json
├── Controllers\
│   ├── BaseApiController.cs
│   └── HealthController.cs
├── appsettings.json
└── appsettings.Development.json
```

---

## 7. Bước tiếp theo

**Tiếp theo:** [BUILD_07 - Logging Setup](BUILD_07_Logging_Setup.md)

---

**Quay lại:** [Mục lục](BUILD_INDEX.md)
