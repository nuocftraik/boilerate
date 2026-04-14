# Template cho Module Documentation

> 📚 **Mục đích:** Template này giúp viết documentation nhất quán cho mỗi module/feature trong `{ProjectName}` solution.

---

## 📖 Nguyên tắc Viết Docs

### **1. Self-Contained (Tự đủ)**
- ✅ **PHẢI** có đầy đủ code trong docs
- ✅ **KHÔNG** reference đến code có sẵn trong workspace
- ✅ Mục đích: Tạo lại solution từ đầu chỉ từ docs
- ✅ Copy/paste code từ docs phải chạy được ngay

### **2. Tiếng Việt & Dễ Hiểu**
- ✅ Giải thích bằng tiếng Việt
- ✅ Thuật ngữ tiếng Anh có giải thích
- ✅ Code comments bằng tiếng Việt (hoặc tiếng Anh rõ ràng)
- ✅ Ví dụ thực tế, gần gũi

### **3. Chia Nhỏ Docs Phức Tạp**
- ✅ Main doc: Focus usage & overview
- ✅ Sub docs: Chi tiết implementation (BUILD_XX_DetailName.md)
- ✅ Ví dụ: BUILD_11 + BUILD_11_Specification

### **4. Code Quality**
- ✅ Code phải compile được
- ✅ Có comments giải thích logic
- ✅ Namespace đúng chuẩn project
- ✅ Follow naming conventions

### **5. Build Order & Dependency (Bắt buộc)**
- ✅ Code ở bước sau chỉ được dùng thành phần đã tạo ở bước trước
- ✅ Interface/contract phải xuất hiện trước implementation
- ✅ Ví dụ: tạo `IRepository<T>` trước rồi mới implement `Repository<T>`
- ✅ Không tham chiếu class chưa tồn tại trong chuỗi build

---

## 3) Quy tắc kiến trúc bắt buộc

1. **Flat Folder Structure**
   - ✅ `src/Domain/`
   - ❌ `src/Core/Domain/`

2. **Project name = Folder name**
   - Ví dụ: `src/Domain/Domain.csproj`

3. **Namespace chuẩn**
   - `{ProjectName}.Shared`
   - `{ProjectName}.Domain`
   - `{ProjectName}.Application`
   - `{ProjectName}.Infrastructure`
   - `{ProjectName}.Host`

---

## 📖 Cấu trúc chuẩn cho mỗi Module Doc

### **Header Section**

**Template:**
```markdown
# [Module Name] - [Short Description]

> 📚 [Quay lại Mục lục](BUILD_INDEX.md)  
> 📋 **Prerequisites:** [Bước trước đó phải hoàn thành]

Tài liệu này hướng dẫn xây dựng [Module Name] - [Purpose].

---
```

**Ví dụ thực tế:**
```markdown
# Repository Pattern và Specification

> 📚 [Quay lại Mục lục](BUILD_INDEX.md)  
> 📋 **Prerequisites:** Bước 10 (Service Registration) đã hoàn thành

Tài liệu này hướng dẫn về Repository Pattern với Ardalis.Specification và Domain Events.

---

### **Section 1: Overview (Tổng quan)**

**Required elements:**
- "Làm gì" (What)
- "Tại sao cần" (Why)
- "Trong bước này chúng ta sẽ" (Checklist)
- Real-world example (nếu phức tạp)

```markdown
## 1. Overview

**Làm gì:** [Mô tả ngắn gọn module này làm gì]

**Tại sao cần:**
- **[Lý do 1]:** [Giải thích]
- **[Lý do 2]:** [Giải thích]
- **[Lý do 3]:** [Giải thích]

**Trong bước này chúng ta sẽ:**
- ✅ [Task 1]
- ✅ [Task 2]
- ✅ [Task 3]

