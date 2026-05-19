using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using VTYS_PROJE.Data;
using VTYS_PROJE.Filters;
using VTYS_PROJE.Infrastructure;
using VTYS_PROJE.Models;
using VTYS_PROJE.ViewModels;

namespace VTYS_PROJE.Controllers;

public class OrdersController : Controller
{
    private const string CartSessionKey = "order_cart";
    private const string CheckoutSessionKey = "order_checkout_draft";

    private static readonly HashSet<string> AllowedStatuses =
    [
        "PENDING",
        "PREPARING",
        "ON_THE_WAY",
        "DELIVERED",
        "CANCELLED"
    ];

    private readonly FoodOrderingContext _context;

    public OrdersController(FoodOrderingContext context)
    {
        _context = context;
    }

    [HttpGet]
    [AuthorizeCustomer]
    public async Task<IActionResult> Create()
    {
        var vm = new OrderCreateViewModel { Quantity = 1 };
        await PopulateSelections(vm);
        LoadCart(vm);
        LoadCheckoutDraft(vm);
        var activeCustomerId = GetActiveCustomerIdFromSession();
        if (vm.CustomerId <= 0)
        {
            vm.CustomerId = activeCustomerId;
        }
        ApplyRestaurantFromCart(vm);
        ViewBag.LockedToSessionCustomer = activeCustomerId > 0;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeCustomer]
    public async Task<IActionResult> AddToCart(long customerId, long restaurantId, long? courierId, long productId, int quantity)
    {
        var activeCustomerId = GetActiveCustomerIdFromSession();
        if (activeCustomerId > 0)
        {
            customerId = activeCustomerId;
        }

        var product = await _context.products
            .Include(p => p.restaurant)
            .FirstOrDefaultAsync(p => p.product_id == productId && p.is_active);

        if (product is null)
        {
            TempData["Error"] = "Secilen urun bulunamadi.";
            return RedirectToAction(nameof(Create));
        }

        if (quantity < 1)
        {
            TempData["Error"] = "Adet en az 1 olmali.";
            return RedirectToAction(nameof(Create));
        }

        if (customerId <= 0 || restaurantId <= 0)
        {
            TempData["Error"] = "Musteri ve restoran secimi zorunludur.";
            return RedirectToAction(nameof(Create));
        }

        if (product.restaurant_id != restaurantId)
        {
            TempData["Error"] = "Urun secilen restorana ait degil.";
            return RedirectToAction(nameof(Create));
        }

        var cart = GetCart();
        var existing = cart.FirstOrDefault(c => c.ProductId == productId);

        var newQuantity = (existing?.Quantity ?? 0) + quantity;
        if (product.stock_qty < newQuantity)
        {
            TempData["Error"] = "Yeterli stok yok.";
            return RedirectToAction(nameof(Create));
        }

        if (existing is null)
        {
            cart.Add(new OrderCartItemViewModel
            {
                ProductId = product.product_id,
                ProductName = product.product_name,
                RestaurantId = product.restaurant_id,
                RestaurantName = product.restaurant.restaurant_name,
                UnitPrice = product.unit_price,
                Quantity = quantity
            });
        }
        else
        {
            existing.Quantity = newQuantity;
        }

        SaveCart(cart);
        SaveCheckoutDraft(new OrderCheckoutDraftViewModel
        {
            CustomerId = customerId,
            RestaurantId = restaurantId,
            CourierId = courierId
        });
        TempData["Success"] = "Urun sepete eklendi.";
        return RedirectToAction(nameof(Create));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeCustomer]
    public IActionResult RemoveFromCart(long productId)
    {
        var cart = GetCart();
        cart.RemoveAll(c => c.ProductId == productId);
        SaveCart(cart);
        TempData["Success"] = "Urun sepetten cikarildi.";
        return RedirectToAction(nameof(Create));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeCustomer]
    public async Task<IActionResult> ChangeCartQuantity(long productId, int delta)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(c => c.ProductId == productId);
        if (item is null)
        {
            TempData["Error"] = "Sepet urunu bulunamadi.";
            return RedirectToAction(nameof(Create));
        }

