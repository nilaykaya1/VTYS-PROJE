using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VTYS_PROJE.Data;
using VTYS_PROJE.Filters;
using VTYS_PROJE.Infrastructure;
using VTYS_PROJE.Models;
using VTYS_PROJE.ViewModels;

namespace VTYS_PROJE.Controllers;

public class AskidaYemekController : Controller
{
    private readonly FoodOrderingContext _context;

    public AskidaYemekController(FoodOrderingContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AuthorizeCustomer]
    public async Task<IActionResult> Donate()
    {
        var vm = new AskidaYemekDonateViewModel();
        var customerId = GetActiveCustomerId();
        if (customerId > 0)
        {
            vm.CustomerId = customerId;
        }

        await PopulateCustomers(vm);
        ViewBag.PoolBalance = await GetPoolBalance();
        ViewBag.LockedToSessionCustomer = customerId > 0;
        return View(vm);
    }

    [HttpGet]
    [AuthorizeAdmin]
    public async Task<IActionResult> History()
    {
        var pool = await _context.askida_pools
            .OrderBy(p => p.pool_id)
            .FirstOrDefaultAsync();

        var donations = await _context.askida_donations
            .Include(d => d.donor_customer)
            .OrderByDescending(d => d.created_at)
            .Take(50)
            .ToListAsync();

        var redemptions = await _context.askida_redemptions
            .Include(r => r.beneficiary_customer)
            .Include(r => r.order)
            .OrderByDescending(r => r.created_at)
            .Take(50)
            .ToListAsync();

        var vm = new AskidaYemekHistoryViewModel
        {
            PoolBalance = pool?.current_balance ?? 0,
            TotalDonated = pool?.total_donated_balance ?? 0,
            TotalUsed = pool?.total_used_balance ?? 0,
            Donations = donations,
            Redemptions = redemptions
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeCustomer]
    public async Task<IActionResult> Donate(AskidaYemekDonateViewModel model)
    {
        var activeCustomerId = GetActiveCustomerId();
        if (activeCustomerId > 0)
        {
            model.CustomerId = activeCustomerId;
        }

        if (!ModelState.IsValid)
        {
            await PopulateCustomers(model);
            ViewBag.PoolBalance = await GetPoolBalance();
            return View(model);
        }

        var pool = await _context.askida_pools.OrderBy(p => p.pool_id).FirstOrDefaultAsync();
        if (pool is null)
        {
            pool = new askida_pool
            {
                current_balance = 0,
                total_donated_balance = 0,
                total_used_balance = 0,
                updated_at = DateTime.Now
            };
            _context.askida_pools.Add(pool);
            await _context.SaveChangesAsync();
        }

        _context.askida_donations.Add(new askida_donation
        {
            pool_id = pool.pool_id,
            donor_customer_id = model.CustomerId,
            donation_type = model.DonationType,
            amount = model.Amount,
            is_anonymous = model.IsAnonymous,
            note = model.Note,
            created_at = DateTime.Now
        });

        await _context.SaveChangesAsync();
        TempData["Success"] = "Bagisiniz alindi.";
        return RedirectToAction(nameof(Donate));
    }

    private async Task PopulateCustomers(AskidaYemekDonateViewModel vm)
    {
        vm.Customers = await _context.customers
            .Where(c => c.is_active)
            .OrderBy(c => c.full_name)
            .Select(c => new SelectListItem(c.full_name, c.customer_id.ToString()))
            .ToListAsync();
    }

    private async Task<decimal> GetPoolBalance()
    {
        return await _context.askida_pools
            .OrderBy(p => p.pool_id)
            .Select(p => p.current_balance)
            .FirstOrDefaultAsync();
    }

    private long GetActiveCustomerId()
    {
        var raw = HttpContext.Session.GetString(AuthSessionKeys.CustomerId);
        return long.TryParse(raw, out var id) ? id : 0;
    }
}
