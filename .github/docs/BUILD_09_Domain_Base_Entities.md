# BUILD_09 - Domain Base Entities và Domain Events

> 📚 [Quay lại Mục lục](BUILD_INDEX.md)  
> 📋 **Prerequisites:** BUILD_08 (Database Initialization) đã hoàn thành  
> ⏱️ **Thời gian:** Khoảng 20 phút

Tài liệu này hướng dẫn về Domain Base Entities và Domain Events - nền tảng cho tất cả entities trong hệ thống.

---

## 1. Overview

**Làm gì:** Tạo base entities và domain events foundation cho toàn bộ domain layer.

**Tại sao cần:**
- **DDD Foundation:** Nền tảng cho Domain-Driven Design
- **Event-Driven:** Hỗ trợ domain events và event sourcing
- **Consistency:** Tất cả entities follow cùng pattern
- **Audit Trail:** Track changes với CreatedBy, UpdatedBy, etc.

**Trong bước này chúng ta sẽ:**
- ✅ Tạo IEvent interface (domain event marker)
- ✅ Tạo IEntity interface và base contracts
- ✅ Tạo DomainEvent base class
- ✅ Tạo BaseEntity và AuditableEntity
- ✅ Tạo IAggregateRoot marker interface
- ✅ Tạo entity lifecycle events (Created, Updated, Deleted)

---

## 2. Add Required Packages

### Bước 2.1: Add NewId Package

**File:** `src/Domain/Domain.csproj`

```xml
<ItemGroup>
 <!-- For sequential GUID generation -->
    <PackageReference Include="NewId" Version="4.0.1" />
</ItemGroup>
```

**Why NewId:**
- `NewId.Next().ToGuid()` tạo sequential GUIDs
- Better database performance (less fragmentation)
- Better indexing performance

---

## 3. Tạo Domain Event Contracts

### Bước 3.1: IEvent Interface

**File:** `src/Domain/Common/Contracts/IEvent.cs`

```csharp
namespace {ProjectName}.Domain.Common.Contracts;

/// <summary>
/// Marker interface for all domain events
/// </summary>
public interface IEvent
{
}
```

**Giải thích:**
- Marker interface - không có methods
- Đánh dấu class là một domain event
- Tất cả domain events phải implement interface này

**Why in Domain layer:**
- Events là domain concept (business logic)
- Không phải infrastructure concern
- Follow DDD principles

---

### Bước 3.2: DomainEvent Base Class

**File:** `src/Domain/Common/Contracts/DomainEvent.cs`

```csharp
namespace {ProjectName}.Domain.Common.Contracts;

/// <summary>
/// Base class for all domain events
/// </summary>
public abstract class DomainEvent : IEvent
{
    /// <summary>
    /// When this event was triggered
    /// </summary>
    public DateTime TriggeredOn { get; protected set; } = DateTime.UtcNow;
}
```

**Giải thích:**
- Abstract class: Không thể instantiate trực tiếp
- `TriggeredOn`: Timestamp tự động khi event được tạo
- Kế thừa từ `IEvent` (đã tạo ở Bước 3.1)

**Dependencies:**
```
IEvent (Domain.Common.Contracts)
 ↓
DomainEvent (Domain.Common.Contracts)
    ↓
EntityCreatedEvent, EntityUpdatedEvent, etc.
```

---

## 4. Tạo Entity Contracts

### Bước 4.1: IEntity Interface

**File:** `src/Domain/Common/Contracts/IEntity.cs`

```csharp
namespace {ProjectName}.Domain.Common.Contracts;

/// <summary>
/// Base interface for all entities
/// </summary>
public interface IEntity
{
    List<DomainEvent> DomainEvents { get; }
}

/// <summary>
/// Base interface for entities with typed Id
/// </summary>
public interface IEntity<TId> : IEntity
{
    TId Id { get; }
}
```

**Giải thích:**
- `IEntity`: Tất cả entities phải có DomainEvents collection
- `IEntity<TId>`: Generic Id support (Guid, int, string, etc.)

---

