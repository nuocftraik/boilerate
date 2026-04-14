# BUILD_01 - Tạo Solution và Cấu hình Build

> 📚 [Quay lại Mục lục](BUILD_INDEX.md)  
> 📋 **Yêu cầu:** .NET 8 SDK ([Tải về](https://dotnet.microsoft.com/download/dotnet/8.0))  
> ⏱️ **Thời gian:** Khoảng 15-20 phút

---

## 📋 Mục tiêu

Tạo solution với **Clean Architecture** (5 layers + 1 project migrations):

```
Shared → Domain → Application → Infrastructure → Host
(Layer trong không phụ thuộc layer ngoài)
```

**Kết quả:** Solution với 6 projects, cấu hình build, code quality tools hoạt động.

---

## 1. Kiểm tra Prerequisites

```powershell
# Kiểm tra .NET 8 đã cài chưa
dotnet --version
# Kết quả mong đợi: 8.0.x
```

**Nếu chưa có:** Tải tại https://dotnet.microsoft.com/download/dotnet/8.0

---

## 2. Tạo Solution

```powershell
# Di chuyển đến thư mục gốc
cd {Your_Workspace_Directory}

# Tạo solution
# ⚠️ Luôn dùng placeholder tên solution trong docs
dotnet new sln -n {ProjectName}
```

**✅ Checkpoint:**
```powershell
ls *.sln
# Phải thấy: {ProjectName}.sln
```

---

## 3. Tạo Projects (theo thứ tự dependency)

### ⚠️ Quan trọng: Phải tạo đúng thứ tự!

**Tại sao?** Layer trong phải tồn tại trước khi layer ngoài reference.

### ✅ Quy tắc kiến trúc áp dụng cho bước này

1. **Flat Folder Structure**
   - ✅ `src/Domain/`
   - ❌ `src/Core/Domain/`

2. **Project name = Folder name**
   - Ví dụ: `src/Domain/Domain.csproj`

---

### 3.1: Shared (Layer 1)

```powershell
dotnet new classlib -n Shared -o src\Shared
dotnet sln {ProjectName}.sln add src\Shared\Shared.csproj
```

**Dependency:** Không phụ thuộc gì ✅

---

### 3.2: Domain (Layer 2)

```powershell
dotnet new classlib -n Domain -o src\Domain
dotnet sln {ProjectName}.sln add src\Domain\Domain.csproj
dotnet add src\Domain\Domain.csproj reference src\Shared\Shared.csproj
```

**Dependency:** Shared ✅

---

### 3.3: Application (Layer 3)

```powershell
dotnet new classlib -n Application -o src\Application
dotnet sln {ProjectName}.sln add src\Application\Application.csproj
dotnet add src\Application\Application.csproj reference src\Domain\Domain.csproj
dotnet add src\Application\Application.csproj reference src\Shared\Shared.csproj
```

**Dependency:** Domain + Shared ✅

---

### 3.4: Infrastructure (Layer 4)

```powershell
dotnet new classlib -n Infrastructure -o src\Infrastructure
dotnet sln {ProjectName}.sln add src\Infrastructure\Infrastructure.csproj
dotnet add src\Infrastructure\Infrastructure.csproj reference src\Application\Application.csproj
dotnet add src\Infrastructure\Infrastructure.csproj reference src\Domain\Domain.csproj
```

**Dependency:** Application + Domain ✅

---

### 3.5: Host (Layer 5)

```powershell
dotnet new webapi -n Host -o src\Host
dotnet sln {ProjectName}.sln add src\Host\Host.csproj
dotnet add src\Host\Host.csproj reference src\Infrastructure\Infrastructure.csproj
dotnet add src\Host\Host.csproj reference src\Application\Application.csproj
```

**Dependency:** Infrastructure + Application ✅

---

### 3.6: Migrators

```powershell
dotnet new classlib -n Migrators.MSSQL -o src\Migrators.MSSQL
dotnet sln {ProjectName}.sln add src\Migrators.MSSQL\Migrators.MSSQL.csproj
dotnet add src\Migrators.MSSQL\Migrators.MSSQL.csproj reference src\Infrastructure\Infrastructure.csproj
dotnet add src\Migrators.MSSQL\Migrators.MSSQL.csproj reference src\Domain\Domain.csproj
```

**Dependency:** Infrastructure + Domain ✅

---

**✅ Checkpoint:**
```powershell
dotnet build {ProjectName}.sln
# Kết quả: Build succeeded
```

---

## 4. Cấu hình Build

### 4.1: Directory.Build.props

**File:** `Directory.Build.props` (root - cùng cấp .sln)

```xml
<Project>
	<PropertyGroup>
		<AnalysisLevel>latest</AnalysisLevel>
		<AnalysisMode>All</AnalysisMode>
		<TreatWarningsAsErrors>false</TreatWarningsAsErrors>
		<EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
	</PropertyGroup>
	
	<ItemGroup>
		<PackageReference
			Include="StyleCop.Analyzers"
			Version="1.1.118"
			PrivateAssets="all"
			Condition="$(MSBuildProjectExtension) == '.csproj'"
		/>
		<PackageReference
			Include="SonarAnalyzer.CSharp"
			Version="9.32.0.97167"
			PrivateAssets="all"
			Condition="$(MSBuildProjectExtension) == '.csproj'"
		/>
	</ItemGroup>
	
	<ItemGroup>
		<AdditionalFiles Include="$(MSBuildThisFileDirectory)stylecop.json" Link="stylecop.json" />
	</ItemGroup>
</Project>
```

**Giải thích:**
- **StyleCop:** Kiểm tra code style
- **SonarAnalyzer:** Phát hiện bugs, security issues
- Áp dụng cho tất cả .csproj

---

### 4.2: Directory.Build.targets

**File:** `Directory.Build.targets` (root)

```xml
<Project>
	<PropertyGroup>
		<DocumentationFile>$(OutputPath)$(AssemblyName).xml</DocumentationFile>
	</PropertyGroup>
</Project>
```

**Tác dụng:** Auto-generate XML documentation cho IntelliSense.

---

### 4.3: stylecop.json

**File:** `stylecop.json` (root)

```json
{
  "$schema": "https://raw.githubusercontent.com/DotNetAnalyzers/StyleCopAnalyzers/master/StyleCop.Analyzers/StyleCop.Analyzers/Settings/stylecop.schema.json",
  "settings": {
    "orderingRules": {
      "systemUsingDirectivesFirst": true,
      "usingDirectivesPlacement": "outsideNamespace"
    },
    "layoutRules": {
    "newlineAtEndOfFile": "omit"
    }
  }
}
```

**Giải thích:**
- System usings trước
- Usings ngoài namespace (C# 10+)

---

### 4.4: .editorconfig version mới nhất

**File:** `.editorconfig` (root)

```ini
root = true

[*]
charset = utf-8
indent_style = space
insert_final_newline = false
trim_trailing_whitespace = true

[*.cs]
indent_size = 4

# PascalCase cho classes
dotnet_naming_rule.types_should_be_pascal_case.severity = warning
dotnet_naming_rule.types_should_be_pascal_case.symbols = types
dotnet_naming_rule.types_should_be_pascal_case.style = pascal_case

dotnet_naming_symbols.types.applicable_kinds = class, struct, interface, enum
dotnet_naming_style.pascal_case.capitalization = pascal_case

# _camelCase cho private fields
dotnet_naming_rule.private_fields_should_be_camel_case_with_underscore.severity = warning
dotnet_naming_rule.private_fields_should_be_camel_case_with_underscore.symbols = private_fields
dotnet_naming_rule.private_fields_should_be_camel_case_with_underscore.style = camel_case_with_underscore

dotnet_naming_symbols.private_fields.applicable_kinds = field
dotnet_naming_symbols.private_fields.applicable_accessibilities = private

dotnet_naming_style.camel_case_with_underscore.required_prefix = _
dotnet_naming_style.camel_case_with_underscore.capitalization = camel_case

# var preferences
csharp_style_var_for_built_in_types = false:warning
csharp_style_var_when_type_is_apparent = true:suggestion

# File-scoped namespaces
csharp_style_namespace_declarations = file_scoped:warning

# Always use braces
csharp_prefer_braces = true:warning

[*.{xml,csproj,props,targets}]
indent_size = 2

[*.json]
indent_size = 2
```

**Giải thích:**
- **Naming:** PascalCase classes, _camelCase private fields
- **var:** Chỉ dùng khi type rõ ràng
- **Namespaces:** File-scoped (modern C#)
- **Braces:** Luôn dùng {} (safety)

---

**✅ Checkpoint:**
```powershell
dotnet clean
dotnet build {ProjectName}.sln -v detailed | Select-String "analyzer"

# Kết quả mong đợi:
# Using analyzer: StyleCop.Analyzers
# Using analyzer: SonarAnalyzer.CSharp
```

---

## 5. Cấu trúc thư mục

```
{Your_Workspace_Directory}\
├── {ProjectName}.sln            ⭐ Solution
├── Directory.Build.props        ⭐ Build config
├── Directory.Build.targets      ⭐ XML docs
├── stylecop.json                ⭐ StyleCop rules
├── .editorconfig                ⭐ Editor format
│
└── src\
    ├── Shared\
    │   ├── Shared.csproj        ⭐ Layer 1
    │   └── Class1.cs            (có thể xóa)
    ├── Domain\
    │   ├── Domain.csproj        ⭐ Layer 2
    │   └── Class1.cs
    ├── Application\
    │   ├── Application.csproj   ⭐ Layer 3
    │   └── Class1.cs
    ├── Infrastructure\
    │   ├── Infrastructure.csproj ⭐ Layer 4
    │   └── Class1.cs
    ├── Host\
    │   ├── Host.csproj          ⭐ Layer 5
    │   ├── Program.cs
    │   └── Controllers\
    └── Migrators.MSSQL\
        ├── Migrators.MSSQL.csproj ⭐ DB tool
        └── Class1.cs
```

**Cleanup (optional):**
```powershell
# Xóa Class1.cs template files
Remove-Item src\Shared\Class1.cs -ErrorAction SilentlyContinue
Remove-Item src\Domain\Class1.cs -ErrorAction SilentlyContinue
Remove-Item src\Application\Class1.cs -ErrorAction SilentlyContinue
Remove-Item src\Infrastructure\Class1.cs -ErrorAction SilentlyContinue
Remove-Item src\Migrators.MSSQL\Class1.cs -ErrorAction SilentlyContinue
```

---

## 6. Tổng kết

### ✅ Đã hoàn thành:

**Solution:**
- ✅ 6 projects theo Clean Architecture
- ✅ Dependencies đúng thứ tự
- ✅ Build thành công

**Build Config:**
- ✅ StyleCop + SonarAnalyzer
- ✅ XML documentation
- ✅ Code style rules
- ✅ Editor formatting

---

## 7. Bước tiếp theo

**Tiếp tục:** [BUILD_02 - Shared Layer](BUILD_02_Shared_Layer.md)

Tạo authorization constants (Actions, Functions, Roles, Permissions).

---

## 💡 Quick Setup Script

Tạo `setup-solution.ps1`:

```powershell
$root = "{Your_Workspace_Directory}"
cd $root

dotnet new sln -n {ProjectName}

dotnet new classlib -n Shared -o src\Shared
dotnet new classlib -n Domain -o src\Domain
dotnet new classlib -n Application -o src\Application
dotnet new classlib -n Infrastructure -o src\Infrastructure
dotnet new webapi -n Host -o src\Host
dotnet new classlib -n Migrators.MSSQL -o src\Migrators.MSSQL

Get-ChildItem -Recurse -Filter *.csproj | ForEach-Object { dotnet sln {ProjectName}.sln add $_.FullName }

dotnet add src\Domain\Domain.csproj reference src\Shared\Shared.csproj
dotnet add src\Application\Application.csproj reference src\Domain\Domain.csproj src\Shared\Shared.csproj
dotnet add src\Infrastructure\Infrastructure.csproj reference src\Application\Application.csproj src\Domain\Domain.csproj
dotnet add src\Host\Host.csproj reference src\Infrastructure\Infrastructure.csproj src\Application\Application.csproj
dotnet add src\Migrators.MSSQL\Migrators.MSSQL.csproj reference src\Infrastructure\Infrastructure.csproj src\Domain\Domain.csproj

dotnet build {ProjectName}.sln

Write-Host "✅ Setup hoàn thành!"
```

**Chạy:**
```powershell
.\setup-solution.ps1
```

---

**Quay lại:** [Mục lục](BUILD_INDEX.md)
