# BUILD_07 - Logging Setup (Serilog + Seq/Elasticsearch)

> 📚 [Quay lại Mục lục](BUILD_INDEX.md)  
> 📋 **Prerequisites:** BUILD_06 (Host Layer) đã hoàn thành  
> ⏱️ **Thời gian:** Khoảng 15 phút

---

## 1. Tổng quan

**Làm gì:** Cấu hình hệ thống Logging tập trung sử dụng Serilog.

**Tại sao cần:**
- **Bắt lỗi startup:** Bắt được các lỗi xảy ra trong quá trình khởi động ứng dụng trước khi Host được build.
- **Lưu trữ tập trung (Centralized Logging):** Ghi log ra ElasticSearch để query và phân tích (qua Kibana).
- **Linh hoạt:** Cấu hình bật/tắt ghi file, log có cấu trúc (Structured Logging) thông qua cấu hình JSON. 
- **Lọc nhiễu:** Ghi đè (Override) minimum log level cho một số namespace mặc định của Microsoft, EntityFramework để giảm log rác.

**Trong bước này chúng ta sẽ:**
- ✅ Cài đặt các package Serilog và Figgle (cho ASCII Art Banner).
- ✅ Tạo `LoggerSettings` để map với cấu hình `logger.json`.
- ✅ Thiết lập `StaticLogger` (Bootstrap Logger).
- ✅ Viết `Extensions.cs` để cấu hình động cho Serilog (Elasticsearch, File, Console).
- ✅ Tích hợp cấu hình vào `Program.cs`.

---

## 2. Thêm Packages (Nuget)

### Bước 2.1: Infrastructure packages

**File:** `src/Infrastructure/Infrastructure.csproj`

```xml
<ItemGroup>
    <PackageReference Include="Figgle" Version="0.5.1" />
    <PackageReference Include="Serilog.Exceptions" Version="8.4.0" />
    <PackageReference Include="Serilog.Enrichers.Process" Version="2.0.2" />
    <PackageReference Include="Serilog.Enrichers.Thread" Version="3.1.0" />
    <PackageReference Include="Serilog.Expressions" Version="3.4.1" />
    <PackageReference Include="Serilog.Enrichers.Environment" Version="2.2.0" />
    <PackageReference Include="Serilog.Extensions.Hosting" Version="5.0.1" />
    <PackageReference Include="Serilog.Formatting.Compact" Version="1.1.0" />
    <PackageReference Include="Serilog.Settings.Configuration" Version="3.4.0" />
    <PackageReference Include="Serilog.Sinks.Async" Version="1.5.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="4.1.0" />
    <PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />
    <PackageReference Include="Serilog.Sinks.MSSqlServer" Version="6.3.0" />
    <PackageReference Include="Serilog.Sinks.Elasticsearch" Version="9.0.0" />
    <PackageReference Include="Serilog.Sinks.Seq" Version="5.2.2" />
</ItemGroup>
```

**Giải thích packages:**
- `Serilog.Sinks.Elasticsearch`: Hỗ trợ đẩy logs vào Elasticsearch.
- `Serilog.Sinks.Async`: Ghi log bất đồng bộ, tránh block main thread.
- `Serilog.Formatting.Compact`: Ghi cấu trúc JSON tinh gọn.
- `Figgle`: Dùng để in ra ASCII text "ECO.WebAPI" lúc ứng dụng start.

### Bước 2.2: Host packages

**File:** `src/Host/Host.csproj`

```xml
<ItemGroup>
    <PackageReference Include="Hangfire.Console.Extensions.Serilog" Version="1.0.2" />
    <PackageReference Include="Serilog.AspNetCore" Version="6.1.0" />
</ItemGroup>
```

---

## 3. Logger Settings và Bootstrap Logger

### Bước 3.1: DTO Cấu hình Logger

Tạo model để tự động binding với cấu hình trong file `appsettings.json` (hoặc `logger.json`).

**File:** `src/Infrastructure/Logging/LoggerSettings.cs`

```csharp
namespace {ProjectName}.Infrastructure.Logging;

public class LoggerSettings
{
    public string AppName { get; set; } = "{ProjectName}";
    public string ElasticSearchUrl { get; set; } = string.Empty;
    public bool WriteToFile { get; set; } = false;
    public bool StructuredConsoleLogging { get; set; } = false;
    public string MinimumLogLevel { get; set; } = "Information";
}
```

### Bước 3.2: Static Logger (Bootstrap)

Dùng để log ngay cả trước khi host (Dependency Injection) khởi tạo xong, bảo đảm những exception do quá trình startup gây ra vẫn được lưu.

