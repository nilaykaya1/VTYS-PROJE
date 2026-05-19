using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VTYS_PROJE.Data;
using VTYS_PROJE.Infrastructure;
using VTYS_PROJE.Models;

namespace VTYS_PROJE.Controllers;

public class MenuController : Controller
{
    private readonly FoodOrderingContext _context;

    public MenuController(FoodOrderingContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index(long? restaurantId)
    {
        ViewBag.IsYonetici = HttpContext.Session.GetString(AuthSessionKeys.Role) == AuthSessionKeys.RoleAdmin;

        if (!restaurantId.HasValue || restaurantId.Value <= 0)
        {
            var restaurants = await _context.restaurants
                .Where(r => r.is_active)
                .OrderByDescending(r => r.rating)
                .ToListAsync();

            return View("Restaurants", restaurants);
        }

        var restaurant = await _context.restaurants
            .FirstOrDefaultAsync(r => r.restaurant_id == restaurantId.Value && r.is_active);

        if (restaurant is null)
        {
            TempData["Error"] = "Restoran bulunamadi.";
            return RedirectToAction(nameof(Index));
        }

        var products = await _context.products
            .Include(p => p.category)
            .Where(p => p.restaurant_id == restaurantId.Value && p.is_active)
            .OrderBy(p => p.product_name)
            .ToListAsync();

        List<restaurant_review> reviews;
        if (await ReviewTableHelper.TableExistsAsync(_context))
        {
            reviews = await ReviewTableHelper.LoadReviewsAsync(_context, restaurantId.Value);
        }
        else
        {
            reviews = [];
            TempData["Error"] =
                "Puan/yorum tablosu (restaurant_reviews) veritabaninda yok. SSMS'te Database/Scripts/03_Puanlama_Yorum.sql dosyasini calistirin.";
        }

        var isCustomer = HttpContext.Session.GetString(AuthSessionKeys.Role) == AuthSessionKeys.RoleCustomer;
        long customerId = 0;
        if (isCustomer && long.TryParse(HttpContext.Session.GetString(AuthSessionKeys.CustomerId), out var cid))
        {
            customerId = cid;
        }

        ViewBag.RestaurantName = restaurant.restaurant_name;
        ViewBag.RestaurantId = restaurantId.Value;
        ViewBag.RestaurantRating = restaurant.rating;
        ViewBag.Reviews = reviews;
        ViewBag.ReviewCount = reviews.Count;
        ViewBag.IsCustomer = isCustomer;
        ViewBag.CustomerHasReview = customerId > 0 && reviews.Any(r => r.customer_id == customerId);
        return View(products);
    }
}
