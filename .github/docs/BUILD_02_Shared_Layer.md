# BUILD_02 - Shared Layer (Authorization Constants)

> 📚 [Quay lại Mục lục](BUILD_INDEX.md)  
> 📋 **Prerequisites:** BUILD_01 đã hoàn thành  
> ⏱️ **Thời gian:** Khoảng 10 phút

---

## 📋 Mục tiêu

Tạo **Shared Layer** với authorization constants cơ bản.

**Kết quả:** Constants cho Actions, Functions, Roles, Claims, Permissions - foundation cho authorization system.

**Tại sao cần?**
- ✅ **Type-safe:** Tránh magic strings `"Users.View"` → `AppPermission.NameFor(AppAction.View, AppFunction.Users)`
- ✅ **Centralized:** Thêm/sửa permission ở 1 chỗ
- ✅ **Maintainable:** Dễ refactor, IDE support

> 💡 **Lưu ý:** Shared layer sẽ chứa thêm Events, Notifications sau (BUILD_09, BUILD_29). Bây giờ chỉ focus Authorization.

---

## 1. Xóa file template

```powershell
# Xóa Class1.cs (file template không dùng)
Remove-Item src\Shared\Class1.cs -ErrorAction SilentlyContinue
```

---

## 2. Tạo Authorization Constants

### Bước 2.1: Tạo folder

```powershell
New-Item -ItemType Directory -Path "src\Shared\Authorization" -Force
```

---

### Bước 2.2: AppAction - CRUD Actions

**Làm gì:** Định nghĩa actions cơ bản (View, Create, Update, Delete...)

**Tại sao:** Tạo building blocks cho permission system, tránh hardcode strings.

**File:** `src/Shared/Authorization/AppAction.cs`

```csharp
namespace {ProjectName}.Shared.Authorization;

/// <summary>
/// Các actions cơ bản trong hệ thống
/// </summary>
public static class AppAction
{
    public const string View = nameof(View);
    public const string Search = nameof(Search);
    public const string Create = nameof(Create);
    public const string Update = nameof(Update);
    public const string Delete = nameof(Delete);
    public const string Import = nameof(Import);
    public const string Export = nameof(Export);
    public const string Clean = nameof(Clean);
}
```

**Giải thích:**
- `const string`: Compile-time constant cho performance
- `nameof()`: Type-safe, refactor-friendly (IDE tự động rename)
- **View/Search:** Read operations
- **Create/Update/Delete:** Write operations
- **Import/Export:** Batch operations
- **Clean:** Cleanup/Archive operations

**Tại sao dùng `const` thay vì `enum`:**
- Permissions dùng string (JWT claims, database)
- `const` compile thành literal → zero overhead
- Dễ serialize/deserialize JSON

---

### Bước 2.3: AppFunction - Modules

**Làm gì:** Định nghĩa modules/features trong hệ thống.

**Tại sao:** Kết hợp với Actions để tạo permissions có cấu trúc rõ ràng.

**File:** `src/Shared/Authorization/AppFunction.cs`

```csharp
namespace {ProjectName}.Shared.Authorization;

/// <summary>
/// Các modules/features trong hệ thống
/// </summary>
public static class AppFunction
{
    public const string Dashboard = nameof(Dashboard);
    public const string Hangfire = nameof(Hangfire);
    public const string Users = nameof(Users);
    public const string Roles = nameof(Roles);
    public const string Products = nameof(Products);
    public const string Categories = nameof(Categories);
}
```

**Giải thích:**
- Mỗi function = 1 module quản lý
- Dùng để generate permissions: `"Permissions.Users.View"`, `"Permissions.Products.Create"`
- **Dashboard:** Trang chủ (chỉ cần View)
- **Hangfire:** Background jobs monitoring
- **Users/Roles:** Identity management
- **Products/Categories:** Business modules

**Tại sao pattern này:**
- Scalable: Thêm module chỉ cần thêm 1 dòng
- Discoverable: IDE autocomplete list tất cả modules
- Type-safe: Refactor an toàn

