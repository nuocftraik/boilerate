using Boilerate.Application.Common.FileStorage;
using Boilerate.Application.Common.Persistence;
using Boilerate.Application.Common.Caching;
using Boilerate.Application.Common.Exceptions;
using Boilerate.Application.Common.Extensions;
using Boilerate.Domain.Catalog;
using Boilerate.Domain.Common;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Boilerate.Application.Catalog.Products;

public class UpdateProductRequest : IRequest<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public Guid CategoryId { get; set; }
    public bool DeleteCurrentImage { get; set; }
    public Microsoft.AspNetCore.Http.IFormFile? Image { get; set; }
}

public class UpdateProductRequestValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductRequestValidator(IReadRepository<Product> productRepo, IReadRepository<Category> categoryRepo)
    {
        RuleFor(p => p.Name)
            .NotEmpty()
            .MaximumLength(75)
            .MustAsync(async (req, name, ct) => 
                await productRepo.GetBySpecAsync(new ProductByNameSpec(name), ct)
                    is not Product existingProduct || existingProduct.Id == req.Id)
                .WithMessage((_, name) => $"Sản phẩm {name} đã tồn tại.");

        RuleFor(p => p.CategoryId)
            .NotEmpty()
            .MustAsync(async (id, ct) => await categoryRepo.GetByIdAsync(id, ct) is not null)
                .WithMessage((_, id) => $"Danh mục với Id {id} không tồn tại.");

        RuleFor(p => p.Price)
            .GreaterThanOrEqualTo(0);
    }
}

public class UpdateProductRequestHandler : IRequestHandler<UpdateProductRequest, Guid>
{
    private readonly IRepositoryWithEvents<Product> _repository;
    private readonly IFileStorageService _file;
    private readonly ICacheService _cache;
    private readonly ILogger<UpdateProductRequestHandler> _logger;

    public UpdateProductRequestHandler(IRepositoryWithEvents<Product> repository, IFileStorageService file, ICacheService cache, ILogger<UpdateProductRequestHandler> logger)
    {
        _repository = repository;
        _file = file;
        _cache = cache;
        _logger = logger;
    }

    public async Task<Guid> Handle(UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Sản phẩm với Id {request.Id} không tồn tại.");

        if (product.IsDeleted())
        {
            _logger.LogWarning("Ai đó đang cố gắng cập nhật một sản phẩm đã bị xóa mềm! [ProductId={Id}]", request.Id);
            throw new InvalidOperationException("Không thể cập nhật sản phẩm đã bị xóa. Vui lòng khôi phục trước.");
        }

        // Xóa hệ lưu trữ hình ảnh nếu được yêu cầu
        if (request.DeleteCurrentImage && !string.IsNullOrEmpty(product.ImagePath))
        {
            _file.Remove(product.ImagePath);
            product.ImagePath = null;
        }

        // Tải ảnh rỗng / upload ảnh mới
        string? imagePath = request.Image is not null
            ? await _file.UploadAsync<Product>(request.Image, FileType.Image, cancellationToken)
            : null;

        // Xoá ảnh cũ nếu có ảnh mới up lên mà người dùng không tick xóa thủ công
        if (imagePath is not null && !request.DeleteCurrentImage && !string.IsNullOrEmpty(product.ImagePath))
        {
            _file.Remove(product.ImagePath);
        }

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.CategoryId = request.CategoryId;
        
        if (imagePath is not null)
        {
            product.ImagePath = imagePath;
        }

        await _repository.UpdateAsync(product, cancellationToken);
        await _cache.RemoveAsync($"Product:{request.Id}", cancellationToken);

        return request.Id;
    }
}
