# Antigravity Project Rules (GEMINI.md)

> **Purpose**: Quick reference for Antigravity (Gemini) AI-assisted development  
> **Full Documentation**: See `docs/BUILD_INDEX.md` and `docs/MODULE_DOCUMENTATION_TEMPLATE.md`  
> **Version**: 3.0 - Lean & Reference-Based

---

## 📚 Documentation Structure

**Primary References (READ THESE FIRST BEFORE CODING):**
- **`docs/BUILD_INDEX.md`**: Complete roadmap with 30+ build steps.
- **`docs/MODULE_DOCUMENTATION_TEMPLATE.md`**: Standard for writing docs.
- **`docs/BUILD_XX_*.md`**: Feature-specific implementation details.

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

---

## 📐 Naming Conventions (Critical Patterns)

### Interfaces
Pattern: `I{Capability}Service`
✅ `IMailService`, `IFileStorageService`, `INotificationService`
❌ `IEmailService`, `ISmtpService`, `IService`

### Implementations
Pattern: `{Technology}{Capability}Service`
✅ `SmtpMailService`, `AzureBlobStorageService`, `LocalFileStorageService`
❌ `MailService`, `FileStorage`, `AzureService`

### Settings Classes
Pattern: `{Feature}Settings` (MUST match `appsettings.json` section)
✅ `JwtSettings`, `DatabaseSettings`, `CacheSettings`
❌ `JwtOptions`, `JwtConfiguration`, `JWT`

### DTOs & Requests
Pattern: `{Action}{Entity}{Type}`
✅ `CreateUserRequest`, `UpdateProductRequest`, `GetOrdersQuery`
❌ `UserRequest`, `Request`, `User`

---

## 🔧 Core Patterns (Quick Reference)

### 1. Dependency Injection
**Marker Interfaces (Auto-Registration):**
- `ITransientService`
- `IScopedService`
- `ISingletonService`

**Constructor Injection (Mandatory):**
Use `IOptions<T>` for settings and unwrap with `.Value`.

### 2. Configuration Binding
Match section name to Settings class name. Use `services.Configure<T>(config.GetSection(nameof(T)))`.

### 3. MediatR Handler
Implement `IRequest<TResponse>` and `IRequestHandler<TRequest, TResponse>`.

### 4. File Organization
Application layer holds Interfaces and DTOs.
Infrastructure layer holds Implementations, Settings, and `Startup.cs`.

---

## 📖 Documentation Standards

When documenting a new feature, follow `docs/MODULE_DOCUMENTATION_TEMPLATE.md`.
Code must be fully complete (NO placeholders), self-contained, and use XML docs for public APIs. Explain WHY, not WHAT.

---

## 🤖 Antigravity Behavior Instructions

### When Generating Code
1. **Search workspace patterns first** using your `grep_search` or `list_dir` tools to find similar services (e.g., `IMailService`).
2. **Check `docs/BUILD_INDEX.md`**: See if a feature is already documented.
    - Yes: Follow exact patterns from the existing doc.
    - No: Follow `docs/MODULE_DOCUMENTATION_TEMPLATE.md`.
3. **Apply patterns consistently**: Naming, Layer Structure, DI, Config Binding.

### Step-by-Step Feature Workflow
1. Read `docs/BUILD_INDEX.md` and find a similar feature.
2. Read the corresponding `BUILD_XX` doc to understand patterns.
3. **Application Layer**: Create Interface + DTOs.
4. **Infrastructure Layer**: Create Implementation + Settings + `Startup.cs`.
5. **Wire in** `Infrastructure/Startup.cs` (`Add{Feature}()`).
6. **Document** in `docs/BUILD_XX_{Feature}.md`.
7. **Update** `docs/BUILD_INDEX.md` with the new entry.

### When Uncertain
**Ask the user if:**
- Layer dependency is unclear.
- Breaking changes, DB schema changes, or API contract changes are needed.

**Proceed confidently if:**
- Following established patterns from existing docs.
- Writing tests or standard CRUD operations.

---

## 🚫 Common Pitfalls (Anti-Patterns)
❌ Layer violations (e.g., Application depending on Infrastructure)
❌ Hardcoded configuration
❌ Missing `CancellationToken`
❌ Swallowing exceptions (`catch (Exception) { return null; }`)
❌ `async void` (Always use `async Task`)

---

## 🎯 Key Principles
1. **Documentation-Driven**: Always read docs first.
2. **Pattern Consistency**: Copy patterns from existing features.
3. **Layer Sacred**: Never violate dependency rules.
4. **Self-Contained**: Code in docs must compile standalone.
5. **WHY over WHAT**: Comments explain reasoning.
6. **Full Code**: Do NOT use placeholders.