        if (delta > 0)
        {
            var product = await _context.products
                .FirstOrDefaultAsync(p => p.product_id == productId && p.is_active);
            if (product is null)
            {
                TempData["Error"] = "Urun pasif veya bulunamadi.";
                return RedirectToAction(nameof(Create));
            }

            if (item.Quantity + delta > product.stock_qty)
            {
                TempData["Error"] = "Stok limiti asiliyor.";
                return RedirectToAction(nameof(Create));
            }
        }

        item.Quantity += delta;
        if (item.Quantity <= 0)
        {
            cart.Remove(item);
        }

        SaveCart(cart);
        return RedirectToAction(nameof(Create));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeCustomer]
    public IActionResult ClearCart()
    {
        HttpContext.Session.Remove(CartSessionKey);
        HttpContext.Session.Remove(CheckoutSessionKey);
        TempData["Success"] = "Sepet temizlendi.";
        return RedirectToAction(nameof(Create));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeCustomer]
    public async Task<IActionResult> Create(OrderCreateViewModel model)
    {
        var activeCustomerId = GetActiveCustomerIdFromSession();
        if (activeCustomerId > 0)
        {
            model.CustomerId = activeCustomerId;
        }

        var cart = GetCart();
        if (cart.Count == 0)
        {
            TempData["Error"] = "Sepet bos. Once urun ekleyin.";
            return RedirectToAction(nameof(Create));
        }

        if (!ModelState.IsValid || model.CustomerId <= 0 || model.RestaurantId <= 0 || string.IsNullOrWhiteSpace(model.DeliveryAddress))
        {
            await PopulateSelections(model);
            model.CartItems = cart;
            model.CartSubtotal = cart.Sum(c => c.LineTotal);
            return View(model);
        }

        model.RestaurantId = cart.First().RestaurantId;

        var customerExists = await _context.customers
            .AnyAsync(c => c.customer_id == model.CustomerId && c.is_active);
        if (!customerExists)
        {
            ModelState.AddModelError(nameof(model.CustomerId), "Musteri bulunamadi.");
            await PopulateSelections(model);
            return View(model);
        }

        var restaurantExists = await _context.restaurants
            .AnyAsync(r => r.restaurant_id == model.RestaurantId && r.is_active);
        if (!restaurantExists)
        {
            ModelState.AddModelError(nameof(model.RestaurantId), "Restoran bulunamadi.");
            await PopulateSelections(model);
            return View(model);
        }

        if (cart.Any(c => c.RestaurantId != model.RestaurantId))
        {
            ModelState.AddModelError(nameof(model.RestaurantId), "Sepette secili restoran disinda urun var.");
            await PopulateSelections(model);
            model.CartItems = cart;
            model.CartSubtotal = cart.Sum(c => c.LineTotal);
            return View(model);
        }

        var productIds = cart.Select(c => c.ProductId).Distinct().ToList();
        var dbProducts = await _context.products
            .Where(p => productIds.Contains(p.product_id) && p.is_active)
            .ToDictionaryAsync(p => p.product_id);

        foreach (var item in cart)
        {
            if (!dbProducts.TryGetValue(item.ProductId, out var dbProduct) || dbProduct.stock_qty < item.Quantity)
            {
                ModelState.AddModelError(string.Empty, $"{item.ProductName} icin stok yetersiz veya urun pasif.");
                await PopulateSelections(model);
                model.CartItems = cart;
                model.CartSubtotal = cart.Sum(c => c.LineTotal);
                return View(model);
            }
        }

        var subtotal = cart.Sum(c => c.LineTotal);
        var askidaUsed = 0m;
        askida_pool? pool = null;
        long orderIdForConfirmation = 0;

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            if (model.UseAskidaBalance)
            {
                pool = await _context.askida_pools.OrderBy(p => p.pool_id).FirstOrDefaultAsync();
                if (pool is not null && pool.current_balance > 0)
                {
                    askidaUsed = Math.Min(pool.current_balance, subtotal);
                    pool.current_balance -= askidaUsed;
                    pool.total_used_balance += askidaUsed;
                    pool.updated_at = DateTime.Now;
                }
            }

            var order = new order
            {
                customer_id = model.CustomerId,
                restaurant_id = model.RestaurantId,
                courier_id = model.CourierId,
                delivery_address = model.DeliveryAddress,
                payment_method = "CARD",
                status = "PENDING",
                order_date = DateTime.Now,
                is_active = true,
                revenue_recognized = false,
                askida_used_amount = askidaUsed,
                total_amount = subtotal - askidaUsed
            };
            _context.orders.Add(order);
            await _context.SaveChangesAsync();
            orderIdForConfirmation = order.order_id;

            foreach (var item in cart)
            {
                var dbProduct = dbProducts[item.ProductId];
                _context.order_items.Add(new order_item
                {
                    order_id = order.order_id,
                    product_id = item.ProductId,
                    quantity = item.Quantity,
                    unit_price = dbProduct.unit_price
                });
                dbProduct.stock_qty -= item.Quantity;
            }

            if (askidaUsed > 0 && pool is not null)
            {
                _context.askida_redemptions.Add(new askida_redemption
                {
                    pool_id = pool.pool_id,
                    beneficiary_customer_id = model.CustomerId,
                    order_id = order.order_id,
                    amount_used = askidaUsed,
                    status = "APPROVED",
                    created_at = DateTime.Now
                });
            }

            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch
        {
            await tx.RollbackAsync();
            ModelState.AddModelError(string.Empty, "Siparis olusturulurken bir hata olustu.");
            await PopulateSelections(model);
            model.CartItems = cart;
            model.CartSubtotal = cart.Sum(c => c.LineTotal);
            return View(model);
        }

        HttpContext.Session.Remove(CartSessionKey);
        HttpContext.Session.Remove(CheckoutSessionKey);
        TempData["Success"] = "Siparisiniz alindi.";
        return RedirectToAction(nameof(Confirmation), new { id = orderIdForConfirmation });
    }

    [AuthorizeCustomerOrAdmin]
    public async Task<IActionResult> History(long? customerId)
    {
        var isAdmin = HttpContext.Session.GetString(AuthSessionKeys.Role) == AuthSessionKeys.RoleAdmin;
        var activeCustomerId = GetActiveCustomerIdFromSession();

        if (!isAdmin)
        {
            if (activeCustomerId > 0)
            {
                customerId = activeCustomerId;
            }
            else if (!customerId.HasValue || customerId.Value <= 0)
            {
                return RedirectToAction("Login", "Account");
            }
        }

        var query = _context.orders
            .Include(o => o.customer)
            .Include(o => o.restaurant)
            .Include(o => o.order_items)
            .ThenInclude(i => i.product)
            .Where(o => o.is_active)
            .AsQueryable();

        if (customerId.HasValue)
        {
            query = query.Where(o => o.customer_id == customerId.Value);
        }

        var orders = await query
            .OrderByDescending(o => o.order_date)
            .ToListAsync();

        ViewBag.SelectedCustomerId = customerId;
        ViewBag.CustomerOptions = await _context.customers
            .Where(c => c.is_active)
            .OrderBy(c => c.full_name)
            .Select(c => new SelectListItem(c.full_name, c.customer_id.ToString()))
            .ToListAsync();
        ViewBag.LockedToSessionCustomer = !isAdmin && activeCustomerId > 0;
        ViewBag.IsAdminView = isAdmin;

        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeCustomer]
    public async Task<IActionResult> Reorder(long id)
    {
        var order = await _context.orders
            .Include(o => o.restaurant)
            .Include(o => o.order_items)
            .ThenInclude(i => i.product)
            .FirstOrDefaultAsync(o => o.order_id == id);

        if (order is null)
        {
            TempData["Error"] = "Tekrar siparis verilecek kayit bulunamadi.";
            return RedirectToAction(nameof(History));
        }

        var activeCustomerId = GetActiveCustomerIdFromSession();
        if (activeCustomerId > 0 && order.customer_id != activeCustomerId)
        {
            return Forbid();
        }

        var cart = new List<OrderCartItemViewModel>();
        foreach (var item in order.order_items)
        {
            if (!item.product.is_active || item.product.stock_qty < item.quantity)
            {
                TempData["Error"] = $"{item.product.product_name} icin stok yetersiz veya urun pasif.";
                return RedirectToAction(nameof(History));
            }

            cart.Add(new OrderCartItemViewModel
            {
                ProductId = item.product_id,
                ProductName = item.product.product_name,
                RestaurantId = order.restaurant_id,
                RestaurantName = order.restaurant.restaurant_name,
                UnitPrice = item.unit_price,
                Quantity = item.quantity
            });
        }

        SaveCart(cart);
        SaveCheckoutDraft(new OrderCheckoutDraftViewModel
        {
            CustomerId = order.customer_id,
            RestaurantId = order.restaurant_id,
            CourierId = order.courier_id
        });

        TempData["Success"] = "Sepet onceki siparisinize gore hazirlandi.";
        return RedirectToAction(nameof(Create));
    }

    [HttpGet]
    [AuthorizeCustomer]
    public async Task<IActionResult> ProductsByRestaurant(long restaurantId)
    {
        var products = await _context.products
            .Where(p => p.restaurant_id == restaurantId && p.is_active)
            .OrderBy(p => p.product_name)
            .Select(p => new { id = p.product_id, name = p.product_name, stock = p.stock_qty })
            .ToListAsync();

        return Json(products);
    }

    [AuthorizeCustomerOrAdmin]
    public async Task<IActionResult> Details(long id)
    {
        var order = await _context.orders
            .Include(o => o.customer)
            .Include(o => o.restaurant)
            .Include(o => o.courier)
            .Include(o => o.order_items)
            .ThenInclude(i => i.product)
            .Include(o => o.askida_redemption)
            .FirstOrDefaultAsync(o => o.order_id == id && o.is_active);

        if (order is null)
        {
            return NotFound();
        }

        var isAdmin = HttpContext.Session.GetString(AuthSessionKeys.Role) == AuthSessionKeys.RoleAdmin;
        if (!isAdmin)
        {
            var activeCustomerId = GetActiveCustomerIdFromSession();
            if (activeCustomerId <= 0 || order.customer_id != activeCustomerId)
            {
                return Forbid();
            }
        }

        return View(order);
    }

    [AuthorizeCustomer]
    public async Task<IActionResult> Confirmation(long id)
    {
        var order = await _context.orders
            .Include(o => o.customer)
            .Include(o => o.restaurant)
            .Include(o => o.order_items)
            .ThenInclude(i => i.product)
            .FirstOrDefaultAsync(o => o.order_id == id);

        if (order is null)
        {
            return RedirectToAction(nameof(History));
        }

        var activeCustomerId = GetActiveCustomerIdFromSession();
        if (activeCustomerId > 0 && order.customer_id != activeCustomerId)
        {
            return Forbid();
        }

        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeAdmin]
    public async Task<IActionResult> UpdateStatus(long id, string status)
    {
        var order = await _context.orders.FindAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        var normalizedStatus = status.Trim().ToUpperInvariant();
        if (!AllowedStatuses.Contains(normalizedStatus))
        {
            TempData["Error"] = "Gecersiz siparis durumu.";
            return RedirectToAction(nameof(History));
        }

        if (order.status is "DELIVERED" or "CANCELLED")
        {
            TempData["Error"] = "Tamamlanmis veya iptal edilmis siparisin durumu degistirilemez.";
            return RedirectToAction(nameof(History));
        }

        if (!IsValidStatusTransition(order.status, normalizedStatus))
        {
            TempData["Error"] = $"Gecersiz durum gecisi: {order.status} -> {normalizedStatus}";
            return RedirectToAction(nameof(History));
        }

        order.status = normalizedStatus;
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(History));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeAdmin]
    public async Task<IActionResult> Deactivate(long id)
    {
        var order = await _context.orders.FindAsync(id);
        if (order is null)
        {
            return NotFound();
        }

        order.is_active = false;
        order.status = "CANCELLED";
        await _context.SaveChangesAsync();
        TempData["Success"] = "Siparis pasife alindi.";
        return RedirectToAction(nameof(History));
    }

    private async Task PopulateSelections(OrderCreateViewModel vm)
    {
        vm.Customers = await _context.customers
            .Where(c => c.is_active)
            .OrderBy(c => c.full_name)
            .Select(c => new SelectListItem(c.full_name, c.customer_id.ToString()))
            .ToListAsync();
        vm.Restaurants = await _context.restaurants
            .Where(r => r.is_active)
            .OrderBy(r => r.restaurant_name)
            .Select(r => new SelectListItem(r.restaurant_name, r.restaurant_id.ToString()))
            .ToListAsync();
        vm.Couriers = await _context.couriers
            .Where(c => c.is_active)
            .OrderBy(c => c.full_name)
            .Select(c => new SelectListItem(c.full_name, c.courier_id.ToString()))
            .ToListAsync();
        vm.Products = await _context.products
            .Where(p => p.is_active)
            .OrderBy(p => p.product_name)
            .Select(p => new SelectListItem(p.product_name, p.product_id.ToString()))
            .ToListAsync();
    }

    private List<OrderCartItemViewModel> GetCart()
    {
        var json = HttpContext.Session.GetString(CartSessionKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<OrderCartItemViewModel>();
        }

        return JsonSerializer.Deserialize<List<OrderCartItemViewModel>>(json) ?? new List<OrderCartItemViewModel>();
    }

    private void SaveCart(List<OrderCartItemViewModel> cart)
    {
        HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
    }

    private void LoadCart(OrderCreateViewModel vm)
    {
        vm.CartItems = GetCart();
        vm.CartSubtotal = vm.CartItems.Sum(c => c.LineTotal);
    }

    private void ApplyRestaurantFromCart(OrderCreateViewModel vm)
    {
        if (vm.CartItems.Count == 0)
        {
            return;
        }

        var restaurantId = vm.CartItems.First().RestaurantId;
        if (vm.CartItems.All(c => c.RestaurantId == restaurantId))
        {
            vm.RestaurantId = restaurantId;
        }
    }

    private void SaveCheckoutDraft(OrderCheckoutDraftViewModel draft)
    {
        HttpContext.Session.SetString(CheckoutSessionKey, JsonSerializer.Serialize(draft));
    }

    private void LoadCheckoutDraft(OrderCreateViewModel vm)
    {
        var json = HttpContext.Session.GetString(CheckoutSessionKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        var draft = JsonSerializer.Deserialize<OrderCheckoutDraftViewModel>(json);
        if (draft is null)
        {
            return;
        }

        vm.CustomerId = draft.CustomerId;
        vm.RestaurantId = draft.RestaurantId;
        vm.CourierId = draft.CourierId;

        var activeCustomerId = GetActiveCustomerIdFromSession();
        if (activeCustomerId > 0)
        {
            vm.CustomerId = activeCustomerId;
        }
    }

    private long GetActiveCustomerIdFromSession()
    {
        var raw = HttpContext.Session.GetString(AuthSessionKeys.CustomerId);
        return long.TryParse(raw, out var customerId) ? customerId : 0;
    }

    private static bool IsValidStatusTransition(string current, string next)
    {
        if (string.Equals(current, next, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return current switch
        {
            "PENDING" => next is "PREPARING" or "CANCELLED",
            "PREPARING" => next is "ON_THE_WAY" or "CANCELLED",
            "ON_THE_WAY" => next is "DELIVERED" or "CANCELLED",
            _ => false
        };
    }
}


