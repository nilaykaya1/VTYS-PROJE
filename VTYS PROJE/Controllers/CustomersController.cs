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
        var customers = await _context.customers
            .Where(c => c.is_active == true)
            .OrderBy(c => c.full_name)
            .ToListAsync();

        return View(customers);
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

            TempData["Success"] = "Müşteri eklendi.";
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Kayıt sırasında hata oluştu. E-posta veya telefon zaten kayıtlı olabilir.");
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(long id)
    {
        var customer = await _context.customers.FindAsync(id);

        if (customer == null || customer.is_active != true)
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

        model.is_active = true;

        try
        {
            _context.customers.Update(model);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Müşteri güncellendi.";
            return RedirectToAction(nameof(Index));
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Güncelleme sırasında hata oluştu.");
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(long id)
    {
        var customer = await _context.customers.FindAsync(id);

        if (customer == null)
        {
            return NotFound();
        }

        customer.is_active = false;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Müşteri pasife alındı.";
        return RedirectToAction(nameof(Index));
    }

    private bool ValidateCustomerModel(customer model)
    {
        if (string.IsNullOrWhiteSpace(model.full_name) ||
            string.IsNullOrWhiteSpace(model.email) ||
            string.IsNullOrWhiteSpace(model.phone) ||
            string.IsNullOrWhiteSpace(model.password_hash))
        {
            ModelState.AddModelError(string.Empty, "Ad, e-posta, telefon ve şifre alanı zorunludur.");
            return false;
        }

        InputValidation.ValidatePersonName(ModelState, nameof(model.full_name), model.full_name);
        InputValidation.ValidateEmail(ModelState, nameof(model.email), model.email);
        InputValidation.ValidatePhone(ModelState, nameof(model.phone), model.phone);

        return ModelState.IsValid;
    }
}