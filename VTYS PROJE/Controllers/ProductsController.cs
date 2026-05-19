using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VTYS_PROJE.Data;
using VTYS_PROJE.Filters;
using VTYS_PROJE.Infrastructure;
using VTYS_PROJE.Models;

namespace VTYS_PROJE.Controllers;

[AuthorizeAdmin]
public class ProductsController : Controller
{
    private readonly FoodOrderingContext _context;

    public ProductsController(FoodOrderingContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var products = await _context.products
            .Include(p => p.restaurant)
            .Include(p => p.category)
            .Where(p => p.is_active)
            .OrderBy(p => p.product_name)
            .ToListAsync();
        return View(products);
    }

    public async Task<IActionResult> Create()
    {
        await PopulateSelections();
        return View(new product());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(product model)
    {
        if (!ValidateProductModel(model))
        {
            await PopulateSelections();
            return View(model);
        }

        model.product_name = model.product_name.Trim();
        model.created_at = DateTime.Now;
        model.is_active = true;

        try
        {
            _context.products.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Urun eklendi.";
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Kayit sirasinda hata olustu.");
            await PopulateSelections();
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(long id)
    {
        var product = await _context.products.FindAsync(id);
        if (product is null || !product.is_active)
        {
            return NotFound();
        }

        await PopulateSelections();
        return View(product);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, product model)
    {
        if (id != model.product_id)
        {
            return BadRequest();
        }

        if (!ValidateProductModel(model))
        {
            await PopulateSelections();
            return View(model);
        }

        model.product_name = model.product_name.Trim();

        try
        {
            _context.products.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Urun guncellendi.";
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Guncelleme sirasinda hata olustu.");
            await PopulateSelections();
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(long id)
    {
        var product = await _context.products.FindAsync(id);
        if (product is null)
        {
            return NotFound();
        }

        product.is_active = false;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Urun pasife alindi.";
        return RedirectToAction(nameof(Index));
    }

    private bool ValidateProductModel(product model)
    {
        if (string.IsNullOrWhiteSpace(model.product_name) || model.unit_price <= 0 || model.stock_qty < 0)
        {
            ModelState.AddModelError(string.Empty, "Urun adi zorunlu, fiyat 0'dan buyuk, stok negatif olamaz.");
            return false;
        }

        InputValidation.ValidatePersonName(ModelState, nameof(model.product_name), model.product_name);
        return ModelState.IsValid;
    }

    private async Task PopulateSelections()
    {
        ViewBag.Restaurants = await _context.restaurants
            .Where(r => r.is_active)
            .OrderBy(r => r.restaurant_name)
            .Select(r => new SelectListItem(r.restaurant_name, r.restaurant_id.ToString()))
            .ToListAsync();

        ViewBag.Categories = await _context.categories
            .OrderBy(c => c.category_name)
            .Select(c => new SelectListItem(c.category_name, c.category_id.ToString()))
            .ToListAsync();
    }
}