**Real-world example:** (nếu module phức tạp)
```csharp
// Ví dụ usage code để người đọc hiểu được mục đích
public class ExampleUsage
{
    // ...
}
\```


---

### **Section 2: Add Required Packages**

**Required elements:**
- Packages với version cụ thể
- Giải thích "Why" cho mỗi package
- File path chính xác

```markdown
## 2. Add Required Packages

### Bước 2.1: [Package Group Name]

**File:** `src/[Project]/[Project].csproj`

```xml
<ItemGroup>
    <!-- [Mục đích của package group] -->
    <PackageReference Include="PackageName" Version="x.x.x" />
    <PackageReference Include="AnotherPackage" Version="y.y.y" />
</ItemGroup>
\```

**Giải thích packages:**
- `PackageName`: [Tại sao cần package này, nó làm gì]
- `AnotherPackage`: [Giải thích]

**⚠️ Lưu ý:**
- [Lưu ý đặc biệt nếu có]

---
```

**Ví dụ thực tế:**
```markdown
## 2. Add Required Packages

### Bước 2.1: Add NewId Package

**File:** `src/Core/Domain/Domain.csproj`

```xml
<ItemGroup>
    <!-- For sequential GUID generation -->
    <PackageReference Include="NewId" Version="4.0.1" />
</ItemGroup>
\```

**Why NewId:**
- `NewId.Next().ToGuid()` tạo sequential GUIDs
- Better database performance (less fragmentation)
- Better indexing performance

---
```

---

### **Section 3-N: Implementation Steps**

**Required elements:**
- Đánh số Bước X.Y rõ ràng
- File path CHÍNH XÁC
- FULL CODE (không tóm tắt)
- Comments giải thích trong code
- Section "Giải thích" sau code
- "Tại sao" hoặc "Lợi ích"

```markdown
## 3. [Major Component Name]

### Bước 3.1: [Specific Task]

**Làm gì:** [Mô tả ngắn gọn]

**Tại sao:** [Lý do cần làm bước này]

**File:** `src/[Project]/[Path]/[FileName].cs`

```csharp
// FULL CODE - Không tóm tắt
namespace {ProjectName}.[Namespace];

/// <summary>
/// [XML comment mô tả class]
/// </summary>
public class ClassName
{
    /// <summary>
    /// [XML comment mô tả property/method]
    /// </summary>
    public string Property { get; set; } = default!;
 
    // Comment giải thích logic
    public void Method()
    {
        // Implementation với comments
    }
}
\```

**Giải thích:**
- **Property:** [Giải thích property]
- **Method():** [Giải thích method]
- **Line X-Y:** [Giải thích đoạn code đặc biệt]

**Tại sao [Design Decision]:**
- [Lý do 1]
- [Lý do 2]

**Lợi ích:**
- ✅ [Benefit 1]
- ✅ [Benefit 2]

**⚠️ Lưu ý:**
- [Điều quan trọng cần nhớ]

---
```

**Ví dụ thực tế:**
```markdown
## 3. Tạo Domain Event Contracts

### Bước 3.1: IEvent Interface

**Làm gì:** Tạo marker interface cho tất cả domain events.

**Tại sao:** Đánh dấu class là domain event, hỗ trợ generic handlers.

**File:** `src/Domain/Domain.csproj`

```csharp
namespace {ProjectName}.Domain.Common.Contracts;

/// <summary>
/// Marker interface for all domain events
/// </summary>
public interface IEvent
{
}
\```

**Giải thích:**
- Marker interface - không có methods
- Đánh dấu class là một domain event
- Tất cả domain events phải implement interface này

**Why in Domain layer:**
- Events là domain concept (business logic)
- Không phải infrastructure concern
- Follow DDD principles

---
```

---

### **Handling Complex Implementation**

**Nếu code QUÁ PHỨC TẠP (>200 dòng):**

