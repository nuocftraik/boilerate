# User Management Service - User CRUD Operations

> 📚 [Quay lại Mục lục](BUILD_INDEX.md)  
> 📋 **Prerequisites:** Bước 15 (JWT Authentication) đã hoàn thành

Tài liệu này hướng dẫn xây dựng User Management Service - Quản lý người dùng với đầy đủ CRUD operations, sử dụng ASP.NET Core Identity.

---

## 1. Overview

**Làm gì:** Xây dựng User Management Service để quản lý người dùng (Search, Get, Create, Update, Toggle Status, Password Management).

**Tại sao cần:**
- **User Management:** CRUD operations cho user accounts thông qua ASP.NET Core Identity
- **Self-Registration:** Cho phép users tự đăng ký tài khoản (anonymous)
- **Profile Management:** Users có thể update thông tin cá nhân và avatar
- **Status Management:** Admin có thể activate/deactivate users
- **Partial Class Pattern:** Tổ chức code theo nhóm chức năng, dễ maintain

**Trong bước này chúng ta sẽ:**
- ✅ Tạo User DTOs (`UserDetailDto`, `CreateUserRequest`, `UpdateUserRequest`, `ToggleUserStatusRequest`, `UserParameterFilter`)
- ✅ Tạo Password DTOs (`ForgotPasswordRequest`, `ResetPasswordRequest`, `ChangePasswordRequest`)
- ✅ Tạo `IUserService` interface với đầy đủ method groups
- ✅ Implement `UserService` partial class (Main, CreateUpdate, Confirm, Password, Role, Permission)
- ✅ Tạo `UsersController` kế thừa `BaseApiController` với đầy đủ RESTful endpoints
- ✅ Register `UserService` vào DI container qua `Identity/Startup.cs`

**Real-world example:**
```csharp
// Admin tạo user mới
var message = await _userService.CreateAsync(new CreateUserRequest
{
    FirstName = "Nguyen",
    LastName = "Van A",
    Email = "nguyenvana@example.com",
    UserName = "nguyenvana",
    Password = "SecurePass123!",
    ConfirmPassword = "SecurePass123!",
    PhoneNumber = "+84987654321"
}, origin);
// → "User nguyenvana Registered.\nPlease check nguyenvana@example.com to verify your account!"

// User tự update profile
await _userService.UpdateAsync(new UpdateUserRequest
{
    Id = userId,
    FirstName = "Nguyen",
    LastName = "Van B",
    PhoneNumber = "+84987654322",
    Email = "nguyenvanb@example.com"
}, userId);

// Admin deactivate user
await _userService.ToggleStatusAsync(new ToggleUserStatusRequest
{
    UserId = userId,
    ActivateUser = false
}, cancellationToken);
// ConflictException nếu target là Admin account
```

---

## 2. User DTOs

### Bước 2.1: UserDetailDto

**Làm gì:** DTO để trả về thông tin user trong responses.

**Tại sao:** Không expose entity `ApplicationUser` ra ngoài Infrastructure layer - chỉ trả về fields cần thiết cho client.

**File:** `src/Application/Identity/Users/UserDetailDto.cs`

```csharp
namespace {ProjectName}.Application.Identity.Users;

/// <summary>
/// DTO trả về thông tin user cho client
/// Sử dụng Mapster để map từ ApplicationUser
/// </summary>
public class UserDetailDto
{
    public Guid Id { get; set; }

    /// <summary>Username (unique trong hệ thống)</summary>
    public string? UserName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? Email { get; set; }

    /// <summary>Admin có thể set false để ngăn user login</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>True nếu user đã xác nhận email</summary>
    public bool EmailConfirmed { get; set; }

    public string? PhoneNumber { get; set; }

    /// <summary>Relative path đến avatar image</summary>
    public string? ImageUrl { get; set; }
}
```

**Giải thích:**
- **Tại sao không dùng `ApplicationUser` trực tiếp:** Entity nằm ở Infrastructure layer, Application layer không được phụ thuộc vào Infrastructure (vi phạm Clean Architecture)
- **Mapster projection:** `ProjectToType<UserDetailDto>()` trong `SearchAsync` chỉ SELECT đúng fields cần thiết → tối ưu database query

---

### Bước 2.2: CreateUserRequest

**Làm gì:** Request DTO tạo user mới kèm FluentValidation với async uniqueness checks.

**Tại sao:** Validate trước khi đến service layer - phát hiện lỗi sớm, tiết kiệm DB roundtrip.

**File:** `src/Application/Identity/Users/CreateUserRequest.cs`

```csharp
using FluentValidation;

namespace {ProjectName}.Application.Identity.Users;

/// <summary>Request để tạo user mới (dùng cho cả Admin create và Self-register)</summary>
public class CreateUserRequest
{
    public string FirstName { get; set; } = default!;
    public string LastName { get; set; } = default!;
    public string Email { get; set; } = default!;

    /// <summary>Username - unique, min 6 ký tự</summary>
    public string UserName { get; set; } = default!;

    public string Password { get; set; } = default!;
    public string ConfirmPassword { get; set; } = default!;

    /// <summary>Optional - unique nếu được cung cấp</summary>
    public string? PhoneNumber { get; set; }
}

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator(IUserService userService)
    {
        RuleFor(u => u.Email).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .EmailAddress()
                .WithMessage("Invalid Email Address.")
            .MustAsync(async (email, _) => !await userService.ExistsWithEmailAsync(email))
                .WithMessage((_, email) => $"Email {email} is already registered.");

        RuleFor(u => u.UserName).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MinimumLength(6)
            .MustAsync(async (name, _) => !await userService.ExistsWithNameAsync(name))
                .WithMessage((_, name) => $"Username {name} is already taken.");

        RuleFor(u => u.PhoneNumber).Cascade(CascadeMode.Stop)
            .MustAsync(async (phone, _) => !await userService.ExistsWithPhoneNumberAsync(phone!))
                .WithMessage((_, phone) => $"Phone number {phone} is already registered.")
            .Unless(u => string.IsNullOrWhiteSpace(u.PhoneNumber));

        RuleFor(p => p.FirstName).Cascade(CascadeMode.Stop)
            .NotEmpty();

        RuleFor(p => p.LastName).Cascade(CascadeMode.Stop)
            .NotEmpty();

        RuleFor(p => p.Password).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .MinimumLength(6);

        RuleFor(p => p.ConfirmPassword).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Equal(p => p.Password);
    }
}
```

