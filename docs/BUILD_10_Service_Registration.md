# BUILD_10 - Service Registration Pattern

> 📚 [Quay lại Mục lục](BUILD_INDEX.md)  
> 📋 **Prerequisites:** BUILD_09 (Domain Base Entities) đã hoàn thành  
> ⏱️ **Thời gian:** Khoảng 15 phút

---

## 1. Overview

**Làm gì:** Setup auto-registration pattern cho services với marker interfaces.

**Tại sao cần:**
- **Automation:** Tự động đăng ký services, không cần manual registration
- **Convention-based:** Services follow convention → auto-discovered
- **Maintainability:** Thêm service mới không cần update Startup
- **Type-safe:** Compile-time safety với interfaces

**Trong bước này chúng ta sẽ:**
- ✅ Tạo marker interfaces (ITransientService, IScopedService)
- ✅ Tạo AddServices extension method
- ✅ Implement auto-registration logic
- ✅ Setup trong Infrastructure Startup

---

## 2. Understanding Service Lifetimes

**Transient:**
```csharp
services.AddTransient<IService, Service>();
```
- Tạo instance mới mỗi lần request
- Use for: Stateless services, lightweight operations
- Examples: Validators, Formatters, Calculators

**Scoped:**
```csharp
services.AddScoped<IService, Service>();
```
- Tạo instance mới mỗi HTTP request
- Shared trong cùng request
- Use for: Services with request-specific state
- Examples: DbContext, Current User Service, Repository

**Singleton:**
```csharp
services.AddSingleton<IService, Service>();
```
- Tạo instance duy nhất cho toàn app lifetime
- Shared across all requests
- Use for: Configuration, Caching, Logging
- Examples: IConfiguration, IMemoryCache, ILogger

---

## 3. Tạo Marker Interfaces

### Bước 3.1: ITransientService

**File:** `src/Application/Common/Interfaces/ITransientService.cs`

```csharp
namespace {ProjectName}.Application.Common.Interfaces;

/// <summary>
/// Marker interface for transient services.
/// Services implementing this will be registered with Transient lifetime.
/// </summary>
public interface ITransientService
{
}
```

---

### Bước 3.2: IScopedService

**File:** `src/Application/Common/Interfaces/IScopedService.cs`

```csharp
namespace {ProjectName}.Application.Common.Interfaces;

/// <summary>
/// Marker interface for scoped services.
/// Services implementing this will be registered with Scoped lifetime.
/// </summary>
public interface IScopedService
{
}
```

**Marker Interfaces:**
- Không có methods hay properties
- Chỉ dùng để "đánh dấu" service lifetime
- Convention: Service implements business interface + marker interface

---

## 4. Tạo AddServices Extension Method

**File:** `src/Infrastructure/Common/Startup.cs`

```csharp
using {ProjectName}.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace {ProjectName}.Infrastructure.Common;

internal static class Startup
{
    internal static IServiceCollection AddServices(this IServiceCollection services) =>
        services
            .AddServices(typeof(ITransientService), ServiceLifetime.Transient)
            .AddServices(typeof(IScopedService), ServiceLifetime.Scoped);

    internal static IServiceCollection AddServices(this IServiceCollection services, Type interfaceType, ServiceLifetime lifetime)
    {
        var interfaceTypes =
            AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(t => interfaceType.IsAssignableFrom(t)
                            && t.IsClass && !t.IsAbstract)
                .Select(t => new
                {
                    Service = t.GetInterfaces().FirstOrDefault(),
                    Implementation = t
                })
                .Where(t => t.Service is not null
                            && interfaceType.IsAssignableFrom(t.Service));

        foreach (var type in interfaceTypes)
        {
            services.AddService(type.Service!, type.Implementation, lifetime);
        }

        return services;
    }

    internal static IServiceCollection AddService(this IServiceCollection services, Type serviceType, Type implementationType, ServiceLifetime lifetime) =>
        lifetime switch
        {
            ServiceLifetime.Transient => services.AddTransient(serviceType, implementationType),
            ServiceLifetime.Scoped => services.AddScoped(serviceType, implementationType),
            ServiceLifetime.Singleton => services.AddSingleton(serviceType, implementationType),
            _ => throw new ArgumentException("Invalid lifeTime", nameof(lifetime))
        };
}
```

