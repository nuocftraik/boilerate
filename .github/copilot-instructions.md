# AI Build Guide

> **Purpose**: Quick reference for AI-assisted development commands  
> **Full Documentation**: See `docs/BUILD_INDEX.md` and `docs/MODULE_DOCUMENTATION_TEMPLATE.md`  
> **Version**: 3.0 - Lean & Reference-Based

---

## 📚 Documentation Structure

**Primary References (READ THESE FIRST):**
- **`docs/BUILD_INDEX.md`**: Complete roadmap with 30+ build steps
- **`docs/MODULE_DOCUMENTATION_TEMPLATE.md`**: Standard for writing docs
- **`docs/BUILD_XX_*.md`**: Feature-specific implementation details

**This file contains:** Quick patterns and rules for immediate reference applicable to any project using this standardized Clean Architecture template.

---

## 🏗️ Project Architecture (Quick Reference)

### Layer Structure

```text
Host → Infrastructure → Application → Domain → Shared
 ↓         ↓           ↓          ↓         ↓
API      EF Core       Use Cases   Entities  Constants
```

### Flattened Directory Structure

```text
src/
├── Shared/
├── Domain/
├── Application/
├── Infrastructure/
├── Migrators.MSSQL/
└── Host/
```
*Note: Projects are placed directly in `src/` without logical layer hierarchy folders (no Core/Infrastructure intermediate folders).*

**Dependency Rules:**
- ✅ Host depends on: Infrastructure, Application
- ✅ Infrastructure depends on: Application, Domain
- ✅ Application depends on: Domain, Shared
- ✅ Domain depends on: Shared
- ✅ Shared depends on: NOTHING
- ❌ NEVER: Inner layers depend on outer layers

**Full details**: `docs/BUILD_INDEX.md` → PHASE 1

---

## 📐 Naming Conventions (Critical Patterns)

### Interfaces
```csharp
Pattern: I{Capability}Service

✅ IMailService, IFileStorageService, INotificationService
❌ IEmailService, ISmtpService, IService
```

### Implementations
```csharp
Pattern: {Technology}{Capability}Service

✅ SmtpMailService, AzureBlobStorageService, LocalFileStorageService
❌ MailService, FileStorage, AzureService
```

### Settings Classes
```csharp
Pattern: {Feature}Settings (MUST match appsettings.json section)

✅ JwtSettings, DatabaseSettings, CacheSettings
❌ JwtOptions, JwtConfiguration, JWT
```

### DTOs & Requests
```csharp
Pattern: {Action}{Entity}{Type}

✅ CreateUserRequest, UpdateProductRequest, GetOrdersQuery
❌ UserRequest, Request, User
```

**Full conventions**: `docs/MODULE_DOCUMENTATION_TEMPLATE.md` → Section "Naming Conventions"

---

## 🔧 Core Patterns (Quick Reference)

### 1. Dependency Injection

**Marker Interfaces (Auto-Registration):**
```csharp
public interface IMyService : ITransientService { }  // Auto-registered
public interface IMyService : IScopedService { }
public interface IMyService : ISingletonService { }
```

**Constructor Injection (Mandatory):**
```csharp
public MyService(
    IOptions<MySettings> settings,
    ILogger<MyService> logger)
{
    _settings = settings.Value;  // Unwrap IOptions
    _logger = logger;
}
```

**Full pattern**: `docs/BUILD_INDEX.md` → Bước 8 & 10

---

### 2. Configuration Binding

```csharp
// Settings class
public class MyFeatureSettings
{
    public string ApiKey { get; set; } = default!;
}

// appsettings.json (section name = class name)
{
  "MyFeatureSettings": { "ApiKey": "secret" }
}

// Startup.cs
services.Configure<MyFeatureSettings>(
    config.GetSection(nameof(MyFeatureSettings))
);
```

**Full pattern**: `docs/BUILD_INDEX.md` → Bước 5

---

### 3. MediatR Handler