**Giải thích:**
- **`Cascade(CascadeMode.Stop)`:** Dừng validation chain khi rule đầu tiên fail → tránh gọi DB thừa (ví dụ: email empty thì không cần gọi `ExistsWithEmailAsync`)
- **`MustAsync`:** Gọi database async để check uniqueness - sử dụng `IUserService` được inject vào validator (FluentValidation DI support)
- **PhoneNumber `Unless`:** Chỉ validate uniqueness khi phone được cung cấp, phone là optional

---

### Bước 2.3: UpdateUserRequest

**Làm gì:** Request DTO update user profile kèm validation với `exceptId` pattern.

**Tại sao:** User cần update email/phone nhưng không bị lỗi "đã tồn tại" với chính record của họ.

**File:** `src/Application/Identity/Users/UpdateUserRequest.cs`

```csharp
namespace {ProjectName}.Application.Identity.Users;

/// <summary>Request để update thông tin user profile</summary>
public class UpdateUserRequest
{
    /// <summary>User ID (lấy từ route param trong controller)</summary>
    public string Id { get; set; } = default!;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }

    /// <summary>Upload avatar mới (từ FileUploadRequest trong Application Common)</summary>
    public FileUploadRequest? Image { get; set; }

    /// <summary>Flag để xóa avatar hiện tại, set về null</summary>
    public bool DeleteCurrentImage { get; set; } = false;
}

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator(IUserService userService)
    {
        RuleFor(p => p.Id)
            .NotEmpty();

        RuleFor(p => p.FirstName)
            .NotEmpty()
            .MaximumLength(75);

        RuleFor(p => p.LastName)
            .NotEmpty()
            .MaximumLength(75);

        RuleFor(p => p.Email)
            .NotEmpty()
            .EmailAddress()
                .WithMessage("Invalid Email Address.")
            // ExceptId: exclude chính user đang update khỏi uniqueness check
            .MustAsync(async (user, email, _) => !await userService.ExistsWithEmailAsync(email, user.Id))
                .WithMessage((_, email) => string.Format("Email {0} is already registered.", email));

        RuleFor(p => p.Image);

        RuleFor(u => u.PhoneNumber).Cascade(CascadeMode.Stop)
            .MustAsync(async (user, phone, _) => !await userService.ExistsWithPhoneNumberAsync(phone!, user.Id))
                .WithMessage((_, phone) => string.Format("Phone number {0} is already registered.", phone))
            .Unless(u => string.IsNullOrWhiteSpace(u.PhoneNumber));
    }
}
```

**Giải thích:**
- **ExceptId Pattern:** `ExistsWithEmailAsync(email, user.Id)` - truyền `user.Id` để service query `WHERE Email = @email AND Id != @exceptId` → user giữ nguyên email của mình không bị reject
- **`FileUploadRequest`:** Class từ `Application/Common/FileStorage/` (BUILD_20) - chứa base64 content và filename của image upload

---

### Bước 2.4: ToggleUserStatusRequest

**Làm gì:** Request để admin activate/deactivate user account.

**File:** `src/Application/Identity/Users/ToggleUserStatusRequest.cs`

```csharp
namespace {ProjectName}.Application.Identity.Users;

/// <summary>Request để admin toggle trạng thái active của user</summary>
public class ToggleUserStatusRequest
{
    /// <summary>User ID cần toggle</summary>
    public string UserId { get; set; } = default!;

    /// <summary>True = activate, False = deactivate</summary>
    public bool ActivateUser { get; set; }
}
```

**Giải thích:**
- Đơn giản, không cần validator riêng vì `UserId` được validate ở controller route
- Service sẽ throw `ConflictException` nếu target là Admin account

---

### Bước 2.5: UserParameterFilter

**Làm gì:** Filter cho search user list với pagination.

**Tại sao:** Kế thừa `PaginationFilter` để tái sử dụng PageNumber/PageSize logic từ BUILD_11.

**File:** `src/Application/Identity/Users/UserParameterFilter.cs`

```csharp
using {ProjectName}.Application.Common.Models;

namespace {ProjectName}.Application.Identity.Users;

/// <summary>
/// Filter cho search users với pagination
/// Kế thừa PaginationFilter để có PageNumber và PageSize
/// </summary>
public class UserParameterFilter : PaginationFilter
{
    /// <summary>Filter theo trạng thái active (null = tất cả)</summary>
    public bool? IsActive { get; set; }
}
```

**⚠️ Lưu ý:** Filter chỉ có `IsActive`. Search theo keyword được xử lý ở frontend (sort/filter client-side).

---

## 3. Password DTOs

Password operations (Forgot/Reset/Change) được tổ chức riêng trong subfolder `Password/` để tách biệt khỏi CRUD operations chính.

### Bước 3.1: ForgotPasswordRequest

**File:** `src/Application/Identity/Users/Password/ForgotPasswordRequest.cs`

```csharp
using FluentValidation;

namespace {ProjectName}.Application.Identity.Users.Password;

/// <summary>Request gửi email reset password</summary>
public class ForgotPasswordRequest
{
    public string Email { get; set; } = default!;
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator() =>
        RuleFor(p => p.Email).Cascade(CascadeMode.Stop)
            .NotEmpty()
            .EmailAddress()
                .WithMessage("Invalid Email Address.");
}
```

---

### Bước 3.2: ResetPasswordRequest

**File:** `src/Application/Identity/Users/Password/ResetPasswordRequest.cs`