## 5. Tạo BaseEntity

### Bước 5.1: BaseEntity Generic

**File:** `src/Domain/Common/Contracts/BaseEntity.cs`

```csharp
using System.ComponentModel.DataAnnotations.Schema;
using MassTransit;

namespace {ProjectName}.Domain.Common.Contracts;

/// <summary>
/// Base entity with generic Id type
/// </summary>
public abstract class BaseEntity<TId> : IEntity<TId>
{
    public TId Id { get; protected set; } = default!;

    [NotMapped]
    public List<DomainEvent> DomainEvents { get; } = new();
}

/// <summary>
/// Base entity with Guid Id (most common case)
/// </summary>
public abstract class BaseEntity : BaseEntity<Guid>
{
    protected BaseEntity() => Id = NewId.Next().ToGuid();
}
```

**Key Features:**
- `protected set`: Chỉ derived classes có thể set Id
- `[NotMapped]`: DomainEvents không persist vào database
- `NewId.Next().ToGuid()`: Sequential GUID generation

**Why Sequential GUIDs:**
```
Regular GUID:  a1b2c3d4-...  (random)
Sequential:    00000001-...  (ordered)

Benefits:
- Better database indexing
- Less fragmentation
- Faster inserts
```

---

### Bước 5.2: AuditableEntity

**File:** `src/Domain/Common/Contracts/AuditableEntity.cs`

```csharp
namespace {ProjectName}.Domain.Common.Contracts;

/// <summary>
/// Entity with audit trail (Created/Updated by/on)
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public Guid CreatedBy { get; set; }
    public DateTime CreatedOn { get; private set; }

    public Guid LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }

    protected AuditableEntity()
    {
        CreatedOn = DateTime.UtcNow;
        LastModifiedOn = DateTime.UtcNow;
    }
}

/// <summary>
/// Auditable entity with generic Id type
/// </summary>
public abstract class AuditableEntity<TId> : BaseEntity<TId>
{
    public Guid CreatedBy { get; set; }
    public DateTime CreatedOn { get; private set; }
    
    public Guid LastModifiedBy { get; set; }
    public DateTime? LastModifiedOn { get; set; }

    protected AuditableEntity()
    {
        CreatedOn = DateTime.UtcNow;
        LastModifiedOn = DateTime.UtcNow;
    }
}
```

**Audit Fields:**
- `CreatedBy`: User Id who created entity
- `CreatedOn`: When entity was created (auto-set in constructor)
- `LastModifiedBy`: User Id who last modified entity
- `LastModifiedOn`: When entity was last modified

**Usage:**
```csharp
// For simple entities without audit
public class Category : BaseEntity { }

// For entities requiring audit trail
public class Product : AuditableEntity { }
```

---

## 6. Tạo IAggregateRoot

### Bước 6.1: IAggregateRoot Interface

**File:** `src/Domain/Common/Contracts/IAggregateRoot.cs`

```csharp
namespace {ProjectName}.Domain.Common.Contracts;

/// <summary>
/// Marker interface for aggregate root entities.
/// Repositories should only work with aggregate roots, not their children.
/// </summary>
public interface IAggregateRoot : IEntity
{
}
```

**DDD Aggregate Pattern:**

```csharp
// ✅ Aggregate Root - can be accessed from outside
public class Order : AuditableEntity, IAggregateRoot
{
    public string OrderNumber { get; set; }
    public List<OrderItem> Items { get; private set; } = new();
    
    // Business logic
    public void AddItem(Product product, int quantity)
    {
        var item = new OrderItem(product.Id, quantity);
        Items.Add(item);
    }
}

// ❌ Child entity - accessed only through Order
public class OrderItem : BaseEntity
{
    public Guid OrderId { get; set; }
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    
    // No public constructor
    internal OrderItem(Guid productId, int quantity)
    {
     ProductId = productId;
    Quantity = quantity;
    }
}
```

**Repository Rules:**
```csharp
// ✅ Allowed - Aggregate Root
IRepository<Order>

// ❌ Not allowed - Child entity
IRepository<OrderItem> // Compile error
```