```csharp
// Request
public class CreateProductRequest : IRequest<Guid>
{
    public string Name { get; set; } = default!;
}

// Handler
public class CreateProductHandler : IRequestHandler<CreateProductRequest, Guid>
{
    public async Task<Guid> Handle(CreateProductRequest request, CancellationToken ct)
    {
        // Business logic
 return productId;
    }
}
```

**Full pattern**: `docs/BUILD_INDEX.md` → Bước 11, 14

---

### 4. File Organization

```text
src/Application/Common/{Feature}/
├── I{Feature}Service.cs
├── {Action}{Entity}Request.cs
└── {Action}{Entity}Response.cs

src/Infrastructure/{Feature}/
├── {Tech}{Feature}Service.cs
├── {Feature}Settings.cs
└── Startup.cs
```

**Full structure**: `docs/MODULE_DOCUMENTATION_TEMPLATE.md` → Section "File Organization"

---

## 📖 Documentation Standards

### When Creating New Feature

**1. Read template first:**
```
docs/MODULE_DOCUMENTATION_TEMPLATE.md
```

**2. Follow mandatory sections:**
```markdown
1. Overview (What, Why, Checklist, Real-world example)
2. Add Required Packages
3. Application Layer (Interfaces, DTOs)
4. Infrastructure Layer (Implementations, Settings, Startup)
5. Configuration (appsettings.json)
6. Usage Examples (3+ scenarios)
7. Best Practices
8. Testing
9. Troubleshooting
10. Summary
```

**3. Code rules:**
- ✅ FULL CODE (no placeholders like `// ...existing code...`)
- ✅ Self-contained (can rebuild solution from docs alone)
- ✅ XML documentation on public APIs
- ✅ Comments explain WHY, not WHAT
- ✅ Namespace = `{ProjectName}.{Layer}.{Feature}` (e.g., `{ProjectName}.Infrastructure.Mailing`)

**Full template**: `docs/MODULE_DOCUMENTATION_TEMPLATE.md`

---

## 🤖 AI Behavior Instructions

### When Generating Code

**1. Search workspace patterns first:**
```csharp
// Example: Search for similar service
code_search(["IMailService", "SmtpMailService"])
```

**2. Check BUILD_INDEX.md for module:**
```
Is this feature already documented in docs/BUILD_XX_*.md?
→ Yes: Follow exact patterns from that doc
→ No: Follow MODULE_DOCUMENTATION_TEMPLATE.md
```

**3. Apply patterns consistently:**
- Naming conventions (see above)
- Layer structure (Host → Infrastructure → Application → Domain)
- DI patterns (marker interfaces, constructor injection)
- Configuration binding (Settings classes)

---

### When Creating New Feature

**Step-by-step workflow:**

```
Step 1: Read docs/BUILD_INDEX.md
→ Find similar existing feature (e.g., Mailing, Caching, Storage)

Step 2: Read that BUILD_XX doc
→ Understand patterns used

Step 3: Create Application layer
→ Interface (I{Feature}Service : ITransientService)
→ DTOs ({Action}{Entity}Request/Response)

Step 4: Create Infrastructure layer
→ Implementation ({Tech}{Feature}Service)
→ Settings ({Feature}Settings)
→ Startup (Add{Feature}() extension method)

Step 5: Wire in Infrastructure/Startup.cs
→ Call services.Add{Feature}(config);

Step 6: Document in docs/BUILD_XX_{Feature}.md
→ Follow MODULE_DOCUMENTATION_TEMPLATE.md

Step 7: Update docs/BUILD_INDEX.md
→ Add entry in appropriate PHASE
```

---

### When Uncertain

**1. Ask user if:**
- Layer dependency unclear
- Multiple valid approaches exist
- Breaking changes needed
- Security-sensitive operation
- Database schema change
- API contract change

**2. Proceed confidently if:**
- Following established pattern from docs/BUILD_XX
- Creating standard CRUD
- Adding XML documentation
- Writing tests
- Refactoring within same layer