**Main doc - Focus usage:**
```markdown
## 3. [Complex Component]

### 📌 **Tổng quan**

[Component Name] là [mô tả ngắn gọn].

**Core methods:**
- `Method1()` - [Mô tả]
- `Method2()` - [Mô tả]

**⚠️ Implementation Chi tiết:**

Code của [Component Name] khá phức tạp ([lý do]).  
**FULL CODE implementation** được viết trong document riêng: **[BUILD_XX_DetailName.md](BUILD_XX_DetailName.md)**

**Trong section này chúng ta chỉ học CÁCH SỬ DỤNG, không đi sâu vào implementation.**

---

### Bước 3.1: Cách sử dụng [Component]

**Usage Example 1 - [Scenario]:**
```csharp
// Simple usage example
\```

**Usage Example 2 - [Scenario]:**
```csharp
// More complex example
\```

**⚠️ Để hiểu chi tiết implementation:**
- [Technical detail 1]
- [Technical detail 2]

👉 Xem [BUILD_XX_DetailName.md](BUILD_XX_DetailName.md)

---
```

**Sub doc (BUILD_XX_DetailName.md) - Full implementation:**
```markdown
# [Component Name] - Chi tiết Implementation

> 📚 [Quay lại BUILD_XX](BUILD_XX_Main.md)

Document này chứa FULL CODE implementation của [Component Name].  
Đây là phần phức tạp với [technical aspects].

---

## 1. Overview

**File này implement:**
- [Feature 1]
- [Feature 2]

**Dependencies:**
- [Dependency 1]
- [Dependency 2]

---

## 2. Full Implementation

### Bước 2.1: [Part 1]

**File:** `src/[Project]/[Path]/[FileName].cs`

```csharp
// FULL CODE (có thể 500+ dòng)
namespace {ProjectName}.[Namespace];

public static class ComplexClass
{
    // Full implementation với comments chi tiết
}
\```

**Giải thích chi tiết:**
- [Section 1]: [Detailed explanation]
- [Section 2]: [Detailed explanation]

---

## 3. Flow Diagrams

### 3.1: [Process Name] Flow

\```
[ASCII diagram or detailed explanation]
\```

---

## 4. Usage Examples

[Multiple detailed examples]

---

## 5. Performance Considerations

[Performance tips]

---

## 6. Testing

[Testing examples]

---

**Quay lại:** [BUILD_XX - Main](BUILD_XX_Main.md)
```

---

### **Section: Examples & Usage**

**Required elements:**
- Complete working examples
- Request DTOs
- Response DTOs
- Handler implementation
- Controller usage

```markdown
## [N]. Usage Examples

### Bước [N].1: Complete Example - [Feature Name]

**Request DTOs:**
```csharp
// Request models với full code
public class RequestDto
{
    // Properties với comments
}
\```

**Response DTOs:**
```csharp
// Response models với full code
\```

**Specifications:** (nếu có)
```csharp
// Specification với full code
\```

**Handler:**
```csharp
// Handler implementation với full code
public class FeatureHandler : IRequestHandler<Request, Response>
{
    // Full implementation với comments
}
\```

**Controller Usage:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class FeatureController : ControllerBase
{
    // Controller code
}
\```

**API Call Example:**
```bash
curl -X POST https://localhost:7001/api/endpoint \
  -H "Content-Type: application/json" \
  -d '{
    "key": "value"
  }'
\```

**Expected Response:**
```json
{
  "success": true,
  "data": {...}
}
\```

---
```

---

### **Section: Summary**

**Required elements:**
- Checklist những gì đã hoàn thành
- Architecture diagram (nếu phức tạp)
- Key concepts
- File structure

```markdown
## [N]. Summary

### ✅ Đã hoàn thành trong bước này:

**[Category 1]:**
- ✅ [Item 1]
- ✅ [Item 2]

**[Category 2]:**
- ✅ [Item 1]
- ✅ [Item 2]

### 📊 Architecture Diagram: (nếu phức tạp)

\```
Component A
    │
Component B
    │
Component C
\```

### 📌 Key Concepts:

**[Concept 1]:**
- [Explanation point 1]
- [Explanation point 2]

**[Concept 2]:**
- [Explanation]

### 📁 File Structure:

\```
src/[Project]/
├── Folder1/
│   ├── File1.cs
│   └── File2.cs
└── Folder2/
    └── File3.cs
\```

---
```