**Key Logic:**

1. **Scan assemblies:** Get all types from loaded assemblies
2. **Filter classes:** Only concrete, non-abstract classes
3. **Check marker:** Must implement marker interface
4. **Get business interface:** Get first interface (exclude marker)
5. **Register:** Add to DI container with specified lifetime

**Why filter marker interfaces:**
```csharp
// ❌ Bad: Register with marker interface
services.AddTransient<ITransientService, ProductService>(); // Wrong!

// ✅ Good: Register with business interface
services.AddTransient<IProductService, ProductService>(); // Correct!
```

---

## 5. Setup trong Infrastructure

**Update:** `src/Infrastructure/Startup.cs`

```csharp
using {ProjectName}.Infrastructure.Common;
using {ProjectName}.Infrastructure.Persistence;
using {ProjectName}.Infrastructure.Persistence.Initialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {ProjectName}.Infrastructure;

public static class Startup
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        return services
            .AddPersistence()
            .AddRouting(options => options.LowercaseUrls = true)
            .AddServices(); // ⭐ Auto-register services
    }

    public static async Task InitializeDatabasesAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IDatabaseInitializer>()
            .InitializeDatabasesAsync(cancellationToken);
    }

    public static IApplicationBuilder UseInfrastructure(this IApplicationBuilder builder, IConfiguration config) =>
        builder
            .UseRouting()
            .UseHttpsRedirection()
            .UseAuthentication()
            .UseAuthorization();

    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder builder)
    {
        builder.MapControllers().RequireAuthorization();
        return builder;
    }
}
```

**Thứ tự registration:**
- Call `AddServices()` cuối cùng
- Đảm bảo tất cả dependencies (DbContext, etc.) đã được register trước



---

## 6. Ví dụ Sử dụng

### Bước 6.1: Transient Service Example

**File:** `src/Infrastructure/Services/EmailService.cs`

```csharp
using {ProjectName}.Application.Common.Interfaces;

namespace {ProjectName}.Infrastructure.Services;

public interface IEmailService
{
    Task SendAsync(string to, string subject, string body);
}

// ⭐ Implement business interface + marker interface
internal class EmailService : IEmailService, ITransientService
{
    public async Task SendAsync(string to, string subject, string body)
    {
     // Send email implementation
        await Task.CompletedTask;
    }
}
```

**Result:** Tự động đăng ký:
```csharp
services.AddTransient<IEmailService, EmailService>();
```

---

### Bước 6.2: Scoped Service Example

**File:** `src/Infrastructure/Services/CurrentUserService.cs`

```csharp
using {ProjectName}.Application.Common.Interfaces;
using Microsoft.AspNetCore.Http;

namespace {ProjectName}.Infrastructure.Services;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Email { get; }
}

// ⭐ Scoped per HTTP request
internal class CurrentUserService : ICurrentUserService, IScopedService
{
 private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
  {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId =>
        _httpContextAccessor.HttpContext?.User?.FindFirst("uid")?.Value;

    public string? Email =>
     _httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value;
}
```

**Result:** Tự động đăng ký:
```csharp
services.AddScoped<ICurrentUserService, CurrentUserService>();
```

---

### Bước 6.3: Multiple Interfaces Example

**File:** `src/Infrastructure/Services/ProductService.cs`

```csharp
using {ProjectName}.Application.Common.Interfaces;

namespace {ProjectName}.Infrastructure.Services;

public interface IProductService
{
    Task<ProductDto> GetByIdAsync(int id);
}

public interface IProductQueryService
{
    Task<List<ProductDto>> SearchAsync(String query);
}

// ⭐ Multiple business interfaces + marker
internal class ProductService : 
    IProductService,     // First interface → used for registration
    IProductQueryService, // Also implemented
    ITransientService         // Marker
{
    public async Task<ProductDto> GetByIdAsync(int id)
    {
        // Implementation
    return new ProductDto();
    }

    public async Task<List<ProductDto>> SearchAsync(string query)
    {
        // Implementation
        return new List<ProductDto>();
    }
}
```

**Result:** Registered với first business interface:
```csharp
services.AddTransient<IProductService, ProductService>();
```

