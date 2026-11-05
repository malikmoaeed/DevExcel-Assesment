using InventoryManagement.Application.Services;
using InventoryManagement.Domain.Exceptions;
using InventoryManagement.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
namespace InventoryManagement.Web.Controllers;

public sealed class ProductsController(ProductService products, StockService stock) : Controller
{
    public async Task<IActionResult> Index(string? search, CancellationToken ct) => View(new ProductListModel(await products.SearchAsync(search, ct), search));
    public async Task<IActionResult> Details(int id, CancellationToken ct) => View(await DetailsModel(id, new(), ct));
    public IActionResult Create() => View("Form", new ProductForm());
    [HttpPost]
    public async Task<IActionResult> Create(ProductForm form, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try { var id = await products.CreateAsync(form.ToInput(), ct); TempData["Success"] = "Product created successfully."; return RedirectToAction(nameof(Details), new { id }); }
            catch (BusinessRuleException ex) { ModelState.AddModelError("", ex.Message); }
        }
        return View("Form", form);
    }
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var p = await products.GetAsync(id, ct);
        return View("Form", new ProductForm { Id = p.Id, Sku = p.Sku, Name = p.Name, Description = p.Description, ReorderLevel = p.ReorderLevel, IsActive = p.IsActive });
    }
    [HttpPost]
    public async Task<IActionResult> Edit(int id, ProductForm form, CancellationToken ct)
    {
        if (id != form.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            try { await products.UpdateAsync(id, form.ToInput(), ct); TempData["Success"] = "Product updated successfully."; return RedirectToAction(nameof(Details), new { id }); }
            catch (BusinessRuleException ex) { ModelState.AddModelError("", ex.Message); }
        }
        return View("Form", form);
    }
    [HttpPost]
    public async Task<IActionResult> RecordMovement(int id, [Bind(Prefix = "Movement")] MovementForm movement, CancellationToken ct)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var balance = await stock.RecordAsync(id, movement.Type, movement.Quantity, movement.Note, movement.RequestId, ct);
                TempData["Success"] = $"Stock movement recorded. Current stock: {balance:N0} units.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (BusinessRuleException ex) { ModelState.AddModelError("", ex.Message); }
        }
        return View("Details", await DetailsModel(id, movement, ct));
    }
    private async Task<ProductDetailsModel> DetailsModel(int id, MovementForm movement, CancellationToken ct) => new()
    { Product = await products.GetAsync(id, ct), History = await stock.GetStockMovementHistoryAsync(id, ct), Movement = movement };
}