```csharp
namespace {ProjectName}.Application.Identity.Users.Password;

/// <summary>Request đặt lại password với reset token từ email</summary>
public class ResetPasswordRequest
{
    /// <summary>Email để tìm user</summary>
    public string? Email { get; set; }

    /// <summary>New password</summary>
    public string? Password { get; set; }

    /// <summary>Reset token từ forgot-password email</summary>
    public string? Token { get; set; }
}
```

---

### Bước 3.3: ChangePasswordRequest

**File:** `src/Application/Identity/Users/Password/ChangePasswordRequest.cs`

```csharp
using FluentValidation;

namespace {ProjectName}.Application.Identity.Users.Password;

/// <summary>Request đổi password (user đang login)</summary>
public class ChangePasswordRequest
{
    /// <summary>Password hiện tại - để verify trước khi đổi</summary>
    public string Password { get; set; } = default!;

    public string NewPassword { get; set; } = default!;

    public string ConfirmNewPassword { get; set; } = default!;
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(p => p.Password)
            .NotEmpty();

        RuleFor(p => p.NewPassword)
            .NotEmpty();

        RuleFor(p => p.ConfirmNewPassword)
            .Equal(p => p.NewPassword)
                .WithMessage("Passwords do not match.");
    }
}
```

---

## 4. User Service Interface

### Bước 4.1: IUserService

**Làm gì:** Define contract đầy đủ cho tất cả user operations, phân nhóm bằng `#region`.

**Tại sao:**
- **Abstraction:** Infrastructure implements Application interface - core Clean Architecture principle
- **DI support:** Inject `IUserService` vào controllers và validators mà không phụ thuộc implementation
- **Testability:** Dễ mock trong unit tests

**File:** `src/Application/Identity/Users/IUserService.cs`

```csharp
using {ProjectName}.Application.Identity.Users.Password;

namespace {ProjectName}.Application.Identity.Users;

/// <summary>
/// Contract cho toàn bộ user management operations.
/// Implement bởi UserService (Infrastructure layer) - partial class chia thành 6 files.
/// </summary>
public interface IUserService : ITransientService
{
    // === Default Operations ===

    Task<PaginationResponse<UserDetailDto>> SearchAsync(
        UserParameterFilter filter,
        CancellationToken cancellationToken);

    Task<bool> ExistsWithNameAsync(string name);

    /// <summary>Check email đã tồn tại chưa. exceptId để exclude chính user đang update.</summary>
    Task<bool> ExistsWithEmailAsync(string email, string? exceptId = null);

    /// <summary>Check phone đã tồn tại chưa. exceptId để exclude chính user đang update.</summary>
    Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null);

    /// <summary>Trả về "FirstName LastName"</summary>
    Task<string> GetFullName(Guid userId);

    Task<List<UserDetailDto>> GetListAsync(CancellationToken cancellationToken);

    Task<int> GetCountAsync(CancellationToken cancellationToken);

    Task<UserDetailDto> GetAsync(string userId, CancellationToken cancellationToken);

    // === Create & Update Operations ===

    Task<string> CreateAsync(CreateUserRequest request, string origin);

    Task UpdateAsync(UpdateUserRequest request, string userId);

    Task ToggleStatusAsync(ToggleUserStatusRequest request, CancellationToken cancellationToken);

    // === Role Operations (implement trong BUILD_16B) ===

    Task<List<UserRoleDto>> GetRolesAsync(string userId, CancellationToken cancellationToken);

    Task<string> AssignRolesAsync(
        string userId,
        UserRolesRequest request,
        CancellationToken cancellationToken);

    // === Permission Operations (implement trong BUILD_16C) ===

    Task<List<string>> GetPermissionsAsync(string userId, CancellationToken cancellationToken);

    Task<bool> HasPermissionAsync(
        string userId,
        string permission,
        CancellationToken cancellationToken = default);

    // === Email Confirmation ===

    Task<string> ConfirmEmailAsync(string userId, string code, CancellationToken cancellationToken);

    Task<string> ConfirmPhoneNumberAsync(string userId, string code);

    // === Password Operations ===

    Task<string> ForgotPasswordAsync(ForgotPasswordRequest request, string origin);

    Task<string> ResetPasswordAsync(ResetPasswordRequest request);

    Task ChangePasswordAsync(ChangePasswordRequest request, string userId);
}
```

**Giải thích:**
- **`ITransientService`:** Marker interface để auto-registration (quy ước của project, xem BUILD_08)
- **`exceptId` pattern:** Dùng trong validators khi update - `ExistsWithEmailAsync(email, user.Id)` bỏ qua chính record đang edit
- **Partial class:** Implementation sẽ chia thành 6 files: `UserService.cs`, `UserService.CreateUpdate.cs`, `UserService.Confirm.cs`, `UserService.Password.cs`, `UserService.Role.cs`, `UserService.Permission.cs`

---

## 5. User Service Implementation

> 📌 **Partial Class Pattern:** `UserService` được chia thành nhiều files theo nhóm chức năng. Compiler gộp lại thành một class duy nhất khi build.

### Bước 5.1: UserService.cs - Main Class & Default Operations

**Làm gì:** Constructor chứa toàn bộ dependencies. Implement Search, Get, Exists, Toggle operations.

**File:** `src/Infrastructure/Identity/UserService.cs`