---

## 🚫 Common Pitfalls (Quick Reference)

### ❌ DON'T
```csharp
// 1. Layer violations
using {ProjectName}.Infrastructure.Persistence;  // In Application layer

// 2. Hardcoded config
var apiKey = "hardcoded-secret";

// 3. Missing CancellationToken
public async Task MethodAsync(Request request)

// 4. Swallow exceptions
catch (Exception ex) { return null; }

// 5. async void
public async void ProcessAsync()
```

### ✅ DO
```csharp
// 1. Follow layer separation
// Infrastructure implements Application interfaces

// 2. Use IOptions<T>
private readonly MySettings _settings;
public MyService(IOptions<MySettings> settings)

// 3. Always accept CancellationToken
public async Task MethodAsync(Request request, CancellationToken ct)

// 4. Log and re-throw
catch (Exception ex) { _logger.LogError(ex, "Context"); throw; }

// 5. async Task
public async Task ProcessAsync(CancellationToken ct)
```

**Full anti-patterns**: `docs/MODULE_DOCUMENTATION_TEMPLATE.md` → Section "Anti-Patterns"

---

## 🎯 Key Principles

1. **Documentation-Driven**: Read `docs/BUILD_INDEX.md` first
2. **Pattern Consistency**: Copy patterns from existing `docs/BUILD_XX_*.md`
3. **Layer Sacred**: Never violate dependency rules
4. **Self-Contained**: Code in docs must compile standalone
5. **WHY over WHAT**: Comments explain reasoning
6. **Full Code**: No placeholders in documentation

---

## 📚 Where to Find What

| Need | Location |
|------|----------|
| Roadmap & module list | `docs/BUILD_INDEX.md` |
| Doc writing template | `docs/MODULE_DOCUMENTATION_TEMPLATE.md` |
| Naming conventions | This file + MODULE_DOCUMENTATION_TEMPLATE.md |
| Layer structure | `docs/BUILD_INDEX.md` → PHASE 1 |
| DI patterns | `docs/BUILD_INDEX.md` → Bước 8, 10 |
| Repository pattern | `docs/BUILD_INDEX.md` → Bước 11 |
| Authentication | `docs/BUILD_INDEX.md` → PHASE 4 |
| Infrastructure services | `docs/BUILD_INDEX.md` → PHASE 5 |
| Feature examples | `docs/BUILD_XX_*.md` (30+ modules) |

---

## 🔍 Quick Lookup Commands

When user asks:

**"How do I create a new service?"**
→ Answer: Follow workflow in section "When Creating New Feature" above
→ Reference: `docs/BUILD_INDEX.md` → Similar existing feature

**"What naming convention for X?"**
→ Answer: See "Naming Conventions" section above
→ Reference: `docs/MODULE_DOCUMENTATION_TEMPLATE.md`

**"Where is [Feature] documented?"**
→ Check: `docs/BUILD_INDEX.md` → Find BUILD_XX entry
→ Read: `docs/BUILD_XX_{Feature}.md`

**"How to write documentation?"**
→ Template: `docs/MODULE_DOCUMENTATION_TEMPLATE.md`
→ Example: Any `docs/BUILD_XX_*.md` file

---

## 🎓 Learning Path for New AI Session

```
1. Read this file (5 min)
   → Understand core patterns

2. Skim docs/BUILD_INDEX.md (10 min)
   → See all 30+ modules and phases

3. Read 1-2 BUILD_XX docs (20 min)
   → Learn by example (e.g., BUILD_21_Email_Service)

4. Read docs/MODULE_DOCUMENTATION_TEMPLATE.md (15 min)
   → Understand doc structure

Total: 50 minutes to understand full project context
```

---

**Version**: 3.0 (Lean, Reference-Based, Generic Template)  
**Last Updated**: 2026-01-30
**Maintained By**: {ProjectName} Development Team

**Primary Documentation**: `docs/BUILD_INDEX.md` (Master reference)
