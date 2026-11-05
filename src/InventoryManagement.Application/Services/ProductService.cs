using InventoryManagement.Application.DTOs;
using InventoryManagement.Application.Exceptions;
using InventoryManagement.Application.Interfaces;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Exceptions;
namespace InventoryManagement.Application.Services;

public sealed class ProductService(IInventoryRepository repository)
{
    public Task<IReadOnlyList<ProductDto>> SearchAsync(string? search, CancellationToken ct = default) => repository.SearchAsync(search?.Trim(), ct);
    public async Task<ProductDto> GetAsync(int id, CancellationToken ct = default) => await repository.GetAsync(id, ct) ?? throw new NotFoundException();
    public async Task<int> CreateAsync(ProductInput input, CancellationToken ct = default)
    {
        var product = new Product(input.Sku, input.Name, input.Description, input.ReorderLevel);
        await CheckSku(product.Sku, null, ct);
        return await repository.CreateAsync(product, ct);
    }
    public async Task UpdateAsync(int id, ProductInput input, CancellationToken ct = default)
    {
        _ = new Product(input.Sku, input.Name, input.Description, input.ReorderLevel);
        await GetAsync(id, ct);
        await CheckSku(input.Sku.Trim().ToUpperInvariant(), id, ct);
        await repository.UpdateAsync(id, input, ct);
    }
    private async Task CheckSku(string sku, int? id, CancellationToken ct)
    {
        if (await repository.SkuExistsAsync(sku, id, ct)) throw new BusinessRuleException("A product with this SKU already exists.");
    }
}