```csharp
using Ardalis.Specification;
using Ardalis.Specification.EntityFrameworkCore;
using {ProjectName}.Application.Common.Caching;
using {ProjectName}.Application.Common.Events;
using {ProjectName}.Application.Common.Exceptions;
using {ProjectName}.Application.Common.FileStorage;
using {ProjectName}.Application.Common.Interfaces;
using {ProjectName}.Application.Common.Mailing;
using {ProjectName}.Application.Common.Models;
using {ProjectName}.Application.Common.Specification;
using {ProjectName}.Application.Identity.Users;
using {ProjectName}.Domain.Identity;
using {ProjectName}.Infrastructure.Auth;
using {ProjectName}.Infrastructure.Persistence.Context;
using {ProjectName}.Shared.Authorization;
using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace {ProjectName}.Infrastructure.Identity;

/// <summary>
/// UserService - Main class chứa constructor và Default Operations.
/// Partial class: implementation được chia thành nhiều files.
/// </summary>
internal partial class UserService : IUserService
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;
    private readonly ApplicationDbContext _db;
    private readonly IJobService _jobService;
    private readonly IMailService _mailService;
    private readonly SecuritySettings _securitySettings;
    private readonly IEmailTemplateService _templateService;
    private readonly IFileStorageService _fileStorage;
    private readonly IEventPublisher _events;
    private readonly ICacheService _cache;

    public UserService(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        ApplicationDbContext db,
        IJobService jobService,
        IMailService mailService,
        IOptions<SecuritySettings> securitySettings,
        IEmailTemplateService templateService,
        IFileStorageService fileStorage,
        IEventPublisher events,
        ICacheService cache)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager;
        _db = db;
        _jobService = jobService;
        _mailService = mailService;
        _securitySettings = securitySettings.Value;
        _templateService = templateService;
        _fileStorage = fileStorage;
        _events = events;
        _cache = cache;
    }

    public async Task<PaginationResponse<UserDetailDto>> SearchAsync(
        UserParameterFilter filter,
        CancellationToken cancellationToken)
    {
        var spec = new EntitiesByPaginationFilterSpec<ApplicationUser>(filter);

        // ProjectToType: Mapster projection - chỉ SELECT fields có trong UserDetailDto
        // Tránh load toàn bộ ApplicationUser (có nhiều fields nhạy cảm như PasswordHash)
        var users = await _userManager.Users
            .WithSpecification(spec)
            .ProjectToType<UserDetailDto>()
            .ToListAsync(cancellationToken);

        int count = await _userManager.Users.CountAsync(cancellationToken);

        return new PaginationResponse<UserDetailDto>(users, count, filter.PageNumber, filter.PageSize);
    }

    public async Task<bool> ExistsWithNameAsync(string name)
    {
        return await _userManager.FindByNameAsync(name) is not null;
    }

    public async Task<bool> ExistsWithEmailAsync(string email, string? exceptId = null)
    {
        // Normalize email trước khi tìm (lowercase, trim)
        return await _userManager.FindByEmailAsync(email.Normalize()) is ApplicationUser user
            && user.Id != exceptId;
    }

    public async Task<bool> ExistsWithPhoneNumberAsync(string phoneNumber, string? exceptId = null)
    {
        return await _userManager.Users
            .FirstOrDefaultAsync(x => x.PhoneNumber == phoneNumber) is ApplicationUser user
            && user.Id != exceptId;
    }

    public async Task<string> GetFullName(Guid userId)
    {
        var user = await GetAsync(userId.ToString(), CancellationToken.None);
        return string.Join(" ", user.FirstName, user.LastName);
    }

    public async Task<List<UserDetailDto>> GetListAsync(CancellationToken cancellationToken) =>
        (await _userManager.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken))
        .Adapt<List<UserDetailDto>>();

    public Task<int> GetCountAsync(CancellationToken cancellationToken) =>
        _userManager.Users.AsNoTracking().CountAsync(cancellationToken);

    public async Task<UserDetailDto> GetAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await _userManager.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new NotFoundException("User Not Found.");

        return user.Adapt<UserDetailDto>();
    }

    public async Task ToggleStatusAsync(
        ToggleUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _userManager.Users
            .Where(u => u.Id == request.UserId)
            .FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new NotFoundException("User Not Found.");

        // Admin account không được toggle - phải luôn có ít nhất 1 admin active
        bool isAdmin = await _userManager.IsInRoleAsync(user, {ProjectName}Roles.Admin);
        if (isAdmin)
        {
            throw new ConflictException("Administrators Profile's Status cannot be toggled");
        }

        user.IsActive = request.ActivateUser;
        await _userManager.UpdateAsync(user);
    }
}
```

**Giải thích các design decisions:**
- **Tại sao inject tất cả dependencies vào main class:** Partial class chia sẻ fields - tất cả files đều access được `_userManager`, `_mailService`, etc.
- **`ProjectToType<UserDetailDto>()`:** Mapster projection trực tiếp trong LINQ query - SQL chỉ SELECT đúng columns cần thiết
- **`email.Normalize()`:** Identity normalize email về lowercase trước khi lưu DB - phải normalize khi tìm để match đúng

---

### Bước 5.2: UserService.CreateUpdate.cs

**Làm gì:** Implement `CreateAsync` và `UpdateAsync`.

**File:** `src/Infrastructure/Identity/UserService.CreateUpdate.cs`