**File:** `src/Infrastructure/Common/StaticLogger.cs`

```csharp
using Serilog;

namespace {ProjectName}.Infrastructure.Common;

public static class StaticLogger
{
    public static void EnsureInitialized()
    {
        if (Log.Logger is not Serilog.Core.Logger)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();
        }
    }
}
```

---

## 4. Serilog Configuration Extensions

Đây là file thiết lập behavior của Serilog cho Application runtime, đọc cấu hình từ `LoggerSettings`, thiết lập minimum log level và nối với Elasticsearch nếu được khai báo.

**File:** `src/Infrastructure/Logging/Extensions.cs`

```csharp
using Microsoft.AspNetCore.Builder;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Figgle;
using Serilog.Exceptions;

namespace {ProjectName}.Infrastructure.Logging;

public static class Extensions
{
    public static void RegisterSerilog(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions<LoggerSettings>().BindConfiguration(nameof(LoggerSettings));

        _ = builder.Host.UseSerilog((_, sp, serilogConfig) =>
        {
            var loggerSettings = sp.GetRequiredService<IOptions<LoggerSettings>>().Value;
            string appName = loggerSettings.AppName;
            string elasticSearchUrl = loggerSettings.ElasticSearchUrl;
            bool writeToFile = loggerSettings.WriteToFile;
            bool structuredConsoleLogging = loggerSettings.StructuredConsoleLogging;
            string minLogLevel = loggerSettings.MinimumLogLevel;

            ConfigureEnrichers(serilogConfig, appName);
            ConfigureConsoleLogging(serilogConfig, structuredConsoleLogging);
            ConfigureWriteToFile(serilogConfig, writeToFile);
            ConfigureElasticSearch(builder, serilogConfig, appName, elasticSearchUrl);
            SetMinimumLogLevel(serilogConfig, minLogLevel);
            OverideMinimumLogLevel(serilogConfig);
            
            // Render text lúc khởi động
            Console.WriteLine(FiggleFonts.Standard.Render(loggerSettings.AppName));
        });
    }

    private static void ConfigureEnrichers(LoggerConfiguration serilogConfig, string appName)
    {
        serilogConfig
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", appName)
            .Enrich.WithExceptionDetails()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId();
    }

    private static void ConfigureConsoleLogging(LoggerConfiguration serilogConfig, bool structuredConsoleLogging)
    {
        if (structuredConsoleLogging)
        {
            serilogConfig.WriteTo.Async(wt => wt.Console(new CompactJsonFormatter()));
        }
        else
        {
            serilogConfig.WriteTo.Async(wt => wt.Console());
        }
    }

    private static void ConfigureWriteToFile(LoggerConfiguration serilogConfig, bool writeToFile)
    {
        if (writeToFile)
        {
            serilogConfig.WriteTo.File(
                new CompactJsonFormatter(),
                "Logs/logs.json",
                restrictedToMinimumLevel: LogEventLevel.Information,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 5);
        }
    }

    private static void ConfigureElasticSearch(WebApplicationBuilder builder, LoggerConfiguration serilogConfig, string appName, string elasticSearchUrl)
    {
        if (!string.IsNullOrEmpty(elasticSearchUrl))
        {
            string? formattedAppName = appName?.ToLower().Replace(".", "-").Replace(" ", "-");
            string indexFormat = $"{formattedAppName}-logs-{builder.Environment.EnvironmentName?.ToLower().Replace(".", "-")}-{DateTime.UtcNow:yyyy-MM}";
            
            serilogConfig.WriteTo.Async(writeTo =>
                writeTo.Elasticsearch(new(new Uri(elasticSearchUrl))
                {
                    AutoRegisterTemplate = true,
                    IndexFormat = indexFormat,
                    MinimumLogEventLevel = LogEventLevel.Information,
                })).Enrich.WithProperty("Environment", builder.Environment.EnvironmentName!);
        }
    }

    private static void OverideMinimumLogLevel(LoggerConfiguration serilogConfig)
    {
        serilogConfig
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Hangfire", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Error);
    }

    private static void SetMinimumLogLevel(LoggerConfiguration serilogConfig, string minLogLevel)
    {
        switch (minLogLevel.ToLower())
        {
            case "debug":
                serilogConfig.MinimumLevel.Debug();
                break;
            case "information":
                serilogConfig.MinimumLevel.Information();
                break;
            case "warning":
                serilogConfig.MinimumLevel.Warning();
                break;
            default:
                serilogConfig.MinimumLevel.Information();
                break;
        }
    }
}
```