---

### Bước 2.4: AppRoles - Default Roles

**Làm gì:** Định nghĩa roles mặc định khi khởi tạo hệ thống.

**Tại sao:** Seed database với roles chuẩn, protect default roles khỏi bị xóa.

**File:** `src/Shared/Authorization/AppRoles.cs`

```csharp
using System.Collections.ObjectModel;

namespace {ProjectName}.Shared.Authorization;

/// <summary>
/// Default roles trong hệ thống
/// </summary>
public static class AppRoles
{
    public const string Admin = nameof(Admin);
    public const string Basic = nameof(Basic);

    public static IReadOnlyList<string> DefaultRoles { get; } = new ReadOnlyCollection<string>(new[]
 {
        Admin,
        Basic
    });

    public static bool IsDefault(string roleName) => 
     DefaultRoles.Any(r => r.Equals(roleName, StringComparison.OrdinalIgnoreCase));
}
```

**Giải thích:**
- **Admin:** Full permissions, quản trị hệ thống
- **Basic:** Limited permissions, user thông thường
- **DefaultRoles:** ReadOnly collection để seed database (BUILD_08)
- **IsDefault():** Protect default roles khỏi bị xóa/rename

**Lợi ích:**
- ✅ Centralized role definitions
- ✅ Type-safe role checks
- ✅ Prevent accidental deletion

---

### Bước 2.5: AppClaims - JWT Claims

**Làm gì:** Định nghĩa JWT claim keys.

**Tại sao:** Standardize claim names trong tokens, tránh typo khi đọc/ghi claims.

**File:** `src/Shared/Authorization/AppClaims.cs`

```csharp
namespace {ProjectName}.Shared.Authorization;

/// <summary>
/// JWT claim keys
/// </summary>
public static class AppClaims
{
    public const string Fullname = "fullName";
    public const string Permission = "permission";
    public const string ImageUrl = "image_url";
    public const string IpAddress = "ipAddress";
    public const string Expiration = "exp";
}
```

**Giải thích:**
- **Fullname:** Display name trong UI
- **Permission:** Array of permission strings (critical cho authorization)
- **ImageUrl:** Avatar URL
- **IpAddress:** Audit trail
- **Expiration:** JWT standard claim

**Tại sao quan trọng:**
- JWT claims = string keys → typo = security bug
- Centralized = dễ audit và maintain

---

### Bước 2.6: AppPermission - Dynamic Generation

**Làm gì:** Generate permissions từ Actions + Functions với reflection.

**Tại sao:** Tự động tạo permissions thay vì hardcode từng cái.

**File:** `src/Shared/Authorization/AppPermission.cs`

```csharp
using System.Reflection;

namespace {ProjectName}.Shared.Authorization;

/// <summary>
/// Permission record với dynamic generation
/// Format: "Permissions.{Function}.{Action}"
/// </summary>
public record AppPermission(string Action, string Function)
{
    public string Name => NameFor(Action, Function);

    public static string NameFor(string action, string function) => 
        $"Permissions.{function}.{action}";

    /// <summary>
    /// Generate tất cả permissions cho một function
    /// </summary>
    public static List<string> GeneratePermissionsForFunction(string function)
    {
        // Dùng reflection để lấy tất cả const fields từ AppAction
        var actions = typeof(AppAction)
     .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
       .Where(field => field.IsLiteral && !field.IsInitOnly) // Chỉ lấy const
    .Select(field => field.GetValue(null)?.ToString())
       .Where(value => value != null)
   .Cast<string>()
   .ToList();

        return actions.Select(action => NameFor(action, function)).ToList();
    }

    /// <summary>
    /// Generate permissions với custom actions
    /// </summary>
    public static List<string> GeneratePermissionsForFunction(string function, List<string> actions)
    {
        if (actions == null || actions.Count == 0)
  throw new ArgumentException("Actions không được null hoặc empty", nameof(actions));

    return actions.Select(action => NameFor(action, function)).ToList();
    }
}
```

