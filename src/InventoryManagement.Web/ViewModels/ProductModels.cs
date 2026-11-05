using System.ComponentModel.DataAnnotations;
using InventoryManagement.Application.DTOs;
using InventoryManagement.Domain.Enums;
namespace InventoryManagement.Web.ViewModels;

public sealed class ProductForm
{
    public int Id { get; set; }
    [Required, StringLength(64), Display(Name = "SKU")] public string Sku { get; set; } = "";
    [Required, StringLength(200)] public string Name { get; set; } = "";
    [StringLength(2000)] public string? Description { get; set; }
    [Range(0, int.MaxValue), Display(Name = "Reorder level")] public int ReorderLevel { get; set; }
    [Display(Name = "Active product")] public bool IsActive { get; set; } = true;
    public ProductInput ToInput() => new(Sku, Name, Description, ReorderLevel, IsActive);
}
public sealed class MovementForm
{
    [EnumDataType(typeof(MovementType)), Display(Name = "Movement type")] public MovementType Type { get; set; } = MovementType.In;
    [Range(1, int.MaxValue, ErrorMessage = "Quantity must be greater than zero.")] public int Quantity { get; set; } = 1;
    [StringLength(1000)] public string? Note { get; set; }
    public Guid RequestId { get; set; } = Guid.NewGuid();
}
public sealed class ProductDetailsModel
{
    public required ProductDto Product { get; init; }
    public required IReadOnlyList<MovementDto> History { get; init; }
    public MovementForm Movement { get; init; } = new();
}
public sealed record ProductListModel(IReadOnlyList<ProductDto> Products, string? Search);
