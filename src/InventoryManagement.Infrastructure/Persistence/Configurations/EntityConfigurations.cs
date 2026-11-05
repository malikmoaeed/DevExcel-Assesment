using InventoryManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace InventoryManagement.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("Products", t =>
        {
            t.HasCheckConstraint("CK_Products_Reorder", "[ReorderLevel] >= 0");
            t.HasCheckConstraint("CK_Products_Required", "LEN(LTRIM(RTRIM([Sku]))) > 0 AND LEN(LTRIM(RTRIM([Name]))) > 0");
        });
        b.HasKey(p => p.Id);
        b.Property(p => p.Sku).HasMaxLength(64).UseCollation("Latin1_General_100_CI_AS").IsRequired();
        b.HasIndex(p => p.Sku).IsUnique();
        b.Property(p => p.Name).HasMaxLength(200).IsRequired();
        b.Property(p => p.Description).HasMaxLength(2000);
        b.Property(p => p.CreatedUtc).HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
    }
}
public sealed class MovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> b)
    {
        b.ToTable("StockMovements", t =>
        {
            t.HasCheckConstraint("CK_Movements_Quantity", "[Quantity] > 0");
            t.HasCheckConstraint("CK_Movements_Type", "[Type] IN (1, 2)");
        });
        b.HasKey(m => m.Id);
        b.Property(m => m.Note).HasMaxLength(1000);
        b.HasOne<Product>().WithMany().HasForeignKey(m => m.ProductId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(m => new { m.ProductId, m.CreatedUtc, m.Id });
        b.HasIndex(m => m.RequestId).IsUnique();
        b.Property(m => m.CreatedUtc).HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
    }
}
