using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Mail;
using System.Text.RegularExpressions;
using VTYS_PROJE.Data;
using VTYS_PROJE.Infrastructure;
using static VTYS_PROJE.Infrastructure.InputValidation;
using VTYS_PROJE.Models;
using VTYS_PROJE.Services;
using VTYS_PROJE.ViewModels;

namespace VTYS_PROJE.Controllers;

public class AccountController : Controller
{
    private static readonly Regex PhoneRegex = new(@"^\+?\d{10,15}$", RegexOptions.Compiled);

    private readonly FoodOrderingContext _context;
    private readonly PasswordService _passwordService;
    private readonly IConfiguration _configuration;

    public AccountController(
        FoodOrderingContext context,
        PasswordService passwordService,
        IConfiguration configuration)
    {
        _context = context;
        _passwordService = passwordService;
        _configuration = configuration;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.GetString(AuthSessionKeys.Role) == AuthSessionKeys.RoleCustomer)
        {
            return RedirectToAction("Index", "Home");
        }

        ApplyDeniedMessage();
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim().ToLowerInvariant();
        var customer = await _context.customers
            .FirstOrDefaultAsync(c => c.email == email && c.is_active == true);

        if (customer is null || !_passwordService.VerifyPassword(model.Password, customer.password_hash))
        {
            ModelState.AddModelError(string.Empty, "E-posta veya sifre hatali.");
            return View(model);
        }

        await UpgradePasswordHashIfNeeded(customer, model.Password);

