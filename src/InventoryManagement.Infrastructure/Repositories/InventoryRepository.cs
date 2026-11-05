using System.Data;
using InventoryManagement.Application.DTOs;
using InventoryManagement.Application.Exceptions;
using InventoryManagement.Application.Interfaces;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Exceptions;
using InventoryManagement.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace InventoryManagement.Infrastructure.Repositories;

public sealed class InventoryRepository(ApplicationDbContext db, ILogger<InventoryRepository> logger) : IInventoryRepository
{
    private IQueryable<ProductDto> Products => db.Products.AsNoTracking().Select(p => new ProductDto(
        p.Id, p.Sku, p.Name, p.Description, p.ReorderLevel, p.IsActive, p.CreatedUtc,
        db.StockMovements.Where(m => m.ProductId == p.Id).Sum(m => m.Type == MovementType.In ? (long)m.Quantity : -(long)m.Quantity)));
    public async Task<IReadOnlyList<ProductDto>> SearchAsync(string? search, CancellationToken ct)
    {
        var query = Products;
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(p => p.Sku.Contains(search) || p.Name.Contains(search));
        return await query.OrderBy(p => p.Name).ToListAsync(ct);
    }
    public Task<ProductDto?> GetAsync(int id, CancellationToken ct) => Products.SingleOrDefaultAsync(p => p.Id == id, ct);
    public Task<bool> SkuExistsAsync(string sku, int? excludingId, CancellationToken ct) => db.Products.AnyAsync(p => p.Sku == sku && (!excludingId.HasValue || p.Id != excludingId), ct);
    public async Task<int> CreateAsync(Product product, CancellationToken ct)
    {
        db.Products.Add(product);
        await SaveProduct(ct);
        return product.Id;
    }
    public async Task UpdateAsync(int id, ProductInput input, CancellationToken ct)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == id, ct) ?? throw new NotFoundException();
        product.Update(input.Sku, input.Name, input.Description, input.ReorderLevel, input.IsActive);
        await SaveProduct(ct);
    }
    private async Task SaveProduct(CancellationToken ct)
    {
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
        { throw new BusinessRuleException("A product with this SKU already exists."); }
    }
    public async Task<IReadOnlyList<MovementDto>> HistoryAsync(int productId, CancellationToken ct) =>
        await db.StockMovements.AsNoTracking().Where(m => m.ProductId == productId)
            .OrderByDescending(m => m.CreatedUtc).ThenByDescending(m => m.Id)
            .Select(m => new MovementDto(m.Id, m.Type, m.Quantity, m.Note, m.CreatedUtc)).ToListAsync(ct);

    public async Task<long> RecordAsync(StockMovement movement, Action<Product, long> validate, CancellationToken ct)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            // Acquire the update lock first: competing writers cannot both read the same balance.
            var product = await db.Products.FromSqlInterpolated($"SELECT * FROM dbo.Products WITH (UPDLOCK, HOLDLOCK) WHERE Id = {movement.ProductId}")
                .AsNoTracking().SingleOrDefaultAsync(ct) ?? throw new NotFoundException();
            var existing = await db.StockMovements.AsNoTracking().SingleOrDefaultAsync(m => m.RequestId == movement.RequestId, ct);
            var available = await db.StockMovements.Where(m => m.ProductId == product.Id)
                .SumAsync(m => m.Type == MovementType.In ? (long)m.Quantity : -(long)m.Quantity, ct);
            if (existing is not null)
            {
                if (existing.ProductId != movement.ProductId || existing.Type != movement.Type || existing.Quantity != movement.Quantity || existing.Note != movement.Note)
                    throw new BusinessRuleException("This submission was already used. Reload the page and try again.");
                await transaction.CommitAsync(ct);
                return available;
            }
            validate(product, available);
            db.StockMovements.Add(movement);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            logger.LogInformation("Recorded {Type} of {Quantity} for product {ProductId}, request {RequestId}", movement.Type, movement.Quantity, product.Id, movement.RequestId);
            return available + (movement.Type == MovementType.In ? (long)movement.Quantity : -(long)movement.Quantity);
        }
        catch (Exception ex) when (ex is SqlException { Number: 1205 or 1222 } || ex is DbUpdateException { InnerException: SqlException { Number: 1205 or 1222 or 2601 or 2627 } })
        {
            logger.LogWarning(ex, "Stock transaction conflict for product {ProductId}", movement.ProductId);
            throw new BusinessRuleException("Another stock transaction conflicted with this request. Reload and try again.");
        }
        // Disposal rolls back every uncommitted transaction, including validation failures and cancellation.
    }
}