**Giải thích:**
- **Record:** Immutable data class (C# 9+)
- **NameFor():** Format permission string theo convention
- **GeneratePermissionsForFunction():** 
  - Dùng reflection để scan `AppAction` constants
  - `IsLiteral && !IsInitOnly` = lọc chỉ `const` fields
  - Kết hợp với function → permission strings
- **Overload:** Custom actions cho special cases

**Tại sao dùng Reflection:**
- DRY: Không phải maintain 2 lists (Actions + Permissions)
- Auto-update: Thêm action mới → permissions tự sinh
- Trade-off: Reflection cost chỉ chạy lúc seed database (BUILD_08)

**Usage example:**
```csharp
// Generate all permissions cho Users (8 actions)
var userPermissions = AppPermission.GeneratePermissionsForFunction(AppFunction.Users);
// Result: 
// ["Permissions.Users.View", 
//  "Permissions.Users.Search", 
//  "Permissions.Users.Create", 
//  "Permissions.Users.Update",
//  "Permissions.Users.Delete",
//  "Permissions.Users.Import",
//  "Permissions.Users.Export",
//  "Permissions.Users.Clean"]

// Custom permissions cho Dashboard (chỉ View)
var dashboardPermissions = AppPermission.GeneratePermissionsForFunction(
    AppFunction.Dashboard, 
    new List<string> { AppAction.View }
);
// Result: ["Permissions.Dashboard.View"]
```

**Lợi ích:**
- ✅ DRY (Don't Repeat Yourself)
- ✅ Scalable: Thêm action/function → permissions auto-update
- ✅ Type-safe với IDE IntelliSense

---

## 3. Verify

```powershell
# Build Shared project
dotnet build src\Shared\Shared.csproj

# Kết quả mong đợi: Build succeeded
```

**✅ Checkpoint:**
- Build thành công
- 5 files trong `Authorization` folder
- Namespace = `{ProjectName}.Shared.Authorization`

---

## 4. Cấu trúc thư mục sau BUILD_02

```
src\Shared\
├── Shared.csproj
├── Authorization\     📁 NEW
│   ├── AppAction.cs      ⭐ CRUD actions (8 constants)
│   ├── AppFunction.cs         ⭐ Modules (6 constants)
│   ├── AppRoles.cs            ⭐ Default roles (2 roles)
│   ├── AppClaims.cs    ⭐ JWT claims (5 keys)
│   └── AppPermission.cs       ⭐ Dynamic generation (reflection)
└── obj\       (build artifacts)
```

**Dependencies:** None ✅ (Shared không phụ thuộc layer nào)

---

## 5. Tổng kết

### ✅ Đã hoàn thành trong bước này:

**Authorization Foundation:**
- ✅ `AppAction` - 8 CRUD actions
- ✅ `AppFunction` - 6 modules (Dashboard, Hangfire, Users, Roles, Products, Categories)
- ✅ `AppRoles` - 2 default roles (Admin, Basic)
- ✅ `AppClaims` - 5 JWT claim keys
- ✅ `AppPermission` - Dynamic permission generation với reflection

**Key Concepts:**
- **Type-safe Constants:** Compile-time checking, zero runtime cost
- **Convention over Configuration:** `Permissions.{Function}.{Action}`
- **Reflection for DRY:** Auto-generate permissions từ Actions
- **Centralized:** Một chỗ quản lý tất cả authorization constants

**Chưa làm (sẽ làm sau):**
- ⏸️ Domain Events (BUILD_09 - Domain layer)
- ⏸️ Notifications (BUILD_29 - SignalR)

---

## 6. Bước tiếp theo

**Tiếp theo:** [BUILD_03 - Domain Layer](BUILD_03_Domain_Layer.md)

Trong bước tiếp theo, chúng ta sẽ:
1. ✅ Tạo Domain entities với ASP.NET Core Identity
2. ✅ Setup Identity user/role entities
3. ✅ Tạo custom Permission entities

---

**Quay lại:** [Mục lục](BUILD_INDEX.md)
