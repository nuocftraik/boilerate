# BUILD_03 - Domain Layer (Identity Entities)

> 📚 [Quay lại Mục lục](BUILD_INDEX.md)  
> 📋 **Prerequisites:** BUILD_02 (Shared Layer) đã hoàn thành  
> ⏱️ **Thời gian:** Khoảng 15 phút


---

## 1. Xóa file template

```powershell
Remove-Item src\Domain\Class1.cs -ErrorAction SilentlyContinue
```

---

## 2. Add Required Packages

**File:** `src/Domain/Domain.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>{ProjectName}.Domain</RootNamespace>
    <AssemblyName>{ProjectName}.Domain</AssemblyName>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\Shared\Shared.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="NewId" Version="4.0.1" />
    <PackageReference Include="Microsoft.AspNetCore.Identity" Version="2.1.39" />
    <PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.0" />
  </ItemGroup>
</Project>
```

---

## 3. Tạo Identity Entities

### Bước 3.1: Tạo Identity folder

```powershell
New-Item -ItemType Directory -Path "src\Domain\Identity" -Force
```

---

### Bước 3.2: ApplicationUser

**File:** `src/Domain/Identity/ApplicationUser.cs`

```csharp
using Microsoft.AspNetCore.Identity;

namespace {ProjectName}.Domain.Identity;

public class ApplicationUser : IdentityUser
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ObjectId { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
}
```

---

### Bước 3.3: ApplicationRole

**File:** `src/Domain/Identity/ApplicationRole.cs`

```csharp
using Microsoft.AspNetCore.Identity;

namespace {ProjectName}.Domain.Identity;

public class ApplicationRole : IdentityRole
{
    public ApplicationRole() { }
    
    public ApplicationRole(string roleName, string? description = null) : base(roleName)
    {
 Description = description;
        NormalizedName = roleName.ToUpperInvariant();
    }

    public string? Description { get; set; }
}
```

---

### Bước 3.4: ApplicationRoleClaim

**File:** `src/Domain/Identity/ApplicationRoleClaim.cs`

```csharp
using Microsoft.AspNetCore.Identity;

namespace {ProjectName}.Domain.Identity;

public class ApplicationRoleClaim : IdentityRoleClaim<string>
{
    public string? Description { get; set; }
    public string? Group { get; set; }
    public DateTime CreatedOn { get; set; }
    public string? CreatedBy { get; set; }
}
```

---

## 4. Verify

```powershell
dotnet build src\Domain\Domain.csproj
```

---

## 5. Cấu trúc thư mục

```
src\Domain\
├── Domain.csproj
├── Identity\
│   ├── ApplicationUser.cs
│   ├── ApplicationRole.cs
│   └── ApplicationRoleClaim.cs
└── obj\
```

---

## 7. Bước tiếp theo

**Tiếp theo:** [BUILD_04 - Application Layer](BUILD_04_Application_Layer.md)

---

**Quay lại:** [Mục lục](BUILD_INDEX.md)