```csharp
using {ProjectName}.Application.Common.Exceptions;
using {ProjectName}.Application.Common.Mailing;
using {ProjectName}.Application.Identity.Users;
using {ProjectName}.Domain.Common;
using {ProjectName}.Domain.Identity;
using {ProjectName}.Shared.Authorization;

namespace {ProjectName}.Infrastructure.Identity;

internal partial class UserService
{
    public async Task<string> CreateAsync(CreateUserRequest request, string origin)
    {
        var user = new ApplicationUser
        {
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            UserName = request.UserName,
            PhoneNumber = request.PhoneNumber,
            IsActive = true
        };

        // Identity tự hash password, validate theo PasswordOptions trong Startup
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            throw new InternalServerException("Validation Errors Occurred.", result.GetErrors());
        }

        // Tất cả user mới đều có role "Basic" mặc định
        await _userManager.AddToRoleAsync(user, {ProjectName}Roles.Basic);

        var messages = new List<string> { string.Format("User {0} Registered.", user.UserName) };

        // Gửi email xác nhận nếu SecuritySettings.RequireConfirmedAccount = true
        if (_securitySettings.RequireConfirmedAccount && !string.IsNullOrEmpty(user.Email))
        {
            // Tạo verification link với token được encode Base64Url
            string emailVerificationUri = await GetEmailVerificationUriAsync(user, origin);
            RegisterUserEmailModel eMailModel = new RegisterUserEmailModel()
            {
                Email = user.Email,
                UserName = user.UserName,
                Url = emailVerificationUri
            };
            var mailRequest = new MailRequest(
                new List<string> { user.Email },
                "Confirm Registration",
                _templateService.GenerateEmailTemplate("email-confirmation", eMailModel));

            // Enqueue background job để không block response
            _jobService.Enqueue(() => _mailService.SendAsync(mailRequest, CancellationToken.None));
            messages.Add($"Please check {user.Email} to verify your account!");
        }

        return string.Join(Environment.NewLine, messages);
    }

    public async Task UpdateAsync(UpdateUserRequest request, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        _ = user ?? throw new NotFoundException("User Not Found.");

        // Xử lý avatar upload / delete
        string currentImage = user.ImageUrl ?? string.Empty;
        if (request.Image != null || request.DeleteCurrentImage)
        {
            // FileStorageService tự xử lý lưu file và trả về relative path
            user.ImageUrl = await _fileStorage.UploadAsync<ApplicationUser>(request.Image, FileType.Image);
            if (request.DeleteCurrentImage && !string.IsNullOrEmpty(currentImage))
            {
                string root = Directory.GetCurrentDirectory();
                _fileStorage.Remove(Path.Combine(root, currentImage));
            }
        }

        // Update basic info trực tiếp trên entity
        user.FirstName = request.FirstName;
        user.LastName = request.LastName;
        user.PhoneNumber = request.PhoneNumber;

        // SetPhoneNumberAsync: Identity method - update phone với proper token generation
        string? phoneNumber = await _userManager.GetPhoneNumberAsync(user);
        if (request.PhoneNumber != phoneNumber)
        {
            await _userManager.SetPhoneNumberAsync(user, request.PhoneNumber);
        }

        var result = await _userManager.UpdateAsync(user);

        // RefreshSignInAsync: update security stamp và claims trong current session
        await _signInManager.RefreshSignInAsync(user);

        if (!result.Succeeded)
        {
            throw new InternalServerException("Update profile failed", result.GetErrors());
        }
    }
}
```

**Giải thích:**
- **`_jobService.Enqueue()`:** Background job (Hangfire - BUILD_24) - email gửi async, không block HTTP response
- **`_templateService.GenerateEmailTemplate()`:** Render HTML email từ template file (BUILD_23)
- **`GetEmailVerificationUriAsync()`:** Private helper method trong `UserService.Confirm.cs`
- **`FileType.Image`:** Enum trong Application Common để phân loại file upload
- **Email không update trong `UpdateAsync`:** Email là identity field - thay đổi cần confirm email mới (security best practice)

---

### Bước 5.3: UserService.Confirm.cs

**Làm gì:** Implement email/phone confirmation và helper tạo verification URI.

**File:** `src/Infrastructure/Identity/UserService.Confirm.cs`

```csharp
using System.Text;
using {ProjectName}.Application.Common.Exceptions;
using {ProjectName}.Domain.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;

namespace {ProjectName}.Infrastructure.Identity;

internal partial class UserService
{
    /// <summary>
    /// Private helper: Tạo email verification link với token được encode Base64Url.
    /// URI format: {origin}/api/users/confirm-email?userId=...&code=...
    /// </summary>
    private async Task<string> GetEmailVerificationUriAsync(ApplicationUser user, string origin)
    {
        // Generate Identity email confirmation token
        string code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

        // Encode token để truyền qua query string an toàn
        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));

        const string route = "api/users/confirm-email/";
        var endpointUri = new Uri(string.Concat($"{origin}/", route));

        // Build URL với query params: ?userId=...&code=...
        string verificationUri = QueryHelpers.AddQueryString(endpointUri.ToString(), "userId", user.Id);
        verificationUri = QueryHelpers.AddQueryString(verificationUri, "code", code);

        return verificationUri;
    }

    public async Task<string> ConfirmEmailAsync(
        string userId,
        string code,
        CancellationToken cancellationToken)
    {
        // Chỉ query users chưa confirm email - tránh confirm lại lần 2
        var user = await _userManager.Users
            .Where(u => u.Id == userId && !u.EmailConfirmed)
            .FirstOrDefaultAsync(cancellationToken);

        _ = user ?? throw new InternalServerException("An error occurred while confirming E-Mail.");

        // Decode Base64Url token về string gốc trước khi verify
        code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
        var result = await _userManager.ConfirmEmailAsync(user, code);

        return result.Succeeded
            ? string.Format(
                "Account Confirmed for E-Mail {0}. You can now use the /api/tokens endpoint to generate JWT.",
                user.Email)
            : throw new InternalServerException(
                string.Format("An error occurred while confirming {0}", user.Email));
    }

    public async Task<string> ConfirmPhoneNumberAsync(string userId, string code)
    {
        var user = await _userManager.FindByIdAsync(userId);

        _ = user ?? throw new InternalServerException("An error occurred while confirming Mobile Phone.");

        if (string.IsNullOrEmpty(user.PhoneNumber))
            throw new InternalServerException("An error occurred while confirming Mobile Phone.");

        var result = await _userManager.ChangePhoneNumberAsync(user, user.PhoneNumber, code);

        return result.Succeeded
            ? user.PhoneNumberConfirmed
                ? string.Format(
                    "Account Confirmed for Phone Number {0}. You can now use the /api/tokens endpoint to generate JWT.",
                    user.PhoneNumber)
                : string.Format(
                    "Account Confirmed for Phone Number {0}. You should confirm your E-mail before using the /api/tokens endpoint to generate JWT.",
                    user.PhoneNumber)
            : throw new InternalServerException(
                string.Format("An error occurred while confirming {0}", user.PhoneNumber));
    }
}
```

**Giải thích:**
- **`!u.EmailConfirmed` trong query:** Bảo vệ khỏi replay attack - link confirm email chỉ dùng được 1 lần
- **Base64Url encode/decode:** Cần thiết vì Identity token chứa ký tự đặc biệt không safe cho URL query string
- **`PhoneNumberConfirmed` check:** Return message khác nhau tùy trạng thái xác nhận email

