using InventoryManagement.Application.DTOs;
using InventoryManagement.Domain.Entities;
namespace InventoryManagement.Application.Interfaces;

public interface IInventoryRepository
{
    Task<IReadOnlyList<ProductDto>> SearchAsync(string? search, CancellationToken ct);
    Task<ProductDto?> GetAsync(int id, CancellationToken ct);
    Task<bool> SkuExistsAsync(string sku, int? excludingId, CancellationToken ct);
    Task<int> CreateAsync(Product product, CancellationToken ct);
    Task UpdateAsync(int id, ProductInput input, CancellationToken ct);
    Task<IReadOnlyList<MovementDto>> HistoryAsync(int productId, CancellationToken ct);
    // Locks the product before invoking the application policy and holds it until commit.
    Task<long> RecordAsync(StockMovement movement, Action<Product, long> validate, CancellationToken ct);
}
