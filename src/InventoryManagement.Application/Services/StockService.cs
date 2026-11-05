using InventoryManagement.Application.DTOs;
using InventoryManagement.Application.Interfaces;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Exceptions;
namespace InventoryManagement.Application.Services;

public sealed class StockService(IInventoryRepository repository, ProductService products)
{
    public async Task<long> GetCurrentStockAsync(int productId, CancellationToken ct = default) => (await products.GetAsync(productId, ct)).CurrentStock;
    public Task<long> RecordStockInAsync(int productId, int quantity, string? note, Guid requestId, CancellationToken ct = default)
        => RecordAsync(productId, MovementType.In, quantity, note, requestId, ct);
    public Task<long> RecordStockOutAsync(int productId, int quantity, string? note, Guid requestId, CancellationToken ct = default)
        => RecordAsync(productId, MovementType.Out, quantity, note, requestId, ct);
    public Task<long> RecordAsync(int productId, MovementType type, int quantity, string? note, Guid requestId, CancellationToken ct = default)
    {
        var movement = new StockMovement(productId, type, quantity, note, requestId);
        return repository.RecordAsync(movement, (product, available) =>
        {
            if (!product.IsActive) throw new BusinessRuleException("Inactive products cannot receive stock movements.");
            if (type == MovementType.Out) StockRules.EnsureAvailable(available, quantity);
        }, ct);
    }
    public async Task<IReadOnlyList<MovementDto>> GetStockMovementHistoryAsync(int productId, CancellationToken ct = default)
    {
        await products.GetAsync(productId, ct);
        return await repository.HistoryAsync(productId, ct);
    }
}
