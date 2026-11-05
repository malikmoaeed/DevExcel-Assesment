using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Exceptions;
namespace InventoryManagement.Domain.Common;

public static class StockRules
{
    public static long Calculate(IEnumerable<StockMovement> movements) => movements.Sum(m => m.Type == MovementType.In ? (long)m.Quantity : -(long)m.Quantity);
    public static bool IsLow(long stock, int reorderLevel) => stock <= reorderLevel;
    public static void EnsureAvailable(long available, int requested)
    {
        if (requested > available)
            throw new BusinessRuleException($"Insufficient stock. You requested {requested} units, but only {available} units are available.");
    }
}