**Giải thích:**
- **ConfigureEnrichers:** Thêm các thông tin cơ bản cho log message như Tên App, process id, exception details.
- **ConfigureElasticSearch:** Tổ chức index tự động mỗi ngày (rolling), format index theo tên app và environment.
- **OverideMinimumLogLevel:** Tránh việc in log của System/Microsoft ra ngoài trừ khi gặp lỗi (Warning/Error).


---

## 5. Cấu hình JSON & Khởi chạy Host

### Bước 5.1: File Cấu hình JSON

Đảm bảo project Host có cấu hình `logger.json` map với properties của class `LoggerSettings`.

**File:** `src/Host/Configurations/logger.json`

```json
{
  "LoggerSettings": {
    "AppName": "{ProjectName}",
    "ElasticSearchUrl": "http://localhost:9200",
    "WriteToFile": true,
    "StructuredConsoleLogging": false,
    "MinimumLogLevel": "Information"
  }
}
```

*(Nhớ khai báo nạp `logger.json` trong Configuration Pipeline như đã làm ở Bước 6)*

### Bước 5.2: Tích hợp vào Start Pipeline

Gọi bootstrap logger ngay dòng đầu tiên trong `Program.cs`, và gọi method `.RegisterSerilog()`.

**File:** `src/Host/Program.cs`

```csharp
using {ProjectName}.Infrastructure;
using {ProjectName}.Host.Configurations;
using {ProjectName}.Infrastructure.Common;
using Serilog;
using {ProjectName}.Infrastructure.Logging;
using {ProjectName}.Application;
// Các namespace khác...

// 1. Chạy bootstrap logger đầu tiên
StaticLogger.EnsureInitialized();
Log.Information("Server Booting Up...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // 2. Chú ý dòng này - Load configs trước rồi injection Serilog
    builder.AddConfigurations().RegisterSerilog();
    
    builder.Services.AddControllers();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddApplication();
    
    // ... Setup Swagger ...

    var app = builder.Build();

    // ... Middlewares ...

    Log.Information("Application Starting...");
    app.Run();
}
catch (Exception ex) when (!ex.GetType().Name.Equals("HostAbortedException", StringComparison.Ordinal))
{
    StaticLogger.EnsureInitialized();
    Log.Fatal(ex, "Unhandled exception occurred during application startup");
}
finally
{
    StaticLogger.EnsureInitialized();
    Log.Information("Server Shutting down...");
    Log.CloseAndFlush();
}
```

---

## 6. Usage Examples

### Bước 6.1: Structured logging với ILogger

Sử dụng chuỗi Format tự động theo cơ chế structured logging, rất quan trọng nếu tìm kiếm qua Elasticsearch (Kibana).

```csharp
// ❌ BAD: Dùng string interpolation/concat
_logger.LogInformation($"User {userId} created order {orderId}");

// ✅ GOOD: Dùng cấu trúc param
_logger.LogInformation("User {UserId} created order {OrderId}", userId, orderId);
```

### Bước 6.2: Serilog Context

Sử dụng push context properties tự động thêm properties này ở mọi logs gọi bên trong khối "using".

```csharp
using Serilog.Context;

using (LogContext.PushProperty("UserId", userId))
{
    _logger.LogInformation("Start process payment");
    // Bất cứ log nào trong này đều đính kèm UserId 
    _logger.LogInformation("End process payment");
}
```

---

## 7. Summary

### ✅ Đã hoàn thành:
- ✅ Thiết lập chuỗi Serilog tích hợp ElasticSearch Sink tối ưu cho phân tích log chuyên sâu.
- ✅ Sử dụng Bootstrap Logger (`StaticLogger`) quản lý lỗi trong chuỗi khởi tạo Dependency Injection ban đầu.
- ✅ Tự động filter log vô ích nhờ log level overrides chuẩn trên Serilog.

### 📁 File Structure:

```text
src/
├── Infrastructure/
│   └── Infrastructure/
│       ├── Common/
│       │   └── StaticLogger.cs
│       └── Logging/
│           ├── Extensions.cs
│           └── LoggerSettings.cs
└── Host/
    └── Host/
        ├── Configurations/
        │   └── logger.json
        └── Program.cs
```

---

## 8. Next Steps

**Tiếp theo:** [BUILD_08 - Database Initialization](BUILD_08_Database_Initialization.md)

Trong bước tiếp theo, chúng ta sẽ:
1. ✅ Tạo Database Initializer logic
2. ✅ Chạy Migration tự động hóa
3. ✅ Seed dữ liệu Admin, Roles, Actions ban đầu hệ thống 

---

**Quay lại:** [Mục lục](BUILD_INDEX.md)