---

## 7. Tạo Các Sự Kiện Vòng Đời Entity (Entity Lifecycle Events)

### 📚 **Tổng quan về Entity Lifecycle Events**

**Lifecycle Events là gì?**
- Là các sự kiện tự động phát ra khi entity có thay đổi trạng thái
- Giống như "thông báo" khi có điều gì đó xảy ra với entity
- 3 loại chính: **Created** (Tạo mới), **Updated** (Cập nhật), **Deleted** (Xóa)

**Tại sao cần Events?**
- **Tách biệt logic:** Không cần viết code xử lý trực tiếp trong Repository
- **Dễ mở rộng:** Thêm tính năng mới mà không sửa code cũ
- **Linh hoạt:** Nhiều module có thể lắng nghe cùng 1 event

**Ví dụ thực tế:**
```
Khi tạo Product mới:
- Event "ProductCreated" được phát
- Email Service nghe event → Gửi email thông báo
- Cache Service nghe event → Xóa cache cũ
- Audit Service nghe event → Ghi log
```

---

### Bước 7.1: EntityCreatedEvent - Sự Kiện Tạo Mới

**File:** `src/Domain/Common/Events/EntityCreatedEvent.cs`

```csharp
using {ProjectName}.Domain.Common.Contracts;

namespace {ProjectName}.Domain.Common.Events;

// Static class - chứa hàm factory tạo event
public static class EntityCreatedEvent
{
    // Factory method: Tạo event dễ dàng với type inference
    public static EntityCreatedEvent<TEntity> WithEntity<TEntity>(TEntity entity)
 where TEntity : IEntity
        => new(entity);
}

// Generic class - chứa thông tin entity được tạo
public class EntityCreatedEvent<TEntity> : DomainEvent
    where TEntity : IEntity
{
    // Entity vừa được tạo
    public TEntity Entity { get; }
    
    // Constructor internal - chỉ factory method mới tạo được
    internal EntityCreatedEvent(TEntity entity) => Entity = entity;
}
```

**Giải thích chi tiết:**

**1. Static class `EntityCreatedEvent`:**
```csharp
public static class EntityCreatedEvent
{
    public static EntityCreatedEvent<TEntity> WithEntity<TEntity>(...)
}
```
- Đây là **class tĩnh** chứa hàm factory
- **Factory method** `WithEntity()` giúp tạo event dễ dàng
- Không cần chỉ định kiểu dữ liệu, compiler tự suy luận

**Ví dụ sử dụng:**
```csharp
var product = new Product { Name = "iPhone 15" };

// ✅ Dùng factory - ngắn gọn, compiler tự biết type
var event = EntityCreatedEvent.WithEntity(product);

// ❌ Tạo trực tiếp - dài dòng, phải chỉ định type
var event = new EntityCreatedEvent<Product>(product);
```

**2. Generic class `EntityCreatedEvent<TEntity>`:**
```csharp
public class EntityCreatedEvent<TEntity> : DomainEvent
    where TEntity : IEntity
{
    // Entity vừa được tạo
    public TEntity Entity { get; }
    
    // Constructor internal - chỉ factory method mới tạo được
    internal EntityCreatedEvent(TEntity entity) => Entity = entity;
}
```

**Các thành phần:**

**a) Generic type `<TEntity>`:**
- `TEntity` là kiểu dữ liệu của entity (Product, Order, User, etc.)
- `where TEntity : IEntity` → Chỉ chấp nhận các class kế thừa IEntity

**b) Kế thừa `DomainEvent`:**
```csharp
public class EntityCreatedEvent<TEntity> : DomainEvent
```
- Tự động có thuộc tính `TriggeredOn` (thời gian phát event)
- Kế thừa `IEvent` interface

**c) Property `Entity`:**
```csharp
public TEntity Entity { get; }
```
- Lưu trữ entity vừa được tạo
- **Read-only** (chỉ có get, không có set)