**⚠️ Note:** Chỉ interface đầu tiên được registered. Nếu cần cả 2, phải manual register:
```csharp
services.AddTransient<IProductQueryService>(sp => 
    sp.GetRequiredService<IProductService>() as ProductService);
```

---


## 7. Best Practices

### Bước 7.1: Service Conventions

**✅ Good:**
```csharp
// Clear naming
public interface IEmailService { }
internal class EmailService : IEmailService, ITransientService { }

// Business interface first, marker last
internal class UserService : IUserService, IScopedService { }
```

**❌ Bad:**
```csharp
// Marker first (confusing)
internal class EmailService : ITransientService, IEmailService { }

// No interface
internal class EmailService : ITransientService { } // Won't be registered!
```

---

### Bước 7.2: When to Use Each Lifetime

**Transient:**
- ✅ Stateless services
- ✅ Lightweight operations
- ✅ No shared state
- Examples: Validators, Formatters, Calculators

**Scoped:**
- ✅ Per-request state
- ✅ Database operations (DbContext)
- ✅ Current user context
- Examples: Repositories, UnitOfWork, CurrentUserService

**Singleton:**
- ✅ Application-wide state
- ✅ Expensive to create
- ✅ Thread-safe
- Examples: Configuration, Caching, Logging

---
### Bước 7.3: Testing

**Unit Test Example:**
```csharp
[Fact]
public void AddServices_ShouldRegisterTransientServices()
{
    // Arrange
    var services = new ServiceCollection();

    // Act
    services.AddServices();

    // Assert
    var descriptor = services.FirstOrDefault(s => 
  s.ServiceType == typeof(IEmailService));
    
    Assert.NotNull(descriptor);
    Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    Assert.Equal(typeof(EmailService), descriptor.ImplementationType);
}
```

---

## 8. Common Issues

### Issue 1: "Service not registered"

**Nguyên nhân:** Service không implement cả business interface VÀ marker interface

**Giải pháp:**
```csharp
// ❌ Missing marker interface
internal class EmailService : IEmailService { }

// ✅ Include marker interface
internal class EmailService : IEmailService, ITransientService { }
```

---

### Issue 2: "Wrong interface registered"

**Nguyên nhân:** Marker interface ở vị trí đầu tiên

**Giải pháp:**
```csharp
// ❌ Marker first
internal class EmailService : ITransientService, IEmailService { }

// ✅ Business interface first
internal class EmailService : IEmailService, ITransientService { }
```

---

### Issue 3: "Multiple implementations conflict"

**Nguyên nhân:** Nhiều classes implement cùng interface

**Giải pháp:**
```csharp
// Option 1: Sử dụng named services (manual)
services.AddTransient<IEmailService, SmtpEmailService>();
services.AddTransient<IEmailService, SendGridEmailService>();

// Option 2: Factory pattern
services.AddTransient<IEmailServiceFactory>(sp => 
    new EmailServiceFactory(sp));
```

## 8. Summary

### ✅ Đã hoàn thành:

**Marker Interfaces:**
- ✅ ITransientService
- ✅ IScopedService

**Auto-Registration:**
- ✅ AddServices() extension method
- ✅ Assembly scanning logic
- ✅ Business interface detection

### 📊 Registration Flow:

```
Service implements IXxxService + ITransientService
    ↓
AddServices() scans assemblies
  ↓
Detects marker interface
    ↓
Gets business interface (IXxxService)
    ↓
Registers: services.AddTransient<IXxxService, XxxService>()
```

### 💡 Key Takeaways:

1. **Service must implement:** Business interface + Marker interface
2. **Business interface first:** For correct registration
3. **Marker is just a flag:** No methods, only for lifetime indication
4. **Transient for stateless:** Scoped for per-request state

### 📁 File Structure:

```
src/Application/Common/Interfaces/
├── ITransientService.cs
└── IScopedService.cs

src/Infrastructure/Common/
└── Startup.cs (AddServices extensions)
```

---

## 9. Bước tiếp theo

**Tiếp theo:** [BUILD_11 - Repository Pattern](BUILD_11_Repository_Pattern.md)

---

**Quay lại:** [Mục lục](BUILD_INDEX.md)