---

### Bước 5.4: UserService.Password.cs

**Làm gì:** Implement forgot password, reset password, change password.

**File:** `src/Infrastructure/Identity/UserService.Password.cs`

```csharp
using {ProjectName}.Application.Common.Exceptions;
using {ProjectName}.Application.Common.Mailing;
using {ProjectName}.Application.Identity.Users.Password;
using Microsoft.AspNetCore.WebUtilities;

namespace {ProjectName}.Infrastructure.Identity;

internal partial class UserService
{
    public async Task<string> ForgotPasswordAsync(ForgotPasswordRequest request, string origin)
    {
        var user = await _userManager.FindByEmailAsync(request.Email.Normalize());

        // Không reveal thông tin user không tồn tại hoặc chưa confirm email (security best practice)
        if (user is null || !await _userManager.IsEmailConfirmedAsync(user))
        {
            throw new InternalServerException("An Error has occurred!");
        }

        // Generate password reset token
        string code = await _userManager.GeneratePasswordResetTokenAsync(user);

        // Build reset URL: {origin}/account/reset-password?Token=...
        const string route = "account/reset-password";
        var endpointUri = new Uri(string.Concat($"{origin}/", route));
        string passwordResetUrl = QueryHelpers.AddQueryString(endpointUri.ToString(), "Token", code);

        var mailRequest = new MailRequest(
            new List<string> { request.Email },
            "Reset Password",
            $"Your Password Reset Token is '{code}'. You can reset your password using the {endpointUri} Endpoint.");

        // Background job để gửi email async
        _jobService.Enqueue(() => _mailService.SendAsync(mailRequest, CancellationToken.None));

        return "Password Reset Mail has been sent to your authorized Email.";
    }

    public async Task<string> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email?.Normalize()!);

        // Không reveal user không tồn tại
        _ = user ?? throw new InternalServerException("An Error has occurred!");

        var result = await _userManager.ResetPasswordAsync(user, request.Token!, request.Password!);

        return result.Succeeded
            ? "Password Reset Successful!"
            : throw new InternalServerException("An Error has occurred!");
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest model, string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);

        _ = user ?? throw new NotFoundException("User Not Found.");

        var result = await _userManager.ChangePasswordAsync(user, model.Password, model.NewPassword);

        if (!result.Succeeded)
        {
            throw new InternalServerException("Change password failed", result.GetErrors());
        }
    }
}
```

**Giải thích:**
- **Security by obscurity:** `ForgotPasswordAsync` không reveal user tồn tại hay không - tránh email enumeration attack
- **`IsEmailConfirmedAsync`:** Chỉ gửi reset email cho accounts đã confirm email - ngăn abuse
- **`ChangePasswordAsync`:** Yêu cầu `model.Password` (current password) để verify trước khi đổi - phòng account hijack

---

## 6. Users Controller

### Bước 6.1: UsersController

**Làm gì:** HTTP endpoints cho user management, kế thừa từ `BaseApiController`.

**Tại sao kế thừa `BaseApiController`:**
- Tự động có `[ApiController]` và `[Route("api/[controller]")]`
- Có `Mediator` property (cho CQRS nếu cần)
- Tất cả controllers trong project đồng nhất behavior

**File:** `src/Host/Controllers/Identity/UsersController.cs`

```csharp
using {ProjectName}.Application.Identity.Users;
using {ProjectName}.Application.Identity.Users.Password;
using NSwag.Annotations;

namespace {ProjectName}.Host.Controllers.Identity;

/// <summary>
/// Controller quản lý user accounts.
/// Kế thừa BaseApiController: tự có [ApiController], [Route("api/[controller]")]
/// </summary>
public class UsersController : BaseApiController
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService) => _userService = userService;

    /// <summary>Lấy danh sách tất cả users (không pagination)</summary>
    [HttpGet("list")]
    [OpenApiOperation("Get list of all users.", "")]
    public Task<List<UserDetailDto>> GetListAsync(CancellationToken cancellationToken)
    {
        return _userService.GetListAsync(cancellationToken);
    }

    /// <summary>Lấy thông tin chi tiết user theo ID</summary>
    [HttpGet("{id}")]
    [OpenApiOperation("Get a user's details.", "")]
    public Task<UserDetailDto> GetByIdAsync(string id, CancellationToken cancellationToken)
    {
        return _userService.GetAsync(id, cancellationToken);
    }

    /// <summary>Lấy danh sách roles của user</summary>
    [HttpGet("{id}/roles")]
    [OpenApiOperation("Get a user's roles.", "")]
    public Task<List<UserRoleDto>> GetRolesAsync(string id, CancellationToken cancellationToken)
    {
        return _userService.GetRolesAsync(id, cancellationToken);
    }

    /// <summary>Cập nhật roles cho user</summary>
    [HttpPost("{id}/roles")]
    [OpenApiOperation("Update a user's assigned roles.", "")]
    public Task<string> AssignRolesAsync(string id, UserRolesRequest request, CancellationToken cancellationToken)
    {
        return _userService.AssignRolesAsync(id, request, cancellationToken);
    }

    /// <summary>Admin tạo user mới</summary>
    [HttpPost("create")]
    [OpenApiOperation("Creates a new user.", "")]
    public Task<string> CreateAsync(CreateUserRequest request)
    {
        return _userService.CreateAsync(request, GetOriginFromRequest());
    }

    /// <summary>Anonymous user tự đăng ký tài khoản</summary>
    [HttpPost("self-register")]
    [AllowAnonymous]
    [OpenApiOperation("Anonymous user creates a user.", "")]
    public Task<string> SelfRegisterAsync(CreateUserRequest request)
    {
        return _userService.CreateAsync(request, GetOriginFromRequest());
    }

    /// <summary>
    /// Toggle trạng thái active của user.
    /// Route ID và request.UserId phải khớp để tránh IDOR (Insecure Direct Object Reference).
    /// </summary>
    [HttpPost("{id}/toggle-status")]
    [OpenApiOperation("Toggle a user's active status.", "")]
    public async Task<ActionResult> ToggleStatusAsync(
        string id,
        ToggleUserStatusRequest request,
        CancellationToken cancellationToken)
    {
        // Validate ID match - phòng IDOR attack
        if (id != request.UserId)
        {
            return BadRequest();
        }

        await _userService.ToggleStatusAsync(request, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Xác nhận email qua link được gửi sau khi đăng ký.
    /// Anonymous - không cần login.
    /// </summary>
    [HttpGet("confirm-email")]
    [AllowAnonymous]
    [OpenApiOperation("Confirm email address for a user.", "")]
    public Task<string> ConfirmEmailAsync(
        [FromQuery] string userId,
        [FromQuery] string code,
        CancellationToken cancellationToken)
    {
        return _userService.ConfirmEmailAsync(userId, code, cancellationToken);
    }

    /// <summary>Xác nhận số điện thoại. Anonymous - không cần login.</summary>
    [HttpGet("confirm-phone-number")]
    [AllowAnonymous]
    [OpenApiOperation("Confirm phone number for a user.", "")]
    public Task<string> ConfirmPhoneNumberAsync([FromQuery] string userId, [FromQuery] string code)
    {
        return _userService.ConfirmPhoneNumberAsync(userId, code);
    }

    /// <summary>Gửi email reset password. Anonymous - không cần login.</summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [OpenApiOperation("Request a password reset email for a user.", "")]
    public Task<string> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        return _userService.ForgotPasswordAsync(request, GetOriginFromRequest());
    }

    /// <summary>Đặt lại password với token từ email.</summary>
    [HttpPost("reset-password")]
    [OpenApiOperation("Reset a user's password.", "")]
    public Task<string> ResetPasswordAsync(ResetPasswordRequest request)
    {
        return _userService.ResetPasswordAsync(request);
    }

    /// <summary>
    /// GetOriginFromRequest: Helper tạo origin URL từ request.
    /// Dùng để tạo absolute links trong email (confirmation, password reset).
    /// </summary>
    private string GetOriginFromRequest() =>
        $"{Request.Scheme}://{Request.Host.Value}{Request.PathBase.Value}";
}
```

