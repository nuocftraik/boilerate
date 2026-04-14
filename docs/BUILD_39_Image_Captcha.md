# BUILD 39B - Image Captcha (Chọn ảnh đúng)

> 📚 [Quay lại Mục lục](BUILD_INDEX.md)  
> 📋 **Prerequisites:** Bước 10 (Service Registration) đã hoàn thành

Tài liệu này hướng dẫn xây dựng **Image Captcha** độc lập theo dạng “chọn đúng ảnh theo câu hỏi”, không phụ thuộc Text Captcha.

---

## 1. Overview

**Làm gì:** Tạo service sinh challenge gồm câu hỏi + danh sách ảnh và xác thực lựa chọn người dùng.

**Tại sao cần:**
- **Khó tự động hóa hơn text OCR:** bot phải hiểu ngữ nghĩa hình ảnh.
- **Phù hợp endpoint rủi ro cao:** reset mật khẩu, đăng ký nghi ngờ spam, gửi biểu mẫu công khai.
- **Triển khai nội bộ:** có thể dùng ảnh local/CDN mà không cần phụ thuộc vendor captcha ngoài.

**Trong bước này chúng ta sẽ:**
- ✅ Tạo contract `IImageCaptchaService` ở Application.
- ✅ Tạo kho dữ liệu ảnh mẫu.
- ✅ Implement `DefaultImageCaptchaService` tạo challenge.
- ✅ Lưu challenge và validate theo one-time token.
- ✅ Tích hợp vào use case xác thực user action.

---

## 2. Add Required Packages

Image Captcha theo mô hình này chỉ quản lý metadata ảnh (`ImageUrl`) và validate lựa chọn, nên **không cần package xử lý ảnh**.

---

## 3. Application Layer

### Bước 3.1: DTO option và challenge

**File:** `src/Application/Common/Captcha/ImageCaptchaChoice.cs`

```csharp
namespace {ProjectName}.Application.Common.Captcha;

/// <summary>
/// Một lựa chọn ảnh cho challenge
/// </summary>
public class ImageCaptchaChoice
{
    public string OptionId { get; set; } = default!;
    public string ImageUrl { get; set; } = default!;
}
```

**File:** `src/Application/Common/Captcha/ImageCaptchaChallenge.cs`

```csharp
namespace {ProjectName}.Application.Common.Captcha;

/// <summary>
/// Challenge cho Image Captcha
/// </summary>
public class ImageCaptchaChallenge
{
    public string ChallengeId { get; set; } = default!;
    public string Question { get; set; } = default!;
    public IReadOnlyList<ImageCaptchaChoice> Choices { get; set; } = Array.Empty<ImageCaptchaChoice>();
    public DateTime ExpiresAtUtc { get; set; }
}
```

### Bước 3.2: Contract service

**File:** `src/Application/Common/Captcha/IImageCaptchaService.cs`

```csharp
using {ProjectName}.Application.Common.Interfaces;

namespace {ProjectName}.Application.Common.Captcha;

/// <summary>
/// Service tạo và xác thực Image Captcha
/// </summary>
public interface IImageCaptchaService : ITransientService
{
    Task<ImageCaptchaChallenge> CreateAsync(CancellationToken cancellationToken = default);

    Task<bool> ValidateAsync(string challengeId, string selectedOptionId, CancellationToken cancellationToken = default);
}
```

---

## 4. Infrastructure Layer

### Bước 4.1: Settings

**File:** `src/Infrastructure/Captcha/ImageCaptchaSettings.cs`

```csharp
namespace {ProjectName}.Infrastructure.Captcha;

public class ImageCaptchaSettings
{
    public bool Enable { get; set; } = true;
    public int OptionCount { get; set; } = 4;
    public int ExpiryMinutes { get; set; } = 2;
}
```

**File cấu hình:** `appsettings.json`

```json
{
  "ImageCaptchaSettings": {
    "Enable": true,
    "OptionCount": 4,
    "ExpiryMinutes": 2
  }
}
```

### Bước 4.2: Data model + store

**File:** `src/Infrastructure/Captcha/ImageCaptchaStoreEntry.cs`

```csharp
namespace {ProjectName}.Infrastructure.Captcha;

internal class ImageCaptchaStoreEntry
{
    public string ChallengeId { get; set; } = default!;
    public string ExpectedOptionId { get; set; } = default!;
    public DateTime ExpiresAtUtc { get; set; }
}
```

**File:** `src/Infrastructure/Captcha/IImageCaptchaStore.cs`

```csharp
namespace {ProjectName}.Infrastructure.Captcha;

internal interface IImageCaptchaStore
{
    void Save(ImageCaptchaStoreEntry entry);
    bool TryGet(string challengeId, out ImageCaptchaStoreEntry? entry);
    void Remove(string challengeId);
}
```

**File:** `src/Infrastructure/Captcha/InMemoryImageCaptchaStore.cs`

```csharp
using System.Collections.Concurrent;

namespace {ProjectName}.Infrastructure.Captcha;

internal class InMemoryImageCaptchaStore : IImageCaptchaStore
{
    private static readonly ConcurrentDictionary<string, ImageCaptchaStoreEntry> _entries = new();

    public void Save(ImageCaptchaStoreEntry entry) => _entries[entry.ChallengeId] = entry;

    public bool TryGet(string challengeId, out ImageCaptchaStoreEntry? entry)
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

### Bước 4.3: Implement service

**File:** `src/Infrastructure/Captcha/DefaultImageCaptchaService.cs`

```csharp
using {ProjectName}.Application.Common.Captcha;
using Microsoft.Extensions.Options;

