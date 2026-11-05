using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
namespace InventoryManagement.Infrastructure.Persistence.Seed;
public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        // Serialize seeders even when two application instances start together.
        await db.Database.ExecuteSqlRawAsync("DECLARE @result int; EXEC @result = sp_getapplock @Resource = 'InventorySeed', @LockMode = 'Exclusive', @LockOwner = 'Transaction'; IF @result < 0 THROW 51000, 'Could not acquire seed lock.', 1;", ct);
        if (!await db.Products.AnyAsync(ct))
        {
            var samples = new[] {
                ("APL-IP15P", "iPhone 15 Pro", 5, 18, 8),
                ("SAM-S24", "Samsung Galaxy S24", 5, 12, 9),
                ("DEL-5440", "Dell Latitude 5440", 3, 10, 7),
                ("HP-840", "HP EliteBook 840", 2, 8, 2),
                ("LOG-M185", "Logitech Wireless Mouse", 10, 30, 6)
            };
            foreach (var (sku, name, reorder, received, issued) in samples)
            {
                var product = new Product(sku, name, "Sample inventory for the DevExcel assessment.", reorder);
                db.Products.Add(product); await db.SaveChangesAsync(ct);
                db.StockMovements.Add(new StockMovement(product.Id, MovementType.In, received, "Opening delivery", Guid.NewGuid()));
                db.StockMovements.Add(new StockMovement(product.Id, MovementType.Out, issued, "Customer order", Guid.NewGuid()));
            }
            await db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
    }
}
