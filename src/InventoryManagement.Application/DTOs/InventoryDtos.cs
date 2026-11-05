using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Enums;
namespace InventoryManagement.Application.DTOs;

public sealed record ProductDto(int Id, string Sku, string Name, string? Description, int ReorderLevel, bool IsActive, DateTime CreatedUtc, long CurrentStock)
{
    public bool IsLowStock => StockRules.IsLow(CurrentStock, ReorderLevel);
}
public sealed record ProductInput(string Sku, string Name, string? Description, int ReorderLevel, bool IsActive = true);
public sealed record MovementDto(long Id, MovementType Type, int Quantity, string? Note, DateTime CreatedUtc);