**d) Constructor `internal`:**
```csharp
internal EntityCreatedEvent(TEntity entity) => Entity = entity;
```
- **internal** → Chỉ code bên trong project mới tạo được
- Bắt buộc phải dùng factory method `WithEntity()`

---

### Bước 7.2: EntityUpdatedEvent

**File:** `src/Domain/Common/Events/EntityUpdatedEvent.cs`

```csharp
using {ProjectName}.Domain.Common.Contracts;

namespace {ProjectName}.Domain.Common.Events;

public static class EntityUpdatedEvent
{
    public static EntityUpdatedEvent<TEntity> WithEntity<TEntity>(TEntity entity)
   where TEntity : IEntity
        => new(entity);
}

public class EntityUpdatedEvent<TEntity> : DomainEvent
 where TEntity : IEntity
{
    public TEntity Entity { get; }
    
    internal EntityUpdatedEvent(TEntity entity) => Entity = entity;
}
```

---

### Bước 7.3: EntityDeletedEvent

**File:** `src/Domain/Common/Events/EntityDeletedEvent.cs`

```csharp
using {ProjectName}.Domain.Common.Contracts;

namespace {ProjectName}.Domain.Common.Events;

public static class EntityDeletedEvent
{
  public static EntityDeletedEvent<TEntity> WithEntity<TEntity>(TEntity entity)
   where TEntity : IEntity
        => new(entity);
}

public class EntityDeletedEvent<TEntity> : DomainEvent
    where TEntity : IEntity
{
    public TEntity Entity { get; }
    
    internal EntityDeletedEvent(TEntity entity) => Entity = entity;
}
```

---

### 💡 **Event Pattern - Giải thích kỹ hơn**

**1. Static Factory Method Pattern:**

```csharp
// Thay vì:
var event = new EntityCreatedEvent<Product>(product);  // Dài
var event = new EntityCreatedEvent<Order>(order);      // Dài
var event = new EntityCreatedEvent<User>(user);  // Dài

// Ta dùng:
var event = EntityCreatedEvent.WithEntity(product);    // Ngắn
var event = EntityCreatedEvent.WithEntity(order);      // Ngắn
var event = EntityCreatedEvent.WithEntity(user);    // Ngắn
```

**Lợi ích:**
- **Type inference:** Compiler tự biết `TEntity` là gì
- **Ngắn gọn:** Không cần viết `<Product>`, `<Order>`, etc.
- **Consistent:** Tất cả events đều tạo theo cùng pattern

**2. Internal Constructor:**

```csharp
internal EntityCreatedEvent(TEntity entity) => Entity = entity;
```

**Tại sao internal?**
- **Kiểm soát:** Chỉ factory method mới tạo được event
- **Bảo mật:** Code bên ngoài không thể tạo event tùy tiện
- **Consistency:** Tất cả events đều tạo qua factory

```csharp
// ✅ Được phép - qua factory
var event = EntityCreatedEvent.WithEntity(product);

// ❌ Không được phép - constructor là internal
var event = new EntityCreatedEvent<Product>(product); // Compile error nếu ở project khác
```

---

### 🎯 **Cách Events được sử dụng**

**1. Repository tự động thêm events:**

```csharp
// Trong Repository - CODE TỰ ĐỘNG (decorator pattern)
public async Task<Product> AddAsync(Product product)
{
    // ✅ Event tự động được thêm vào DomainEvents collection
    product.DomainEvents.Add(
        EntityCreatedEvent.WithEntity(product)
    );
    
    await _dbContext.Products.AddAsync(product);
    await _dbContext.SaveChangesAsync(); // SaveChanges sẽ publish events

    return product;
}
```

**2. DbContext publish events khi SaveChanges:**

```csharp
// Trong ApplicationDbContext.SaveChangesAsync()
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
{
    // 1. Lấy tất cả entities có DomainEvents
    var entitiesWithEvents = ChangeTracker.Entries<IEntity>()
        .Where(e => e.Entity.DomainEvents.Any())
        .ToList();
    
// 2. Publish từng event qua MediatR
    foreach (var entry in entitiesWithEvents)
    {
        foreach (var domainEvent in entry.Entity.DomainEvents)
        {
await _mediator.Publish(domainEvent, cancellationToken);
        }
 
  // 3. Clear events sau khi publish
        entry.Entity.DomainEvents.Clear();
  }
    
  // 4. Save changes vào database
    return await base.SaveChangesAsync(cancellationToken);
}
```

