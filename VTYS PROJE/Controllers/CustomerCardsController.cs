using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VTYS_PROJE.Data;
using VTYS_PROJE.Filters;
using VTYS_PROJE.Models;

namespace VTYS_PROJE.Controllers;

[AuthorizeAdmin]
public class CustomerCardsController : Controller
{
    private readonly FoodOrderingContext _context;

    public CustomerCardsController(FoodOrderingContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var cards = await _context.customer_cards
            .Include(c => c.customer)
            .OrderByDescending(c => c.created_at)
            .ToListAsync();

        return View(cards);
    }

    public IActionResult Create()
    {
        return View(new customer_card());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(customer_card model)
    {
        ModelState.Remove("customer");

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        model.created_at = DateTime.Now;

        _context.customer_cards.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Kart bilgisi eklendi.";
        return RedirectToAction(nameof(Index));
    }
}
