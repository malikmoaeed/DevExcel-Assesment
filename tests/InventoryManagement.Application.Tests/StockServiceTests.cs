using InventoryManagement.Application.DTOs;
using InventoryManagement.Application.Exceptions;
using InventoryManagement.Application.Interfaces;
using InventoryManagement.Application.Services;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Exceptions;
namespace InventoryManagement.Application.Tests;
public class StockServiceTests
{
    private readonly FakeRepository repository = new();
    private StockService Service => new(repository, new ProductService(repository));
    [Fact] public async Task SuccessfulReceipt() => Assert.Equal(10, await Service.RecordStockInAsync(1, 10, null, Guid.NewGuid()));
    [Fact] public async Task SuccessfulIssueToZero() { await Service.RecordStockInAsync(1, 10, null, Guid.NewGuid()); Assert.Equal(0, await Service.RecordStockOutAsync(1, 10, null, Guid.NewGuid())); }
    [Fact] public async Task InsufficientStockDoesNotPersist() {
        await Service.RecordStockInAsync(1, 10, null, Guid.NewGuid());
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => Service.RecordStockOutAsync(1, 15, null, Guid.NewGuid()));
        Assert.Contains("only 10 units", ex.Message); Assert.Single(repository.Movements); Assert.Equal(10, await Service.GetCurrentStockAsync(1));
    }
    [Theory] [InlineData(0)] [InlineData(-5)] public async Task InvalidQuantityNeverReachesRepository(int quantity) { await Assert.ThrowsAsync<BusinessRuleException>(() => Service.RecordStockInAsync(1, quantity, null, Guid.NewGuid())); Assert.Empty(repository.Movements); }
    [Fact] public async Task MissingProductRejected() => await Assert.ThrowsAsync<NotFoundException>(() => Service.RecordStockInAsync(99, 1, null, Guid.NewGuid()));
    [Fact] public async Task InactiveProductRejected() { repository.Product.Update("ABC", "Test", null, 0, false); await Assert.ThrowsAsync<BusinessRuleException>(() => Service.RecordStockInAsync(1, 1, null, Guid.NewGuid())); Assert.Empty(repository.Movements); }
    [Fact] public async Task DuplicateSkuRejected() => await Assert.ThrowsAsync<BusinessRuleException>(() => new ProductService(repository).CreateAsync(new(" abc ", "Other", null, 0)));
    private sealed class FakeRepository : IInventoryRepository
    {
        public Product Product { get; } = new("ABC", "Test", null, 0);
        public List<StockMovement> Movements { get; } = [];
        public Task<ProductDto?> GetAsync(int id, CancellationToken ct) => Task.FromResult(id == 1 ? new ProductDto(1, Product.Sku, Product.Name, null, 0, Product.IsActive, Product.CreatedUtc, StockRules.Calculate(Movements)) : null);
        public Task<bool> SkuExistsAsync(string sku, int? excludingId, CancellationToken ct) => Task.FromResult(sku == "ABC" && excludingId != 1);
        public Task<long> RecordAsync(StockMovement movement, Action<Product,long> validate, CancellationToken ct) {
            if (movement.ProductId != 1) throw new NotFoundException();
            validate(Product, StockRules.Calculate(Movements)); Movements.Add(movement); return Task.FromResult(StockRules.Calculate(Movements));
        }
        public Task<int> CreateAsync(Product p, CancellationToken ct) => throw new NotSupportedException();
        public Task UpdateAsync(int id, ProductInput input, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<ProductDto>> SearchAsync(string? search, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<MovementDto>> HistoryAsync(int productId, CancellationToken ct) => throw new NotSupportedException();
    }
}