---

### **Footer Section**

**Required elements:**
- Next steps với checklist
- Link quay lại index

```markdown
## [N+1]. Next Steps

**Tiếp theo:** [BUILD_[X+1] - Next Module](BUILD_[X+1]_Next_Module.md)

Trong bước tiếp theo, chúng ta sẽ:
1. ✅ [Task 1]
2. ✅ [Task 2]
3. ✅ [Task 3]

---

**Quay lại:** [Mục lục](BUILD_INDEX.md)
```

---

## ❌ Anti-Patterns (Không nên làm)

### ❌ **Không tham chiếu đến code có sẵn**

**Wrong:**
```markdown
**File:** `src/Core/Application/Common/Specification/SpecificationBuilderExtensions.cs`

⚠️ Note: File này đã tồn tại trong workspace. Không cần tạo mới, chỉ cần hiểu cách dùng.
```

**Correct:**
```markdown
**File:** `src/Core/Application/Common/Specification/SpecificationBuilderExtensions.cs`

```csharp
// FULL CODE implementation
using System;

namespace {ProjectName}.Application.Common.Specification;

public static class SpecificationBuilderExtensions
{
    // Full implementation here (500+ lines if needed)
}
\```
```

---

### ❌ **Không code tóm tắt**

**Wrong:**
```markdown
```csharp
public class Product
{
    // ... existing properties ...
    public decimal Price { get; set; }
    // ... more properties ...
}
\```
```

**Correct:**
```markdown
```csharp
using {ProjectName}.Domain.Common.Contracts;

namespace {ProjectName}.Domain.Catalog;

public class Product : AuditableEntity, IAggregateRoot
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    
    private Product() { }
    
    public static Product Create(string name, string description, decimal price, int stock)
    {
        // Full implementation
    }
}
\```
```

---

### ❌ **Không giải thích thuật ngữ tiếng Anh**

**Wrong:**
```markdown
Repository Pattern uses Specification Pattern for flexible queries.
```

**Correct:**
```markdown
**Repository Pattern** (Mẫu Kho lưu trữ) sử dụng **Specification Pattern** (Mẫu đặc tả) để query linh hoạt.

**Specification Pattern là gì?**
- Build complex queries từ simple objects
- Reusable query logic
- Type-safe
```

---

### ❌ **Không có examples thực tế**

**Wrong:**
```markdown
Sử dụng repository để query data.
```

**Correct:**
```markdown
**Example - Search Products:**
```csharp
// Request
var request = new SearchProductsRequest
{
    PageNumber = 1,
    PageSize = 10,
    Keyword = "iphone",
    MinPrice = 500,
    MaxPrice = 2000
};

// Specification
var spec = new ProductsBySearchSpec(request);

// Query
var products = await _repository.ListAsync(spec);
var count = await _repository.CountAsync(spec);

// Result
return new PaginatedResult(products, count);
\```
```

---

## ✅ Checklist khi viết Module Doc

### **Content Checklist**
- [ ] Header với link quay lại index và prerequisites
- [ ] Section 1: Overview với "Làm gì", "Tại sao", Checklist
- [ ] Section 2: Add Required Packages (nếu có)
- [ ] Implementation steps có thứ tự logic
- [ ] **FULL CODE** trong mỗi bước (không tóm tắt)
- [ ] Code có namespace đúng (`{ProjectName}.[Layer].[Module]`)
- [ ] Code có comments giải thích
- [ ] Giải thích sau mỗi code block
- [ ] Examples & Usage với complete code
- [ ] Summary với checklist, diagrams, file structure
- [ ] Next Steps với link đến doc tiếp theo
- [ ] **(Optional)** AI Metadata nếu dùng AI assistance


