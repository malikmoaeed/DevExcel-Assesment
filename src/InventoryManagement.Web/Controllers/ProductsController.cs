using InventoryManagement.Application.Services;
using InventoryManagement.Domain.Exceptions;
using InventoryManagement.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace InventoryManagement.Web.Controllers;

public sealed class ProductsController : Controller
{
    private readonly ProductService _productService;
    private readonly StockService _stockService;

    public ProductsController(ProductService productService, StockService stockService)
    {
        _productService = productService;
        _stockService = stockService;
    }

    public async Task<IActionResult> Index(string? search, CancellationToken cancellationToken)
    {
        var products = await _productService.SearchAsync(search, cancellationToken);
        var model = new ProductListModel(products, search);

        return View(model);
    }

    public async Task<IActionResult> Details(int id, CancellationToken cancellationToken)
    {
        var movementForm = new MovementForm();
        var model = await BuildDetailsModelAsync(id, movementForm, cancellationToken);

        return View(model);
    }

    public IActionResult Create()
    {
        return View("Form", new ProductForm());
    }

    [HttpPost]
    public async Task<IActionResult> Create(ProductForm form, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View("Form", form);
        }

        try
        {
            var id = await _productService.CreateAsync(form.ToInput(), cancellationToken);
            TempData["Success"] = "Product created successfully.";

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessRuleException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View("Form", form);
        }
    }

    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetAsync(id, cancellationToken);
        var form = new ProductForm
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Description = product.Description,
            ReorderLevel = product.ReorderLevel,
            IsActive = product.IsActive
        };

        return View("Form", form);
    }

    [HttpPost]
    public async Task<IActionResult> Edit(int id, ProductForm form, CancellationToken cancellationToken)
    {
        if (id != form.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View("Form", form);
        }

        try
        {
            await _productService.UpdateAsync(id, form.ToInput(), cancellationToken);
            TempData["Success"] = "Product updated successfully.";

            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessRuleException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return View("Form", form);
        }
    }

    [HttpPost]
    public async Task<IActionResult> RecordMovement(
        int id,
        [Bind(Prefix = "Movement")] MovementForm movement,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            var model = await BuildDetailsModelAsync(id, movement, cancellationToken);
            return View("Details", model);
        }

        try
        {
            var balance = await _stockService.RecordAsync(
                id,
                movement.Type,
                movement.Quantity,
                movement.Note,
                movement.RequestId,
                cancellationToken);

            TempData["Success"] = $"Stock movement recorded. Current stock: {balance:N0} units.";
            return RedirectToAction(nameof(Details), new { id });
        }
        catch (BusinessRuleException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);

            // Keep the submitted values so the user can correct the movement.
            var model = await BuildDetailsModelAsync(id, movement, cancellationToken);
            return View("Details", model);
        }
    }

    private async Task<ProductDetailsModel> BuildDetailsModelAsync(
        int id,
        MovementForm movement,
        CancellationToken cancellationToken)
    {
        var product = await _productService.GetAsync(id, cancellationToken);
        var history = await _stockService.GetStockMovementHistoryAsync(id, cancellationToken);

        return new ProductDetailsModel
        {
            Product = product,
            History = history,
            Movement = movement
        };
    }
}
