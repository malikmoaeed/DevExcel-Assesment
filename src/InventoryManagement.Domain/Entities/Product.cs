using InventoryManagement.Domain.Exceptions;
namespace InventoryManagement.Domain.Entities;

public sealed class Product
{
    private Product() { }
    public int Id { get; private set; }
    public string Sku { get; private set; } = "";
    public string Name { get; private set; } = "";
    public string? Description { get; private set; }
    public int ReorderLevel { get; private set; }
    public bool IsActive { get; private set; } = true;
    public DateTime CreatedUtc { get; private set; } = DateTime.UtcNow;

    public Product(string sku, string name, string? description, int reorderLevel)
        => Update(sku, name, description, reorderLevel, true);

    public void Update(string sku, string name, string? description, int reorderLevel, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(sku) || sku.Trim().Length > 64)
            throw new BusinessRuleException("SKU is required and must be at most 64 characters.");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 200)
            throw new BusinessRuleException("Name is required and must be at most 200 characters.");
        if (description?.Length > 2000) throw new BusinessRuleException("Description must be at most 2000 characters.");
        if (reorderLevel < 0) throw new BusinessRuleException("Reorder level cannot be negative.");
        Sku = sku.Trim().ToUpperInvariant(); Name = name.Trim(); Description = description?.Trim();
        ReorderLevel = reorderLevel; IsActive = isActive;
    }
}
