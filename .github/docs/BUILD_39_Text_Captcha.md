# BUILD 39A - Text Captcha với ImageSharp

> 📚 [Quay lại Mục lục](BUILD_INDEX.md)  
> 📋 **Prerequisites:** Bước 10 (Service Registration) đã hoàn thành

Tài liệu này hướng dẫn xây dựng **Text Captcha** độc lập bằng `ImageSharp` để sinh ảnh captcha từ chuỗi ký tự ngẫu nhiên.

---

## 1. Overview

**Làm gì:** Tạo service sinh ảnh Text Captcha dạng base64 và xác thực đáp án người dùng.

**Tại sao cần:**
- **Chống bot form cơ bản:** đăng ký, liên hệ, quên mật khẩu.
- **Tích hợp nhanh:** chỉ cần frontend hiển thị ảnh base64.
- **Triển khai độc lập:** không phụ thuộc Google reCAPTCHA bên thứ ba.

**Trong bước này chúng ta sẽ:**
- ✅ Tạo contract `ITextCaptchaService` ở Application.
- ✅ Cài package `ImageSharp` trong Infrastructure.
- ✅ Implement `ImageSharpTextCaptchaService`.
- ✅ Lưu challenge tạm thời bằng in-memory store.
- ✅ Tích hợp vào luồng `RegisterUser`.

---

## 2. Add Required Packages

### Bước 2.1: Cài ImageSharp

**File:** `src/Infrastructure/Infrastructure.csproj`

```xml
<ItemGroup>
  <PackageReference Include="SixLabors.ImageSharp" Version="3.1.7" />
  <PackageReference Include="SixLabors.ImageSharp.Drawing" Version="2.1.5" />
  <PackageReference Include="SixLabors.Fonts" Version="1.0.0" />
</ItemGroup>
```

**Giải thích:**
- `SixLabors.ImageSharp`: xử lý ảnh cross-platform.
- `SixLabors.ImageSharp.Drawing`: vẽ text/noise lên ảnh.
- `SixLabors.Fonts`: xử lý font khi render ký tự captcha.

---

## 3. Application Layer

### Bước 3.1: DTO challenge

**File:** `src/Application/Common/Captcha/TextCaptchaChallenge.cs`

```csharp
namespace {ProjectName}.Application.Common.Captcha;

/// <summary>
/// Dữ liệu challenge cho Text Captcha
/// </summary>
public class TextCaptchaChallenge
{
    public string ChallengeId { get; set; } = default!;

    /// <summary>
    /// Ảnh PNG base64
    /// </summary>
    public string ImageBase64 { get; set; } = default!;

    public DateTime ExpiresAtUtc { get; set; }
}
```

### Bước 3.2: Contract service

**File:** `src/Application/Common/Captcha/ITextCaptchaService.cs`

```csharp
using {ProjectName}.Application.Common.Interfaces;

namespace {ProjectName}.Application.Common.Captcha;

/// <summary>
/// Service tạo và xác thực Text Captcha
/// </summary>
public interface ITextCaptchaService : ITransientService
{
    Task<TextCaptchaChallenge> CreateAsync(CancellationToken cancellationToken = default);

    Task<bool> ValidateAsync(string challengeId, string answer, CancellationToken cancellationToken = default);
}
```

---

## 4. Infrastructure Layer

### Bước 4.1: Settings

**File:** `src/Infrastructure/Captcha/TextCaptchaSettings.cs`

```csharp
namespace {ProjectName}.Infrastructure.Captcha;

public class TextCaptchaSettings
{
    public bool Enable { get; set; } = true;
    public int Length { get; set; } = 5;
    public int Width { get; set; } = 220;
    public int Height { get; set; } = 80;
    public int ExpiryMinutes { get; set; } = 2;
}
```

**File cấu hình:** `appsettings.json`

```json
{
  "TextCaptchaSettings": {
    "Enable": true,
    "Length": 5,
    "Width": 220,
    "Height": 80,
    "ExpiryMinutes": 2
  }
}
```

### Bước 4.2: In-memory store

**File:** `src/Infrastructure/Captcha/TextCaptchaStoreEntry.cs`

```csharp
namespace {ProjectName}.Infrastructure.Captcha;

internal class TextCaptchaStoreEntry
{
    public string ChallengeId { get; set; } = default!;
    public string ExpectedAnswer { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
}
```

**File:** `src/Infrastructure/Captcha/ITextCaptchaStore.cs`

```csharp
namespace {ProjectName}.Infrastructure.Captcha;

internal interface ITextCaptchaStore
{
    void Save(TextCaptchaStoreEntry entry);
    bool TryGet(string challengeId, out TextCaptchaStoreEntry? entry);
    void Remove(string challengeId);
}
```

**File:** `src/Infrastructure/Captcha/InMemoryTextCaptchaStore.cs`

```csharp
using System.Collections.Concurrent;

namespace {ProjectName}.Infrastructure.Captcha;

internal class InMemoryTextCaptchaStore : ITextCaptchaStore
{
    private static readonly ConcurrentDictionary<string, TextCaptchaStoreEntry> _entries = new();

    public void Save(TextCaptchaStoreEntry entry) => _entries[entry.ChallengeId] = entry;

    public bool TryGet(string challengeId, out TextCaptchaStoreEntry? entry)
    {
        if (_entries.TryGetValue(challengeId, out var found))
        {
            entry = found;
            return true;
        }

        entry = null;
        return false;
    }

    public void Remove(string challengeId) => _entries.TryRemove(challengeId, out _);
}
```

### Bước 4.3: Service ImageSharp

**File:** `src/Infrastructure/Captcha/ImageSharpTextCaptchaService.cs`

