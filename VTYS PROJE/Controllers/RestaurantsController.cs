using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VTYS_PROJE.Data;
using VTYS_PROJE.Filters;
using VTYS_PROJE.Infrastructure;
using VTYS_PROJE.Models;

namespace VTYS_PROJE.Controllers;

public class RestaurantsController : Controller
{
    private readonly FoodOrderingContext _context;

    public RestaurantsController(FoodOrderingContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var restaurants = await _context.restaurants
            .Where(r => r.is_active)
            .OrderByDescending(r => r.rating)
            .ToListAsync();

        ViewBag.IsYonetici = HttpContext.Session.GetString(AuthSessionKeys.Role) == AuthSessionKeys.RoleAdmin;
        return View(restaurants);
    }

    [AuthorizeAdmin]
    public IActionResult Create()
    {
        return View(new restaurant());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeAdmin]
    public async Task<IActionResult> Create(restaurant model)
    {
        if (!ValidateRestaurantModel(model))
        {
            return View(model);
        }

        model.email = InputValidation.NormalizeEmail(model.email);
        model.phone = InputValidation.NormalizePhone(model.phone);
        model.restaurant_name = model.restaurant_name.Trim();
        model.cuisine_type = model.cuisine_type.Trim();
        model.created_at = DateTime.Now;
        model.is_active = true;

        try
        {
            _context.restaurants.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Restoran eklendi.";
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Kayit sirasinda hata olustu.");
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [AuthorizeAdmin]
    public async Task<IActionResult> Edit(long id)
    {
        var restaurant = await _context.restaurants.FindAsync(id);
        if (restaurant is null || !restaurant.is_active)
        {
            return NotFound();
        }

        return View(restaurant);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeAdmin]
    public async Task<IActionResult> Edit(long id, restaurant model)
    {
        if (id != model.restaurant_id)
        {
            return BadRequest();
        }

        if (!ValidateRestaurantModel(model))
        {
            return View(model);
        }

        model.email = InputValidation.NormalizeEmail(model.email);
        model.phone = InputValidation.NormalizePhone(model.phone);
        model.restaurant_name = model.restaurant_name.Trim();
        model.cuisine_type = model.cuisine_type.Trim();

        try
        {
            _context.restaurants.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Restoran guncellendi.";
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Guncelleme sirasinda hata olustu.");
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeAdmin]
    public async Task<IActionResult> Deactivate(long id)
    {
        var restaurant = await _context.restaurants.FindAsync(id);
        if (restaurant is null)
        {
            return NotFound();
        }

        restaurant.is_active = false;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Restoran pasife alindi.";
        return RedirectToAction(nameof(Index));
    }

    private bool ValidateRestaurantModel(restaurant model)
    {
        if (string.IsNullOrWhiteSpace(model.restaurant_name) ||
            string.IsNullOrWhiteSpace(model.email) ||
            string.IsNullOrWhiteSpace(model.phone) ||
            string.IsNullOrWhiteSpace(model.cuisine_type))
        {
            ModelState.AddModelError(string.Empty, "Restoran adi, e-posta, telefon ve mutfak turu zorunludur.");
            return false;
        }

        InputValidation.ValidatePersonName(ModelState, nameof(model.restaurant_name), model.restaurant_name);
        InputValidation.ValidatePersonName(ModelState, nameof(model.cuisine_type), model.cuisine_type);
        InputValidation.ValidateEmail(ModelState, nameof(model.email), model.email);
        InputValidation.ValidatePhone(ModelState, nameof(model.phone), model.phone);

        return ModelState.IsValid;
    }
}
