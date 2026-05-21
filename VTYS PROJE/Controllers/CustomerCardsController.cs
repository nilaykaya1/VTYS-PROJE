using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VTYS_PROJE.Data;
using VTYS_PROJE.Filters;
using VTYS_PROJE.Infrastructure;
using VTYS_PROJE.Models;

namespace VTYS_PROJE.Controllers;

public class CustomerCardsController : Controller
{
    private readonly FoodOrderingContext _context;

    public CustomerCardsController(FoodOrderingContext context)
    {
        _context = context;
    }

    [AuthorizeCustomer]
    public async Task<IActionResult> Index()
    {
        var customerIdText = HttpContext.Session.GetString(AuthSessionKeys.CustomerId);

        if (!long.TryParse(customerIdText, out var customerId))
        {
            return RedirectToAction("Login", "Account");
        }

        var cards = await _context.customer_cards
            .Where(c => c.customer_id == customerId)
            .OrderByDescending(c => c.created_at)
            .ToListAsync();

        return View(cards);
    }

    [AuthorizeCustomer]
    public IActionResult Create()
    {
        return View(new customer_card());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeCustomer]
    public async Task<IActionResult> Create(customer_card model)
    {
        var customerIdText = HttpContext.Session.GetString(AuthSessionKeys.CustomerId);

        if (!long.TryParse(customerIdText, out var customerId))
        {
            return RedirectToAction("Login", "Account");
        }

        if (string.IsNullOrWhiteSpace(model.card_holder_name) ||
            string.IsNullOrWhiteSpace(model.card_number) ||
            string.IsNullOrWhiteSpace(model.cvv) ||
            model.expiry_month < 1 ||
            model.expiry_month > 12 ||
            model.expiry_year < DateTime.Now.Year)
        {
            ModelState.AddModelError(string.Empty, "Kart bilgilerini eksiksiz ve doğru giriniz.");
            return View(model);
        }

        model.customer_id = customerId;
        model.card_number = model.card_number.Trim();
        model.card_holder_name = model.card_holder_name.Trim();
        model.cvv = model.cvv.Trim();
        model.created_at = DateTime.Now;

        _context.customer_cards.Add(model);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Kart bilgisi kaydedildi.";
        return RedirectToAction(nameof(Index));
    }

    [AuthorizeCustomer]
    public async Task<IActionResult> Edit(long id)
    {
        var customerIdText = HttpContext.Session.GetString(AuthSessionKeys.CustomerId);

        if (!long.TryParse(customerIdText, out var customerId))
        {
            return RedirectToAction("Login", "Account");
        }

        var card = await _context.customer_cards
            .FirstOrDefaultAsync(c => c.card_id == id && c.customer_id == customerId);

        if (card is null)
        {
            return NotFound();
        }

        return View(card);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeCustomer]
    public async Task<IActionResult> Edit(long id, customer_card model)
    {
        var customerIdText = HttpContext.Session.GetString(AuthSessionKeys.CustomerId);

        if (!long.TryParse(customerIdText, out var customerId))
        {
            return RedirectToAction("Login", "Account");
        }

        if (id != model.card_id)
        {
            return BadRequest();
        }

        var card = await _context.customer_cards
            .FirstOrDefaultAsync(c => c.card_id == id && c.customer_id == customerId);

        if (card is null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(model.card_holder_name) ||
            string.IsNullOrWhiteSpace(model.card_number) ||
            string.IsNullOrWhiteSpace(model.cvv) ||
            model.expiry_month < 1 ||
            model.expiry_month > 12 ||
            model.expiry_year < DateTime.Now.Year)
        {
            ModelState.AddModelError(string.Empty, "Kart bilgilerini eksiksiz ve doğru giriniz.");
            return View(model);
        }

        card.card_holder_name = model.card_holder_name.Trim();
        card.card_number = model.card_number.Trim();
        card.expiry_month = model.expiry_month;
        card.expiry_year = model.expiry_year;
        card.cvv = model.cvv.Trim();

        await _context.SaveChangesAsync();

        TempData["Success"] = "Kart bilgisi güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeCustomer]
    public async Task<IActionResult> Delete(long id)
    {
        var customerIdText = HttpContext.Session.GetString(AuthSessionKeys.CustomerId);

        if (!long.TryParse(customerIdText, out var customerId))
        {
            return RedirectToAction("Login", "Account");
        }

        var card = await _context.customer_cards
            .FirstOrDefaultAsync(c => c.card_id == id && c.customer_id == customerId);

        if (card is null)
        {
            return NotFound();
        }

        _context.customer_cards.Remove(card);
        await _context.SaveChangesAsync();

        TempData["Success"] = "Kart bilgisi silindi.";
        return RedirectToAction(nameof(Index));
    }
}