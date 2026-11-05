using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Exceptions;
namespace InventoryManagement.Domain.Entities;

public sealed class StockMovement
{
    private StockMovement() { }
    public long Id { get; private set; }
    public int ProductId { get; private set; }
    public MovementType Type { get; private set; }
    public int Quantity { get; private set; }
    public string? Note { get; private set; }
    public DateTime CreatedUtc { get; private set; } = DateTime.UtcNow;
    public Guid RequestId { get; private set; }
    public StockMovement(int productId, MovementType type, int quantity, string? note, Guid requestId)
    {
        if (productId <= 0) throw new BusinessRuleException("Product must exist.");
        if (!Enum.IsDefined(type)) throw new BusinessRuleException("Select a valid movement type.");
        if (quantity <= 0) throw new BusinessRuleException("Quantity must be greater than zero.");
        if (note?.Length > 1000) throw new BusinessRuleException("Note must be at most 1000 characters.");
        if (requestId == Guid.Empty) throw new BusinessRuleException("Invalid submission identifier. Reload the page.");
        ProductId = productId; Type = type; Quantity = quantity; Note = note?.Trim(); RequestId = requestId;
    }
}