        SignInCustomer(customer);
        TempData["Success"] = $"Hos geldiniz, {customer.full_name}.";

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.GetString(AuthSessionKeys.Role) == AuthSessionKeys.RoleCustomer)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(new RegisterCustomerViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterCustomerViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var email = model.Email.Trim().ToLowerInvariant();
        ValidatePersonName(ModelState, nameof(model.FullName), model.FullName);
        ValidatePhone(ModelState, nameof(model.Phone), model.Phone);
        ValidateEmail(ModelState, nameof(model.Email), model.Email);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _context.customers.AnyAsync(c => c.email == email))
        {
            ModelState.AddModelError(nameof(model.Email), "Bu e-posta adresi zaten kayitli.");
            return View(model);
        }

        if (await _context.customers.AnyAsync(c => c.phone == NormalizePhone(model.Phone)))
        {
            ModelState.AddModelError(nameof(model.Phone), "Bu telefon numarasi zaten kayitli.");
            return View(model);
        }

        var customer = new customer
        {
            full_name = model.FullName.Trim(),
            email = email,
            phone = NormalizePhone(model.Phone),
            password_hash = _passwordService.HashPassword(model.Password),
            is_beneficiary_verified = false,
            is_active = true,
            created_at = DateTime.Now
        };

        try
        {
            _context.customers.Add(customer);
            await _context.SaveChangesAsync();
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Kayit sirasinda hata olustu. Bilgileri kontrol edin.");
            return View(model);
        }

        SignInCustomer(customer);
        TempData["Success"] = "Kayit basarili. Hos geldiniz!";
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult AdminLogin(string? returnUrl = null)
    {
        if (HttpContext.Session.GetString(AuthSessionKeys.Role) == AuthSessionKeys.RoleAdmin)
        {
            return RedirectToAction("Index", "Customers");
        }

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminLogin(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var adminEmail = model.Email.Trim().ToLowerInvariant();
        var admin = await _context.admins
            .FirstOrDefaultAsync(a => a.email == adminEmail && a.is_active);

        if (admin is null || !_passwordService.VerifyPassword(model.Password, admin.password_hash))
        {
            ModelState.AddModelError(string.Empty, "E-posta veya sifre hatali.");
            return View(model);
        }

        await UpgradeAdminPasswordHashIfNeeded(admin, model.Password);

        SignInAdmin(admin);
        TempData["Success"] = $"Yonetici girisi basarili: {admin.full_name}.";

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Customers");
    }

    [HttpGet]
    public IActionResult AdminRegister()
    {
        if (HttpContext.Session.GetString(AuthSessionKeys.Role) == AuthSessionKeys.RoleAdmin)
        {
            return RedirectToAction("Index", "Customers");
        }

        return View(new RegisterAdminViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AdminRegister(RegisterAdminViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var inviteCode = _configuration["Registration:AdminInviteCode"] ?? "LezzetJet2026";
        if (!string.Equals(model.InviteCode.Trim(), inviteCode, StringComparison.Ordinal))
        {
            ModelState.AddModelError(nameof(model.InviteCode), "Yonetici davet kodu hatali.");
            return View(model);
        }

        var email = model.Email.Trim().ToLowerInvariant();
        ValidatePersonName(ModelState, nameof(model.FullName), model.FullName);
        ValidateEmail(ModelState, nameof(model.Email), model.Email);
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (await _context.admins.AnyAsync(a => a.email == email))
        {
            ModelState.AddModelError(nameof(model.Email), "Bu e-posta adresi zaten kayitli.");
            return View(model);
        }

        var admin = new admin
        {
            full_name = model.FullName.Trim(),
            email = email,
            password_hash = _passwordService.HashPassword(model.Password),
            role = "ADMIN",
            is_active = true,
            created_at = DateTime.Now
        };

        try
        {
            _context.admins.Add(admin);
            await _context.SaveChangesAsync();
        }
        catch
        {
            ModelState.AddModelError(string.Empty, "Kayit sirasinda hata olustu. Bilgileri kontrol edin.");
            return View(model);
        }

        SignInAdmin(admin);
        TempData["Success"] = "Yonetici kaydi tamamlandi. Hos geldiniz!";
        return RedirectToAction("Index", "Customers");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        ClearAuthSession();
        TempData["Success"] = "Cikis yapildi.";
        return RedirectToAction("Index", "Home");
    }

    private void SignInCustomer(customer customer)
    {
        ClearAuthSession();
        HttpContext.Session.SetString(AuthSessionKeys.Role, AuthSessionKeys.RoleCustomer);
        HttpContext.Session.SetString(AuthSessionKeys.CustomerId, customer.customer_id.ToString());
        HttpContext.Session.SetString(AuthSessionKeys.CustomerName, customer.full_name);
    }

    private void SignInAdmin(admin admin)
    {
        ClearAuthSession();
        HttpContext.Session.SetString(AuthSessionKeys.Role, AuthSessionKeys.RoleAdmin);
        HttpContext.Session.SetString(AuthSessionKeys.AdminId, admin.admin_id.ToString());
        HttpContext.Session.SetString(AuthSessionKeys.AdminName, admin.full_name);
    }

    private async Task UpgradePasswordHashIfNeeded(customer customer, string plainPassword)
    {
        if (!_passwordService.NeedsRehash(customer.password_hash))
        {
            return;
        }

        customer.password_hash = _passwordService.HashPassword(plainPassword);
        await _context.SaveChangesAsync();
    }

    private async Task UpgradeAdminPasswordHashIfNeeded(admin admin, string plainPassword)
    {
        if (!_passwordService.NeedsRehash(admin.password_hash))
        {
            return;
        }

        admin.password_hash = _passwordService.HashPassword(plainPassword);
        await _context.SaveChangesAsync();
    }

    private void ClearAuthSession()
    {
        HttpContext.Session.Remove(AuthSessionKeys.Role);
        HttpContext.Session.Remove(AuthSessionKeys.CustomerId);
        HttpContext.Session.Remove(AuthSessionKeys.CustomerName);
        HttpContext.Session.Remove(AuthSessionKeys.AdminId);
        HttpContext.Session.Remove(AuthSessionKeys.AdminName);
    }

    private void ApplyDeniedMessage()
    {
        var denied = HttpContext.Session.GetString("auth_denied_message");
        if (!string.IsNullOrWhiteSpace(denied))
        {
            TempData["Error"] = denied;
            HttpContext.Session.Remove("auth_denied_message");
        }
    }
}
