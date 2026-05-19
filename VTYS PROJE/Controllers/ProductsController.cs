using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VTYS_PROJE.Data;
using VTYS_PROJE.Filters;
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
        ModelState.Remove("restaurant");
        ModelState.Remove("category");

        ValidateProductModel(model);

        if (!ModelState.IsValid)
        {
            await PopulateSelections();
            return View(model);
        }

        model.product_name = model.product_name.Trim();
        model.description = model.description?.Trim();
        model.created_at = DateTime.Now;
        model.is_active = true;

        try
        {
            _context.products.Add(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ürün başarıyla eklendi.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Kayıt sırasında hata oluştu: " +
                (ex.InnerException != null ? ex.InnerException.Message : ex.Message));

            await PopulateSelections();
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(long id)
    {
        var product = await _context.products.FindAsync(id);

        if (product == null || !product.is_active)
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

        ModelState.Remove("restaurant");
        ModelState.Remove("category");

        ValidateProductModel(model);

        if (!ModelState.IsValid)
        {
            await PopulateSelections();
            return View(model);
        }

        model.product_name = model.product_name.Trim();
        model.description = model.description?.Trim();
        model.is_active = true;

        try
        {
            _context.products.Update(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Ürün başarıyla güncellendi.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", "Güncelleme sırasında hata oluştu: " +
                (ex.InnerException != null ? ex.InnerException.Message : ex.Message));

            await PopulateSelections();
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(long id)
    {
        var product = await _context.products.FindAsync(id);

        if (product == null)
        {
            return NotFound();
        }

        product.is_active = false;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Ürün pasife alındı.";
        return RedirectToAction(nameof(Index));
    }

    private void ValidateProductModel(product model)
    {
        if (model.restaurant_id <= 0)
        {
            ModelState.AddModelError(nameof(model.restaurant_id), "Restoran seçiniz.");
        }

        if (model.category_id <= 0)
        {
            ModelState.AddModelError(nameof(model.category_id), "Kategori seçiniz.");
        }

        if (string.IsNullOrWhiteSpace(model.product_name))
        {
            ModelState.AddModelError(nameof(model.product_name), "Ürün adı zorunludur.");
        }

        if (model.unit_price <= 0)
        {
            ModelState.AddModelError(nameof(model.unit_price), "Birim fiyat 0'dan büyük olmalıdır.");
        }

        if (model.stock_qty < 0)
        {
            ModelState.AddModelError(nameof(model.stock_qty), "Stok miktarı negatif olamaz.");
        }
    }

    private async Task PopulateSelections()
    {
        ViewBag.Restaurants = await _context.restaurants
            .Where(r => r.is_active)
            .OrderBy(r => r.restaurant_name)
            .Select(r => new SelectListItem
            {
                Text = r.restaurant_name,
                Value = r.restaurant_id.ToString()
            })
            .ToListAsync();

        ViewBag.Categories = await _context.categories
            .OrderBy(c => c.category_name)
            .Select(c => new SelectListItem
            {
                Text = c.category_name,
                Value = c.category_id.ToString()
            })
            .ToListAsync();
    }
}