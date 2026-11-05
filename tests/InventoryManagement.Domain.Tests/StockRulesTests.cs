using InventoryManagement.Domain.Common;
using InventoryManagement.Domain.Entities;
using InventoryManagement.Domain.Enums;
using InventoryManagement.Domain.Exceptions;
namespace InventoryManagement.Domain.Tests;
public class StockRulesTests
{
    private static StockMovement Movement(MovementType type, int quantity) => new(1, type, quantity, null, Guid.NewGuid());
    [Fact] public void EmptyHistoryIsZero() => Assert.Equal(0, StockRules.Calculate([]));
    [Fact] public void ReceiptsAreSummed() => Assert.Equal(30, StockRules.Calculate([Movement(MovementType.In, 10), Movement(MovementType.In, 20)]));
    [Fact] public void IssuesAreSubtracted() => Assert.Equal(7, StockRules.Calculate([Movement(MovementType.In, 10), Movement(MovementType.Out, 3)]));
    [Fact] public void TotalsDoNotOverflowInt() => Assert.Equal(2L * int.MaxValue, StockRules.Calculate([Movement(MovementType.In, int.MaxValue), Movement(MovementType.In, int.MaxValue)]));
    [Theory] [InlineData(0)] [InlineData(-1)] public void NonPositiveQuantityRejected(int quantity) => Assert.Throws<BusinessRuleException>(() => Movement(MovementType.In, quantity));
    [Fact] public void InvalidTypeRejected() => Assert.Throws<BusinessRuleException>(() => Movement((MovementType)9, 1));
    [Theory] [InlineData("", "Name", 0)] [InlineData("A", " ", 0)] [InlineData("A", "Name", -1)]
    public void InvalidProductRejected(string sku, string name, int reorder) => Assert.Throws<BusinessRuleException>(() => new Product(sku, name, null, reorder));
    [Fact] public void ProductNormalizesSkuAndUsesUtc() { var p = new Product(" abc ", " Name ", null, 0); Assert.Equal("ABC", p.Sku); Assert.Equal(DateTimeKind.Utc, p.CreatedUtc.Kind); Assert.True(p.IsActive); }
    [Theory] [InlineData(0, 0, true)] [InlineData(5, 5, true)] [InlineData(4, 5, true)] [InlineData(6, 5, false)]
    public void LowStockIncludesEquality(long stock, int reorder, bool expected) => Assert.Equal(expected, StockRules.IsLow(stock, reorder));
}