**3. Event Handlers xử lý events:**

```csharp
// Email handler - Gửi email khi product được tạo
public class ProductCreatedEmailHandler 
    : INotificationHandler<EntityCreatedEvent<Product>>
{
    private readonly IEmailService _emailService;
    
    public ProductCreatedEmailHandler(IEmailService emailService)
    {
    _emailService = emailService;
    }
    
    public async Task Handle(
 EntityCreatedEvent<Product> notification, 
   CancellationToken cancellationToken)
    {
        var product = notification.Entity;
        
      await _emailService.SendAsync(
            to: "admin@example.com",
            subject: "Sản phẩm mới",
      body: $"Sản phẩm '{product.Name}' vừa được tạo với giá {product.Price:C}"
        );
    }
}

// Cache handler - Xóa cache khi product được update
public class ProductUpdatedCacheHandler 
    : INotificationHandler<EntityUpdatedEvent<Product>>
{
    private readonly ICacheService _cacheService;
    
    public ProductUpdatedCacheHandler(ICacheService cacheService)
    {
        _cacheService = cacheService;
 }
    
    public async Task Handle(
EntityUpdatedEvent<Product> notification, 
        CancellationToken cancellationToken)
 {
        var product = notification.Entity;
        
        // Xóa cache cũ
        await _cacheService.RemoveAsync($"product:{product.Id}");
        await _cacheService.RemoveAsync("products:all");
    }
}
```

---

### 📊 **Flow hoàn chỉnh:**

```
1. User gọi API: POST /api/products
    ↓
2. ProductService.CreateAsync(dto)
↓
3. Product được tạo: var product = Product.Create(...)
  ↓
4. Repository.AddAsync(product)
    ↓ 
5. Decorator thêm event: product.DomainEvents.Add(EntityCreatedEvent.WithEntity(product))
    ↓
6. DbContext.SaveChangesAsync()
  ↓
7. Detect entities có DomainEvents
    ↓
8. Publish từng event qua MediatR
    ↓
9. MediatR gọi tất cả handlers:
    - ProductCreatedEmailHandler → Gửi email
    - ProductCreatedCacheHandler → Update cache
    - ProductCreatedAuditHandler → Ghi log
    ↓
10. Clear DomainEvents collection
    ↓
11. Commit transaction vào database
    ↓
12. Trả response về cho User
```

---

### ✨ **Lợi ích của pattern này:**

**1. Separation of Concerns (Tách biệt mối quan tâm):**
```csharp
// Repository chỉ lo thêm/sửa/xóa database
// Không cần biết về email, cache, audit, etc.

public async Task<Product> AddAsync(Product product)
{
    await _dbContext.Products.AddAsync(product);
    await _dbContext.SaveChangesAsync();
    return product; // DONE! Không cần code gì thêm
}
```

**2. Open/Closed Principle (Mở để mở rộng, đóng để sửa đổi):**
```csharp
// Thêm tính năng mới mà KHÔNG sửa code cũ

// Thêm SMS handler
public class ProductCreatedSmsHandler 
    : INotificationHandler<EntityCreatedEvent<Product>>
{
    // Send SMS khi product được tạo
}

// Thêm analytics handler
public class ProductCreatedAnalyticsHandler 
    : INotificationHandler<EntityCreatedEvent<Product>>
{
    // Track analytics
}

// Repository code KHÔNG cần sửa gì!
```

