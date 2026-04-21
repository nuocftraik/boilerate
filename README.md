# Boilerate - Clean Architecture Web API Template

**Boilerate** is a production-ready Web API template built with **.NET 8/9** using **Clean Architecture** principles. It provides a solid foundation with essential enterprise features pre-configured.

---

## 🚀 Quick Start (Bắt đầu nhanh)

### 1. Install Template (Cài đặt)
```bash
dotnet new install Boilerate.CleanArchitecture.Template
```

### 2. Create New Project (Tạo dự án mới)
```bash
dotnet new boilerate -n MyAwesomeApp
```

### 3. Update Connection String
**File:** `src/Host/Configurations/database.json`
```json
{
  "DatabaseSettings": {
    "DBProvider": "mssql",
    "ConnectionString": "Server=localhost;Database=MyAwesomeAppDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

---

## ✨ Key Features (Tính năng nổi bật)

- 🏗️ **Clean Architecture**: Flattened 5-layer structure (Shared, Domain, Application, Infrastructure, Host).
- ⚡ **CQRS with MediatR**: Decoupled commands and queries.
- 🔐 **Identity & JWT**: Full Identity system with JWT Authentication & Refresh Tokens.
- 🛡️ **Permission-based Authorization**: Dynamic policy-based access control.
- 📜 **Audit Trail & Soft Delete**: Automatically track changes and protect data.
- 📊 **Premium Export Services**: High-performance Excel and PDF (QuestPDF) exporting.
- 📂 **File Storage**: Optimized Multipart form-data handling.
- ✅ **Automatic Seed Data**: Auto-initialization on first run.
-  **Structured Logging**: Pre-configured Serilog with Console/File sinks.
- ✅ **Automatic Validation**: FluentValidation integrated with MediatR pipeline.

---

## 🏗️ Project Architecture (Kiến trúc)

The project uses a **Flattened Directory Structure** for better navigation:
- **Shared**: Common contracts and constants.
- **Domain**: Entities, Value Objects, and Domain Events.
- **Application**: Use cases, Interfaces, and DTOs.
- **Infrastructure**: DBContext, Persistence, and External Services.
- **Host**: API Controllers and Program configuration.
---

## 🏗️ Deep Dive

### 📂 Important Files

**Configuration Files** (`src/Host/Configurations/`)
- `database.json` - Database connection
- `security.json` - JWT settings
- `cache.json` - Redis configuration
- `mail.json` - SMTP settings
- `hangfire.json` - Background jobs

**Entry Points**
- `src/Host/Program.cs` - Application startup
- `src/Infrastructure/Startup.cs` - Infrastructure registration
- `src/Application/Startup.cs` - Application registration

### 🌱 Seed Data
Automatically runs on first application start:
- ✅ **Actions**: Create, Update, Delete, View, Search, Export, Import, Clean.
- ✅ **Functions**: User, Role, Product, Category, Dashboard, Hangfire.
- ✅ **Roles**: Admin, Basic.
- ✅ **Permissions**: Auto-generated from Actions × Functions.

### 🔑 Default Credentials
- **Admin User**: `admin@gmail.com` / `Abcd@1234`
- **Hangfire Dashboard**: `https://localhost:7001/hangfire` (User: `admin` / Pwd: `SecurePwd1!`)

---

## 🔧 Common Commands

**Build & Run**
```bash
dotnet restore
dotnet build
dotnet run --project src/Host/Host.csproj
```

**Database Migrations (Run from Root)**
```bash
# Add a new migration
dotnet ef migrations add InitialCreate --project src/Migrators.MSSQL/Migrators.MSSQL.csproj --startup-project src/Host/Host.csproj --context ApplicationDbContext --output-dir Migrations

# Apply migrations to database
dotnet ef database update --project src/Migrators.MSSQL/Migrators.MSSQL.csproj --startup-project src/Host/Host.csproj --context ApplicationDbContext
```

**Testing**
```bash
dotnet test
```

---

## 🌐 External Resources

### **Official Documentation**
- [.NET 8/9 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [EF Core Documentation](https://learn.microsoft.com/en-us/ef/core/)

### **Core Libraries**
- [MediatR](https://github.com/jbogard/MediatR) | [FluentValidation](https://docs.fluentvalidation.net/) | [Mapster](https://github.com/MapsterMapper/Mapster) | [Hangfire](https://docs.hangfire.io/) | [Serilog](https://serilog.net/)

---

## 📜 License
Distributed under the **MIT License**.
