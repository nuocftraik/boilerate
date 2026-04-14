# BUILD_04 - Application Layer

> 📚 [Quay lại Mục lục](BUILD_INDEX.md)  
> 📋 **Prerequisites:** BUILD_03 (Domain Layer) đã hoàn thành  
> ⏱️ **Thời gian:** Khoảng 10 phút

---

## 1. Xóa file template

```powershell
Remove-Item src\Application\Class1.cs -ErrorAction SilentlyContinue
```

---

## 2. Add Required Packages

**File:** `src/Application/Application.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>{ProjectName}.Application</RootNamespace>
    <AssemblyName>{ProjectName}.Application</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Domain\Domain.csproj" />
    <ProjectReference Include="..\Shared\Shared.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Ardalis.Specification" Version="8.0.0" />
    <PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.2" />
    <PackageReference Include="Mapster" Version="7.4.0" />
    <PackageReference Include="MediatR" Version="12.4.0" />
    <PackageReference Include="Microsoft.Extensions.Caching.Abstractions" Version="8.0.0" />
    <PackageReference Include="Microsoft.Extensions.Localization" Version="8.0.0" />
  </ItemGroup>
</Project>
```

**Packages:**
- **MediatR:** CQRS pattern
- **FluentValidation:** Request validation
- **Mapster:** Object mapping
- **Ardalis.Specification:** Specification pattern

---

## 3. Tạo Application Startup

**File:** `src/Application/Startup.cs`

```csharp
using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace {ProjectName}.Application;

public static class Startup
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();
        
        return services
            .AddValidatorsFromAssembly(assembly)
    .AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
    }
}
```

---

## 4. Verify

```powershell
dotnet build src\Application\Application.csproj
```

---

## 5. Cấu trúc thư mục

```
src\Application\
├── Application.csproj
├── Startup.cs
└── obj\
```

---

## 6. Bước tiếp theo

**Tiếp theo:** [BUILD_05 - Infrastructure Layer](BUILD_05_Infrastructure_Layer.md)

---

**Quay lại:** [Mục lục](BUILD_INDEX.md)