```csharp
using {ProjectName}.Application.Common.Captcha;
using Microsoft.Extensions.Options;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace {ProjectName}.Infrastructure.Captcha;

public class ImageSharpTextCaptchaService : ITextCaptchaService
{
    private readonly TextCaptchaSettings _settings;
    private readonly ITextCaptchaStore _store;

    public ImageSharpTextCaptchaService(IOptions<TextCaptchaSettings> settings, ITextCaptchaStore store)
    {
        _settings = settings.Value;
        _store = store;
    }

    public Task<TextCaptchaChallenge> CreateAsync(CancellationToken cancellationToken = default)
    {
        var code = GenerateCode(_settings.Length);
        var challengeId = Guid.NewGuid().ToString("N");
        var expires = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        _store.Save(new TextCaptchaStoreEntry
        {
            ChallengeId = challengeId,
            ExpectedAnswer = code,
            ExpiresAtUtc = expires
        });

        return Task.FromResult(new TextCaptchaChallenge
        {
            ChallengeId = challengeId,
            ImageBase64 = RenderImageBase64(code, _settings.Width, _settings.Height),
            ExpiresAtUtc = expires
        });
    }

    public Task<bool> ValidateAsync(string challengeId, string answer, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enable)
        {
            return Task.FromResult(true);
        }

        if (!_store.TryGet(challengeId, out var entry) || entry is null)
        {
            return Task.FromResult(false);
        }

        if (entry.ExpiresAtUtc < DateTime.UtcNow)
        {
            _store.Remove(challengeId);
            return Task.FromResult(false);
        }

        var valid = string.Equals(entry.ExpectedAnswer, answer, StringComparison.OrdinalIgnoreCase);
        _store.Remove(challengeId);
        return Task.FromResult(valid);
    }

    private static string GenerateCode(int length)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return new string(Enumerable.Range(0, length).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }

    private static string RenderImageBase64(string text, int width, int height)
    {
        using var image = new Image<Rgba32>(width, height, Color.WhiteSmoke);

        image.Mutate(ctx =>
        {
            for (var i = 0; i < 8; i++)
            {
                ctx.DrawLine(
                    Color.LightGray,
                    1f,
                    new PointF(Random.Shared.Next(0, width), Random.Shared.Next(0, height)),
                    new PointF(Random.Shared.Next(0, width), Random.Shared.Next(0, height)));
            }

            var font = SystemFonts.CreateFont("Arial", 36, FontStyle.Bold);
            ctx.DrawText(text, font, Color.DarkBlue, new PointF(20, 18));
        });

        using var ms = new MemoryStream();
        image.Save(ms, new PngEncoder());
        return Convert.ToBase64String(ms.ToArray());
    }
}
```

### Bước 4.4: Startup DI

**File:** `src/Infrastructure/Captcha/TextCaptchaStartup.cs`

```csharp
using {ProjectName}.Application.Common.Captcha;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {ProjectName}.Infrastructure.Captcha;

internal static class TextCaptchaStartup
{
    internal static IServiceCollection AddTextCaptcha(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<TextCaptchaSettings>(config.GetSection(nameof(TextCaptchaSettings)));
        services.AddSingleton<ITextCaptchaStore, InMemoryTextCaptchaStore>();
        services.AddTransient<ITextCaptchaService, ImageSharpTextCaptchaService>();
        return services;
    }
}
```

---

## 5. Usage Examples

### Bước 5.1: Tạo challenge

```csharp
public class CreateTextCaptchaHandler : IRequestHandler<CreateTextCaptchaRequest, TextCaptchaChallenge>
{
    private readonly ITextCaptchaService _textCaptchaService;

    public CreateTextCaptchaHandler(ITextCaptchaService textCaptchaService)
    {
        _textCaptchaService = textCaptchaService;
    }

    public Task<TextCaptchaChallenge> Handle(CreateTextCaptchaRequest request, CancellationToken ct)
        => _textCaptchaService.CreateAsync(ct);
}
```

### Bước 5.2: Validate khi đăng ký

```csharp
public class RegisterUserRequest : IRequest<string>
{
    public string Email { get; set; } = default!;
    public string Password { get; set; } = default!;
    public string CaptchaChallengeId { get; set; } = default!;
    public string CaptchaAnswer { get; set; } = default!;
}

public class RegisterUserHandler : IRequestHandler<RegisterUserRequest, string>
{
    private readonly ITextCaptchaService _textCaptchaService;

    public RegisterUserHandler(ITextCaptchaService textCaptchaService)
    {
        _textCaptchaService = textCaptchaService;
    }

    public async Task<string> Handle(RegisterUserRequest request, CancellationToken ct)
    {
        var valid = await _textCaptchaService.ValidateAsync(request.CaptchaChallengeId, request.CaptchaAnswer, ct);
        if (!valid) throw new Exception("Text Captcha không hợp lệ hoặc đã hết hạn.");

        return "User created successfully";
    }
}
```

---

## 6. Summary

### ✅ Đã hoàn thành:
- ✅ Text Captcha độc lập với ImageSharp.
- ✅ Contract/Application riêng (`ITextCaptchaService`).
- ✅ Store và service riêng, one-time validation.

### 📁 File Structure

```text
src/
├── Application/Common/Captcha/
│   ├── ITextCaptchaService.cs
│   └── TextCaptchaChallenge.cs
└── Infrastructure/Captcha/
    ├── TextCaptchaSettings.cs
    ├── TextCaptchaStoreEntry.cs
    ├── ITextCaptchaStore.cs
    ├── InMemoryTextCaptchaStore.cs
    ├── ImageSharpTextCaptchaService.cs
    └── TextCaptchaStartup.cs
```

---

**Quay lại:** [Mục lục](BUILD_INDEX.md)