### **Quality Checklist**
- [ ] Code compile được (test trước khi commit)
- [ ] Namespace đúng chuẩn project
- [ ] File paths chính xác
- [ ] Commands test thành công
- [ ] JSON examples valid
- [ ] Không có typos
- [ ] Formatting nhất quán

### **Vietnamese Language Checklist**
- [ ] Giải thích bằng tiếng Việt
- [ ] Thuật ngữ tiếng Anh có giải thích
- [ ] Comments code dễ hiểu (Việt hoặc Anh rõ ràng)
- [ ] Ví dụ thực tế, gần gũi

### **Complex Module Checklist**
- [ ] Main doc focus vào usage
- [ ] Sub doc (BUILD_XX_DetailName.md) có full implementation
- [ ] Cross-reference giữa main và sub docs
- [ ] Sub doc có: Overview, Full Code, Flow, Examples, Testing

### **Style Checklist**
- [ ] Emojis phù hợp (📚 📖 ✅ ❌ ⚠️ 💡 📁 📌 📊 🔄 📝 🎨)
- [ ] Code blocks có syntax highlighting (\```csharp)
- [ ] Sections có separators (`---`)
- [ ] Lists có indentation đúng
- [ ] Headers có hierarchy rõ (##, ###, ####)

---

## 📁 Naming Convention

### **File Names**
```
BUILD_[Number]_[Module_Name].md
BUILD_[Number]_[DetailName].md (for sub docs)

Examples:
- BUILD_11_Repository_Pattern.md (main)
- BUILD_11_Specification.md (sub doc - implementation details)
- BUILD_14_Authentication.md
- BUILD_15_Authorization.md
```

### **Section Numbers**
```
1. Overview
2. Add Required Packages
3. [Major Component 1]
   3.1. [Specific Task]
   3.2. [Specific Task]
4. [Major Component 2]
   4.1. [Specific Task]
5. Usage Examples
6. Summary
7. Next Steps
```

---

## 📖 Formatting Guidelines

### **Code Blocks**

**C# Code:**
```markdown
```csharp
// FULL CODE với namespace đầy đủ
namespace {ProjectName}.Domain.Catalog;

public class Product
{
    // Full implementation
}
\```
```

**JSON:**
```markdown
```json
{
  "key": "value",
  "nested": {
    "key": "value"
  }
}
\```
```

**Bash/PowerShell:**
```markdown
```bash
# Commands
dotnet build
dotnet run
\```

```powershell
# PowerShell commands
Get-Process | Where-Object Name -like "dotnet*"
\```
```

---

### **Tables**

```markdown
| Column 1 | Column 2 | Column 3 |
|----------|----------|----------|
| Value 1  | Value 2  | Value 3  |
| Value 4  | Value 5  | Value 6  |
```

---

### **Callouts**

```markdown
> ⚠️ **Warning:** Critical information  
> 📌 **Note:** Helpful information  
> 💡 **Tip:** Pro tip  
> 📋 **Prerequisites:** Required steps  
> ❌ **Don't:** Anti-pattern
```

---

### **Emojis Usage**

**Standard emojis:**
- 📚 Documentation/Back to index
- 📋 Prerequisites/Checklist
- ✅ Completed/Correct/Do this
- ❌ Wrong/Don't do this
- ⚠️ Warning/Important
- 💡 Tip/Idea
- 📁 File structure
- 📌 Key points/Concepts
- 📊 Diagram/Chart
- 🔄 Flow/Process
- 📝 Notes/Documentation
- 🎨 Formatting/Style

---

### **Cross-Referencing**

**Internal Links:**
```markdown
[Link Text](BUILD_01_Solution_Setup.md)
[Specific Section](BUILD_01_Solution_Setup.md#section-anchor)
[Sub Document](BUILD_11_Specification.md)
```

**External Links:**
```markdown
[External Resource](https://docs.microsoft.com/...)
```

---

## 📖 Complete Example Template

**File:** `BUILD_XX_Feature_Name.md`

```markdown
# Feature Name - Short Description

> 📚 [Quay lại Mục lục](BUILD_INDEX.md)  
> 📋 **Prerequisites:** Bước [X-1] ([Previous Module]) đã hoàn thành

Tài liệu này hướng dẫn xây dựng [Feature Name] - [Purpose].

---

## 1. Overview

**Làm gì:** [Description]

**Tại sao cần:**
- **[Reason 1]:** [Explanation]
- **[Reason 2]:** [Explanation]
- **[Reason 3]:** [Explanation]

**Trong bước này chúng ta sẽ:**
- ✅ [Task 1]
- ✅ [Task 2]
- ✅ [Task 3]
- ✅ [Task 4]

**Real-world example:**
```csharp
// Usage example
\```

---

## 2. Add Required Packages

### Bước 2.1: [Package Group]

**File:** `src/[Project]/[Project].csproj`

```xml
<ItemGroup>
    <PackageReference Include="PackageName" Version="x.x.x" />
</ItemGroup>
\```

**Giải thích:**
- `PackageName`: [Why need this]

---

## 3. [Major Component]

### Bước 3.1: [Task Name]

**Làm gì:** [Description]

**Tại sao:** [Reason]

**File:** `src/[Project]/[Path]/[FileName].cs`

```csharp
// FULL CODE
namespace {ProjectName}.[Namespace];

public class ClassName
{
    // Implementation
}
\```

**Giải thích:**
- [Explanation]

**Tại sao [Design Decision]:**
- [Reason]

**Lợi ích:**
- ✅ [Benefit]

---

### Bước 3.2: [Next Task]

[Repeat pattern]

---

## 4. Usage Examples

### Bước 4.1: Complete Example - [Feature]

**Request:**
```csharp
// DTOs
\```

**Handler:**
```csharp
// Implementation
\```

**API Call:**
```bash
curl -X POST https://localhost:7001/api/endpoint
\```

**Response:**
```json
{ "success": true }
\```

---

## 5. Summary

### ✅ Đã hoàn thành:

**[Category]:**
- ✅ [Item]

### 📊 Architecture:

\```
[Diagram]
\```

### 📌 Key Concepts:

**[Concept]:**
- [Point]

### 📁 File Structure:

\```
src/
├── [Structure]
\```

---

## 6. Next Steps

**Tiếp theo:** [BUILD_[X+1] - Next Module](BUILD_[X+1]_Next_Module.md)

Trong bước tiếp theo:
1. ✅ [Task 1]
2. ✅ [Task 2]

---

**Quay lại:** [Mục lục](BUILD_INDEX.md)
```

---


```

```


---

## 📝 Final Notes

### **Khi viết docs, hãy nhớ:**

1. **Self-Contained (CỰC KỲ QUAN TRỌNG!):** Code đầy đủ, không reference workspace
2. **Tiếng Việt:** Giải thích dễ hiểu, thuật ngữ có giải thích
3. **Progressive Disclosure:** Main doc (usage) + Sub doc (implementation) nếu phức tạp  
4. **Examples First:** Ví dụ thực tế trước theory - người ta học qua examples
5. **Quality Gates:** Code compile, paths đúng, formatting nhất quán

### **Mục tiêu cuối cùng:**

> **Documentation-Driven Development:**  
> Bất kỳ developer nào đọc docs này đều có thể TẠO LẠI toàn bộ solution 
> từ con số 0, chỉ cần follow từng bước trong docs.

**Test để verify:** Delete solution, rebuild chỉ từ docs → `dotnet build` thành công! ✅


---

**Sử dụng template này để viết documentation nhất quán và chất lượng cao cho {ProjectName}!** 📚

☑️ Save files with a specific encoding
Unicode (UTF-8 with signature) - Code page 65001