**Giải thích:**
- **Trả về `Task<T>` thay vì `IActionResult`:** Project pattern - `BaseApiController` có `[ApiController]` nên ASP.NET Core tự wrap response thành 200 OK với body là T
- **`[AllowAnonymous]`:** Chỉ dùng cho: `self-register`, `confirm-email`, `confirm-phone-number`, `forgot-password` - các actions không cần JWT
- **IDOR check trong `ToggleStatusAsync`:** `if (id != request.UserId) return BadRequest()` - phòng tấn công thay đổi `UserId` trong request body
- **`GetOriginFromRequest()`:** Build URL gốc (`https://api.example.com`) để làm base cho email verification links
- **`[OpenApiOperation]`:** NSwag annotation để generate Swagger documentation

---

## 7. DI Registration

### Bước 7.1: Identity Startup

**Làm gì:** Register ASP.NET Core Identity với các options phù hợp dự án.

**File:** `src/Infrastructure/Identity/Startup.cs`

```csharp
using {ProjectName}.Domain.Identity;
using {ProjectName}.Infrastructure.Persistence.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace {ProjectName}.Infrastructure.Identity;

internal static class Startup
{
    internal static IServiceCollection AddIdentity(this IServiceCollection services) =>
        services
            .AddIdentity<ApplicationUser, ApplicationRole>(options =>
            {
                // Password policy: đơn giản để user dễ sử dụng, min 6 ký tự
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                // Email phải unique trong hệ thống
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders() // Cần cho email confirmation và password reset tokens
            .Services;
}
```

**Giải thích:**
- **`AddEntityFrameworkStores<ApplicationDbContext>()`:** Identity dùng EF Core để persist users, roles, claims, tokens
- **`AddDefaultTokenProviders()`:** Cần thiết cho `GenerateEmailConfirmationTokenAsync()` và `GeneratePasswordResetTokenAsync()`
- **`UserService` auto-registration:** `IUserService : ITransientService` → được scan và register tự động bởi infrastructure startup (xem BUILD_08)
- **`ITransientService`:** Mỗi request tạo 1 instance mới - phù hợp vì UserService không giữ state

---

## 8. Testing

### Bước 8.1: Test Self-Register

```bash
curl -X POST https://localhost:7001/api/users/self-register \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "Nguyen",
    "lastName": "Van A",
    "email": "nguyenvana@example.com",
    "userName": "nguyenvana",
    "password": "123456",
    "confirmPassword": "123456",
    "phoneNumber": "+84987654321"
  }'
```

**Expected Response (RequireConfirmedAccount = false):**
```text
User nguyenvana Registered.
```

**Expected Response (RequireConfirmedAccount = true):**
```text
User nguyenvana Registered.
Please check nguyenvana@example.com to verify your account!
```

---

### Bước 8.2: Test Confirm Email

**Link được gửi qua email có dạng:**
```
https://localhost:7001/api/users/confirm-email?userId=abc123&code=CfDJ8...
```

```bash
curl -X GET "https://localhost:7001/api/users/confirm-email?userId=<user_id>&code=<encoded_code>"
```

**Expected Response:**
```text
Account Confirmed for E-Mail nguyenvana@example.com. You can now use the /api/tokens endpoint to generate JWT.
```

---

### Bước 8.3: Test Toggle User Status (Admin)

```bash
curl -X POST https://localhost:7001/api/users/<user_id>/toggle-status \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <admin_jwt_token>" \
  -d '{
    "userId": "<user_id>",
    "activateUser": false
  }'
```

**Expected Response:** `200 OK`

