using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VTYS_PROJE.Data;
using VTYS_PROJE.Filters;
using VTYS_PROJE.Infrastructure;
using VTYS_PROJE.Models;
using VTYS_PROJE.Services;

namespace VTYS_PROJE.Controllers;

[AuthorizeAdmin]
public class CustomersController : Controller
{
    private readonly FoodOrderingContext _context;
    private readonly PasswordService _passwordService;

    public CustomersController(FoodOrderingContext context, PasswordService passwordService)
    {
        _context = context;
        _passwordService = passwordService;
    }

    public async Task<IActionResult> Index()
    {
        return View(await _context.customers.Where(c => c.is_active).ToListAsync());
    }

    public IActionResult Create()
    {
        return View(new customer());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(customer model)
    {
        if (!ValidateCustomerModel(model))
        {
            return View(model);
        }

        model.email = InputValidation.NormalizeEmail(model.email);
        model.phone = InputValidation.NormalizePhone(model.phone);
        model.full_name = model.full_name.Trim();
        model.password_hash = _passwordService.HashPassword(model.password_hash);
        model.created_at = DateTime.Now;
        model.is_active = true;

        try
        {
            _context.customers.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Musteri eklendi.";
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Kayit sirasinda hata olustu. E-posta veya telefon zaten kayitli olabilir.");
            return View(model);
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(long id)
    {
        var customer = await _context.customers.FindAsync(id);
        if (customer is null || !customer.is_active)
        {
            return NotFound();
        }

        return View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(long id, customer model)
    {
        if (id != model.customer_id)
        {
            return BadRequest();
        }

        if (!ValidateCustomerModel(model))
        {
            return View(model);
        }

        model.email = InputValidation.NormalizeEmail(model.email);
        model.phone = InputValidation.NormalizePhone(model.phone);
        model.full_name = model.full_name.Trim();

        if (!model.password_hash.StartsWith("PBKDF2:", StringComparison.Ordinal))
        {
            model.password_hash = _passwordService.HashPassword(model.password_hash);
        }

        try
        {
            _context.Update(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Musteri guncellendi.";
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
    public async Task<IActionResult> Deactivate(long id)
    {
        var customer = await _context.customers.FindAsync(id);
        if (customer is null)
        {
            return NotFound();
        }

        customer.is_active = false;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Musteri pasife alindi.";
        return RedirectToAction(nameof(Index));
    }

    private bool ValidateCustomerModel(customer model)
    {
        if (string.IsNullOrWhiteSpace(model.full_name) ||
            string.IsNullOrWhiteSpace(model.email) ||
            string.IsNullOrWhiteSpace(model.phone) ||
            string.IsNullOrWhiteSpace(model.password_hash))
        {
            ModelState.AddModelError(string.Empty, "Ad, e-posta, telefon ve sifre alani zorunludur.");
            return false;
        }

        InputValidation.ValidatePersonName(ModelState, nameof(model.full_name), model.full_name);
        InputValidation.ValidateEmail(ModelState, nameof(model.email), model.email);
        InputValidation.ValidatePhone(ModelState, nameof(model.phone), model.phone);

        return ModelState.IsValid;
    }
}
