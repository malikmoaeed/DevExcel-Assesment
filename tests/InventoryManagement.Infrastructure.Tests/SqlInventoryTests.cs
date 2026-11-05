using InventoryManagement.Application.DTOs;
using InventoryManagement.Application.Exceptions;
using InventoryManagement.Application.Services;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Exceptions;
using InventoryManagement.Infrastructure.Persistence;
using InventoryManagement.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
namespace InventoryManagement.Infrastructure.Tests;

// Real SQL Server only. Each test owns a uniquely named disposable database.
public sealed class SqlInventoryTests : IAsyncLifetime
{
    private readonly string connection = $"Server=(localdb)\\MSSQLLocalDB;Database=InventoryTests_{Guid.NewGuid():N};Trusted_Connection=True;TrustServerCertificate=True";
    private ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);
    private static InventoryRepository Repository(ApplicationDbContext db) => new(db, NullLogger<InventoryRepository>.Instance);
    private static StockService Stock(ApplicationDbContext db) => new(Repository(db), new ProductService(Repository(db)));
    public async Task InitializeAsync() { await using var db = Context(); await db.Database.MigrateAsync(); }
    public async Task DisposeAsync() { await using var db = Context(); await db.Database.EnsureDeletedAsync(); }
    private async Task<int> Create(int initial = 0)
    {
        await using var db = Context(); var id = await new ProductService(Repository(db)).CreateAsync(new("TEST", "Test product", null, 5));
        if (initial > 0) await Stock(db).RecordStockInAsync(id, initial, "Initial", Guid.NewGuid()); return id;
    }
    [Fact]
    public async Task ConcurrentIssuesNeverOversell()
    {
        var id = await Create(10); var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        async Task<bool> Issue() { await using var db = Context(); await db.Database.OpenConnectionAsync(); await gate.Task; try { await Stock(db).RecordStockOutAsync(id, 7, null, Guid.NewGuid()); return true; } catch (BusinessRuleException ex) { Assert.Contains("only 3 units", ex.Message); return false; } }
        var first = Issue(); var second = Issue(); gate.SetResult(); var results = await Task.WhenAll(first, second);
        Assert.Single(results, x => x); await using var verify = Context(); Assert.Equal(3, await Stock(verify).GetCurrentStockAsync(id)); Assert.Equal(2, await verify.StockMovements.CountAsync());
    }
    [Fact]
    public async Task RepeatedSubmissionIsIdempotent()
    {
        var id = await Create(); var key = Guid.NewGuid();
        await using (var db = Context()) Assert.Equal(4, await Stock(db).RecordStockInAsync(id, 4, "Delivery", key));
        await using (var db = Context()) Assert.Equal(4, await Stock(db).RecordStockInAsync(id, 4, "Delivery", key));
        await using var verify = Context(); Assert.Single(await verify.StockMovements.ToListAsync());
    }
    [Fact]
    public async Task UniqueSkuIsEnforcedByDatabase()
    {
        await Create(); await using var db = Context();
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => Repository(db).CreateAsync(new Product("test", "Duplicate", null, 0), default)); Assert.Contains("SKU already exists", ex.Message);
    }
    [Fact]
    public async Task EmptyStockSearchAndLowStockWork()
    {
        var id = await Create(); await using var db = Context(); var service = new ProductService(Repository(db));
        var product = await service.GetAsync(id); Assert.Equal(0, product.CurrentStock); Assert.True(product.IsLowStock);
        Assert.Single(await service.SearchAsync("test")); Assert.Empty(await service.SearchAsync("missing"));
    }
    [Fact]
    public async Task InvalidIssueRollsBackAndHistoryIsNewestFirst()
    {
        var id = await Create(10); await using (var db = Context()) await Assert.ThrowsAsync<BusinessRuleException>(() => Stock(db).RecordStockOutAsync(id, 11, null, Guid.NewGuid()));
        await using var verify = Context(); await Stock(verify).RecordStockOutAsync(id, 2, "Order", Guid.NewGuid());
        var history = await Stock(verify).GetStockMovementHistoryAsync(id); Assert.Equal(2, history.Count); Assert.Equal(MovementType.Out, history[0].Type); Assert.Equal(8, await Stock(verify).GetCurrentStockAsync(id));
    }
    [Fact]
    public async Task InactiveAndMissingProductsAreRejected()
    {
        var id = await Create(); await using var db = Context();
        await new ProductService(Repository(db)).UpdateAsync(id, new ProductInput("TEST", "Test", null, 0, false));
        await Assert.ThrowsAsync<BusinessRuleException>(() => Stock(db).RecordStockInAsync(id, 1, null, Guid.NewGuid()));
        await Assert.ThrowsAsync<NotFoundException>(() => Stock(db).RecordStockInAsync(999, 1, null, Guid.NewGuid()));
        Assert.Empty(await db.StockMovements.ToListAsync());
    }
    [Fact]
    public async Task HistoryCannotBeDeletedThroughContext()
    {
        await Create(2); await using var db = Context(); db.StockMovements.Remove(await db.StockMovements.SingleAsync());
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
    }
}