**Error Cases:**
- `user_id` trong route ≠ `userId` trong body → `400 Bad Request`
- Target là Admin account → `409 Conflict: Administrators Profile's Status cannot be toggled`

---

### Bước 8.4: Test Forgot Password

```bash
curl -X POST https://localhost:7001/api/users/forgot-password \
  -H "Content-Type: application/json" \
  -d '{ "email": "nguyenvana@example.com" }'
```

**Expected Response:**
```text
Password Reset Mail has been sent to your authorized Email.
```

**⚠️ Lưu ý:** Cả user không tồn tại và user chưa confirm email đều return lỗi chung `"An Error has occurred!"` - không reveal thông tin nội bộ.

---

## 9. Summary

### ✅ Đã hoàn thành trong bước này:

**Application Layer (`src/Application/Identity/Users/`):**
- ✅ `UserDetailDto` - Response DTO, mapped từ ApplicationUser bởi Mapster
- ✅ `CreateUserRequest` + Validator (async uniqueness checks)
- ✅ `UpdateUserRequest` + Validator (exceptId pattern)
- ✅ `ToggleUserStatusRequest`
- ✅ `UserParameterFilter : PaginationFilter` (IsActive filter)
- ✅ `UserRoleDto`          (dùng trong BUILD_16B)
- ✅ `UserRolesRequest`     (dùng trong BUILD_16B)
- ✅ `RegisterUserEmailModel`
- ✅ `IUserService : ITransientService` - full interface

**Infrastructure Layer (`src/Infrastructure/Identity/`):**
- ✅ `UserService.cs` - Main + Default Operations (Search, Get, Exists, Toggle)
- ✅ `UserService.CreateUpdate.cs` - Create (với email confirmation flow) + Update (với avatar upload)
- ✅ `UserService.Confirm.cs` - Email/Phone confirmation + GetEmailVerificationUri helper
- ✅ `UserService.Password.cs` - Forgot, Reset, Change password
- ✅ `UserService.Role.cs`         (BUILD_16B)
- ✅ `UserService.Permission.cs`   (BUILD_16C)
- ✅ `Identity/Startup.cs` - ASP.NET Core Identity registration

**Host Layer (`src/Host/Controllers/Identity/`):**
- ✅ `UsersController : BaseApiController` - 10 endpoints đầy đủ

### 📊 Architecture Diagram:

```
HTTP Request
     │
     ▼
UsersController (Host)              [AllowAnonymous] cho: self-register, confirm-email, forgot-password
     │  IUserService (DI)
     ▼
UserService (Infrastructure)
  ├── UserService.cs              → Search, Get, Exists, Toggle
  ├── UserService.CreateUpdate.cs → Create (+ email job), Update (+ avatar upload)
  ├── UserService.Confirm.cs      → ConfirmEmail, ConfirmPhone, GetEmailVerificationUri
  ├── UserService.Password.cs     → ForgotPassword, ResetPassword, ChangePassword
  ├── UserService.Role.cs         → GetRoles, AssignRoles (BUILD_16B)
  └── UserService.Permission.cs   → GetPermissions, HasPermission (BUILD_16C)
         │
         ├── UserManager<ApplicationUser>  (ASP.NET Core Identity)
         ├── IJobService                   (Hangfire - BUILD_24)
         ├── IMailService                  (BUILD_23)
         ├── IFileStorageService           (BUILD_20)
         └── ICacheService                 (BUILD_19)
```

### 📌 Key Concepts:

**Partial Class (Lớp bộ phận):**
- Một class chia thành nhiều `.cs` files, compiler gộp lại khi build
- Constructor và tất cả `private` fields chỉ cần khai báo một lần trong `UserService.cs`
- Mọi partial file đều access được `_userManager`, `_mailService`, etc.

**ExceptId Pattern:**
- `ExistsWithEmailAsync(email, user.Id)` → query: `WHERE Email = @email AND Id != @exceptId`
- Cho phép user update profile mà giữ nguyên email/phone của họ

**Security Best Practices trong codebase:**
- `ForgotPasswordAsync`: Không reveal user tồn tại hay không
- `ConfirmEmailAsync`: Query `!u.EmailConfirmed` để tránh replay attack
- `ToggleStatusAsync`: IDOR check (route `id` == request `UserId`)
- `GetOriginFromRequest()`: Build origin từ request thực tế thay vì hardcode

### 📁 File Structure:

```
src/
├── Application/
│   └── Identity/
│       └── Users/
│           ├── UserDetailDto.cs
│           ├── CreateUserRequest.cs
│           ├── UpdateUserRequest.cs
│           ├── ToggleUserStatusRequest.cs
│           ├── UserParameterFilter.cs
│           ├── UserRoleDto.cs          (dùng trong BUILD_16B)
│           ├── UserRolesRequest.cs     (dùng trong BUILD_16B)
│           ├── RegisterUserEmailModel.cs
│           ├── IUserService.cs
│           └── Password/
│               ├── ForgotPasswordRequest.cs
│               ├── ResetPasswordRequest.cs
│               └── ChangePasswordRequest.cs
├── Infrastructure/
│   └── Identity/
│       ├── UserService.cs
│       ├── UserService.CreateUpdate.cs
│       ├── UserService.Confirm.cs
│       ├── UserService.Password.cs
│       ├── UserService.Role.cs         (BUILD_16B)
│       ├── UserService.Permission.cs   (BUILD_16C)
│       ├── IdentityResultExtensions.cs
│       └── Startup.cs
└── Host/
    └── Controllers/
        └── Identity/
            └── UsersController.cs
```

---

## 10. Next Steps

**Tiếp theo:** [BUILD_16B - User Role Management](BUILD_16B_User_Roles.md)

Trong bước tiếp theo, chúng ta sẽ:
1. ✅ Tạo `UserRoleDto`, `UserRolesRequest` DTOs
2. ✅ Implement `UserService.Role.cs` - `GetRolesAsync`, `AssignRolesAsync`
3. ✅ Hiểu business rule: System phải có tối thiểu 2 Admins để toggle Admin role

---