**3. Testable (Dễ test):**
```csharp
[Fact]
public async Task Handle_ShouldSendEmail_WhenProductCreated()
{
    // Arrange
    var emailServiceMock = new Mock<IEmailService>();
    var handler = new ProductCreatedEmailHandler(emailServiceMock.Object);
    var product = Product.Create("Test", "Desc", 100m, 10);
    var notification = EntityCreatedEvent.WithEntity(product);
    
    // Act
    await handler.Handle(notification, CancellationToken.None);
    
  // Assert
    emailServiceMock.Verify(x => x.SendAsync(
      It.IsAny<string>(),
        "Sản phẩm mới",
        It.IsAny<string>()
    ), Times.Once);
}
```

**4. Loose Coupling (Liên kết lỏng):**
```
ProductService → Không biết EmailService
ProductService → Không biết CacheService
ProductService → Không biết AuditService

Tất cả chỉ biết về DomainEvent!
```

---

### 🎓 **Tóm tắt Bước 7:**

**Ba loại event:**
1. **EntityCreatedEvent** - Khi tạo mới entity
2. **EntityUpdatedEvent** - Khi cập nhật entity
3. **EntityDeletedEvent** - Khi xóa entity

**Pattern sử dụng:**
- **Static factory method** - Tạo event dễ dàng
- **Generic type** - Type-safe cho mọi entity
- **Internal constructor** - Kiểm soát việc tạo event

**Flow hoạt động:**
1. Repository thêm event vào `DomainEvents` collection
2. `SaveChangesAsync()` publish events qua MediatR
3. Event handlers nhận và xử lý
4. Events được clear sau khi xử lý

**Lợi ích:**
- Tách biệt logic business
- Dễ mở rộng tính năng mới
- Dễ test
- Giảm coupling giữa các module

---

## 8. Example Domain Entity

### Bước 8.1: Sample Product Entity

**File:** `src/Domain/Catalog/Product.cs` (example)

```csharp
using {ProjectName}.Domain.Common.Contracts;

namespace {ProjectName}.Domain.Catalog;

public class Product : AuditableEntity, IAggregateRoot
{
    public string Name { get; private set; } = default!;
    public string Description { get; private set; } = default!;
    public decimal Price { get; private set; }
    public int Stock { get; private set; }

    // EF Core constructor
    private Product() { }

    // Factory method
    public static Product Create(string name, string description, decimal price, int stock)
    {
    var product = new Product
        {
      Name = name,
         Description = description;
  Price = price,
  Stock = stock
        };

 return product;
    }

    // Business logic
    public void UpdatePrice(decimal newPrice)
    {
  if (newPrice < 0)
     throw new InvalidOperationException("Price cannot be negative");

      Price = newPrice;
    }

    public void ReduceStock(int quantity)
    {
      if (Stock < quantity)
            throw new InvalidOperationException("Insufficient stock");

      Stock -= quantity;
    }
}
```

**Best Practices:**
- ✅ Private setters - encapsulation
- ✅ Factory method - controlled instantiation
- ✅ Business logic methods - not just getters/setters
- ✅ Validation - in domain logic
- ✅ IAggregateRoot - can be accessed by repository

---

## 9. Summary

### ✅ Đã hoàn thành:

**Domain Event Contracts:**
- ✅ IEvent interface
- ✅ DomainEvent base class

**Entity Contracts:**
- ✅ IEntity interface
- ✅ BaseEntity (sequential GUID)
- ✅ AuditableEntity (audit trail)
- ✅ IAggregateRoot

**Entity Lifecycle Events:**
- ✅ EntityCreatedEvent
- ✅ EntityUpdatedEvent
- ✅ EntityDeletedEvent

### 📁 File Structure:

```
src\Domain\Common\
├── Contracts\
│   ├── IEvent.cs
│   ├── IEntity.cs
│   ├── DomainEvent.cs
│   ├── BaseEntity.cs
│   ├── AuditableEntity.cs
│   └── IAggregateRoot.cs
└── Events\
    ├── EntityCreatedEvent.cs
    ├── EntityUpdatedEvent.cs
    └── EntityDeletedEvent.cs
```

---

## 10. Bước tiếp theo

**Tiếp theo:** [BUILD_10 - Service Registration Pattern](BUILD_10_Service_Registration.md)

---

**Quay lại:** [Mục lục](BUILD_INDEX.md)