namespace {ProjectName}.Infrastructure.Captcha;

public class DefaultImageCaptchaService : IImageCaptchaService
{
    private readonly ImageCaptchaSettings _settings;
    private readonly IImageCaptchaStore _store;

    public DefaultImageCaptchaService(IOptions<ImageCaptchaSettings> settings, IImageCaptchaStore store)
    {
        _settings = settings.Value;
        _store = store;
    }

    public Task<ImageCaptchaChallenge> CreateAsync(CancellationToken cancellationToken = default)
    {
        var images = new List<(string Label, string Url)>
        {
            ("cat", "/static/captcha/cat-1.jpg"),
            ("cat", "/static/captcha/cat-2.jpg"),
            ("car", "/static/captcha/car-1.jpg"),
            ("car", "/static/captcha/car-2.jpg"),
            ("tree", "/static/captcha/tree-1.jpg"),
            ("tree", "/static/captcha/tree-2.jpg")
        };

        var labels = new[] { "cat", "car", "tree" };
        var target = labels[Random.Shared.Next(labels.Length)];

        var candidates = images
            .OrderBy(_ => Random.Shared.Next())
            .Take(Math.Max(_settings.OptionCount, 4))
            .ToList();

        if (!candidates.Any(x => x.Label == target))
        {
            candidates[0] = images.First(x => x.Label == target);
        }

        var choices = candidates.Select(x => new ImageCaptchaChoice
        {
            OptionId = Guid.NewGuid().ToString("N"),
            ImageUrl = x.Url
        }).ToList();

        var correctIndex = candidates.FindIndex(x => x.Label == target);
        var correctOptionId = choices[correctIndex].OptionId;

        var challengeId = Guid.NewGuid().ToString("N");
        var expires = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        _store.Save(new ImageCaptchaStoreEntry
        {
            ChallengeId = challengeId,
            ExpectedOptionId = correctOptionId,
            ExpiresAtUtc = expires
        });

        return Task.FromResult(new ImageCaptchaChallenge
        {
            ChallengeId = challengeId,
            Question = $"Hãy chọn ảnh chứa: {target}",
            Choices = choices,
            ExpiresAtUtc = expires
        });
    }

    public Task<bool> ValidateAsync(string challengeId, string selectedOptionId, CancellationToken cancellationToken = default)
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

        var valid = string.Equals(entry.ExpectedOptionId, selectedOptionId, StringComparison.OrdinalIgnoreCase);
        _store.Remove(challengeId);
        return Task.FromResult(valid);
    }
}
```

### Bước 4.4: Startup DI

**File:** `src/Infrastructure/Captcha/ImageCaptchaStartup.cs`

```csharp
using {ProjectName}.Application.Common.Captcha;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace {ProjectName}.Infrastructure.Captcha;

internal static class ImageCaptchaStartup
{
    internal static IServiceCollection AddImageCaptcha(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<ImageCaptchaSettings>(config.GetSection(nameof(ImageCaptchaSettings)));
        services.AddSingleton<IImageCaptchaStore, InMemoryImageCaptchaStore>();
        services.AddTransient<IImageCaptchaService, DefaultImageCaptchaService>();
        return services;
    }
}
```

---

## 5. Usage Examples

### Bước 5.1: Tạo challenge

```csharp
public class CreateImageCaptchaHandler : IRequestHandler<CreateImageCaptchaRequest, ImageCaptchaChallenge>
{
    private readonly IImageCaptchaService _imageCaptchaService;

    public CreateImageCaptchaHandler(IImageCaptchaService imageCaptchaService)
    {
        _imageCaptchaService = imageCaptchaService;
    }

    public Task<ImageCaptchaChallenge> Handle(CreateImageCaptchaRequest request, CancellationToken ct)
        => _imageCaptchaService.CreateAsync(ct);
}
```

### Bước 5.2: Validate trước khi thực hiện action

```csharp
public class VerifySensitiveActionRequest : IRequest<bool>
{
    public string CaptchaChallengeId { get; set; } = default!;
    public string SelectedOptionId { get; set; } = default!;
}

public class VerifySensitiveActionHandler : IRequestHandler<VerifySensitiveActionRequest, bool>
{
    private readonly IImageCaptchaService _imageCaptchaService;

    public VerifySensitiveActionHandler(IImageCaptchaService imageCaptchaService)
    {
        _imageCaptchaService = imageCaptchaService;
    }

    public Task<bool> Handle(VerifySensitiveActionRequest request, CancellationToken ct)
        => _imageCaptchaService.ValidateAsync(request.CaptchaChallengeId, request.SelectedOptionId, ct);
}
```

---

## 6. Summary

### ✅ Đã hoàn thành:
- ✅ Image Captcha độc lập với interface và service riêng.
- ✅ Cơ chế create/validate one-time challenge.
- ✅ Store in-memory và cấu hình tách riêng.

### 📁 File Structure

```text
src/
├──Application/Common/Captcha/
│   ├── IImageCaptchaService.cs
│   ├── ImageCaptchaChoice.cs
│   └── ImageCaptchaChallenge.cs
└── Infrastructure/Captcha/
    ├── ImageCaptchaSettings.cs
    ├── ImageCaptchaStoreEntry.cs
    ├── IImageCaptchaStore.cs
    ├── InMemoryImageCaptchaStore.cs
    ├── DefaultImageCaptchaService.cs
    └── ImageCaptchaStartup.cs
```

---

**Quay lại:** [Mục lục](BUILD_INDEX.md)
