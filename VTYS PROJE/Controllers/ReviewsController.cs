using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VTYS_PROJE.Data;
using VTYS_PROJE.Filters;
using VTYS_PROJE.Infrastructure;
using VTYS_PROJE.Models;
using VTYS_PROJE.Services;
using VTYS_PROJE.ViewModels;

namespace VTYS_PROJE.Controllers;

public class ReviewsController : Controller
{
    private readonly FoodOrderingContext _context;

    public ReviewsController(FoodOrderingContext context)
    {
        _context = context;
    }

    [AuthorizeAdmin]
    public async Task<IActionResult> Index(long? restaurantId)
    {
        if (!await ReviewTableHelper.TableExistsAsync(_context))
        {
            TempData["Error"] =
                "Puan/yorum tablosu yok. SSMS'te Database/Scripts/03_Puanlama_Yorum.sql dosyasini calistirin.";
            return View(new List<restaurant_review>());
        }

        var query = _context.restaurant_reviews
            .Include(r => r.customer)
            .Include(r => r.restaurant)
            .Where(r => r.is_active)
            .AsQueryable();

        if (restaurantId.HasValue && restaurantId.Value > 0)
        {
            query = query.Where(r => r.restaurant_id == restaurantId.Value);
        }

        var reviews = await query
            .OrderByDescending(r => r.created_at)
            .ToListAsync();

        ViewBag.Restaurants = await _context.restaurants
            .Where(r => r.is_active)
            .OrderBy(r => r.restaurant_name)
            .Select(r => new SelectListItem(r.restaurant_name, r.restaurant_id.ToString()))
            .ToListAsync();

        ViewBag.SelectedRestaurantId = restaurantId;
        return View(reviews);
    }

    [HttpGet]
    [AuthorizeCustomer]
    public async Task<IActionResult> Create(long restaurantId)
    {
        if (!await ReviewTableHelper.TableExistsAsync(_context))
        {
            TempData["Error"] =
                "Puan/yorum tablosu yok. SSMS'te Database/Scripts/03_Puanlama_Yorum.sql dosyasini calistirin.";
            return RedirectToAction("Index", "Menu", new { restaurantId });
        }

        var restaurant = await _context.restaurants
            .FirstOrDefaultAsync(r => r.restaurant_id == restaurantId && r.is_active);

        if (restaurant is null)
        {
            return NotFound();
        }

        var customerId = GetCustomerId();
        var existing = await _context.restaurant_reviews
            .FirstOrDefaultAsync(r => r.restaurant_id == restaurantId && r.customer_id == customerId);

        var vm = new RestaurantReviewCreateViewModel
        {
            RestaurantId = restaurantId,
            RestaurantName = restaurant.restaurant_name,
            Rating = existing?.rating ?? 5,
            Comment = existing?.comment ?? string.Empty
        };

        ViewBag.IsUpdate = existing is not null;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeCustomer]
    public async Task<IActionResult> Create(RestaurantReviewCreateViewModel model)
    {
        if (!await ReviewTableHelper.TableExistsAsync(_context))
        {
            TempData["Error"] =
                "Puan/yorum tablosu yok. SSMS'te Database/Scripts/03_Puanlama_Yorum.sql dosyasini calistirin.";
            return RedirectToAction("Index", "Menu", new { restaurantId = model.RestaurantId });
        }

        var restaurant = await _context.restaurants
            .FirstOrDefaultAsync(r => r.restaurant_id == model.RestaurantId && r.is_active);

        if (restaurant is null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            model.RestaurantName = restaurant.restaurant_name;
            return View(model);
        }

        var customerId = GetCustomerId();
        if (customerId <= 0)
        {
            return RedirectToAction("Login", "Account");
        }

        var comment = model.Comment.Trim();
        var existing = await _context.restaurant_reviews
            .FirstOrDefaultAsync(r => r.restaurant_id == model.RestaurantId && r.customer_id == customerId);

        if (existing is null)
        {
            _context.restaurant_reviews.Add(new restaurant_review
            {
                restaurant_id = model.RestaurantId,
                customer_id = customerId,
                rating = (byte)model.Rating,
                comment = comment,
                is_active = true,
                created_at = DateTime.Now
            });
        }
        else
        {
            existing.rating = (byte)model.Rating;
            existing.comment = comment;
            existing.is_active = true;
            existing.created_at = DateTime.Now;
        }

        await _context.SaveChangesAsync();
        await RestaurantRatingService.RecalculateAsync(_context, model.RestaurantId);

        TempData["Success"] = "Puan ve yorumunuz kaydedildi.";
        return RedirectToAction("Index", "Menu", new { restaurantId = model.RestaurantId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeAdmin]
    public async Task<IActionResult> Deactivate(long id)
    {
        var review = await _context.restaurant_reviews.FindAsync(id);
        if (review is null)
        {
            return NotFound();
        }

        review.is_active = false;
        await _context.SaveChangesAsync();
        await RestaurantRatingService.RecalculateAsync(_context, review.restaurant_id);

        TempData["Success"] = "Yorum pasife alindi.";
        return RedirectToAction(nameof(Index), new { restaurantId = review.restaurant_id });
    }

    private long GetCustomerId()
    {
        var raw = HttpContext.Session.GetString(AuthSessionKeys.CustomerId);
        return long.TryParse(raw, out var id) ? id : 0;
    }
}
