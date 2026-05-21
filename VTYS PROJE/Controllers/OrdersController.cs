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
            vm.CustomerId = activeCustomerId;

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
            customerId = activeCustomerId;

        var product = await _context.products
            .Include(p => p.restaurant)
            .FirstOrDefaultAsync(p => p.product_id == productId && p.is_active == true);

        if (product == null)
        {
            TempData["Error"] = "Seçilen ürün bulunamadı.";
            return RedirectToAction(nameof(Create));
        }

        if (quantity < 1)
        {
            TempData["Error"] = "Adet en az 1 olmalıdır.";
            return RedirectToAction(nameof(Create));
        }

        if (customerId <= 0 || restaurantId <= 0)
        {
            TempData["Error"] = "Müşteri ve restoran seçimi zorunludur.";
            return RedirectToAction(nameof(Create));
        }

        if (product.restaurant_id != restaurantId)
        {
            TempData["Error"] = "Ürün seçilen restorana ait değil.";
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

        if (existing == null)
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

        TempData["Success"] = "Ürün sepete eklendi.";
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

        TempData["Success"] = "Ürün sepetten çıkarıldı.";
        return RedirectToAction(nameof(Create));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeCustomer]
    public async Task<IActionResult> ChangeCartQuantity(long productId, int delta)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(c => c.ProductId == productId);

        if (item == null)
        {
            TempData["Error"] = "Sepet ürünü bulunamadı.";
            return RedirectToAction(nameof(Create));
        }

        if (delta > 0)
        {
            var product = await _context.products
                .FirstOrDefaultAsync(p => p.product_id == productId && p.is_active == true);

            if (product == null)
            {
                TempData["Error"] = "Ürün pasif veya bulunamadı.";
                return RedirectToAction(nameof(Create));
            }

            if (item.Quantity + delta > product.stock_qty)
            {
                TempData["Error"] = "Stok limiti aşılıyor.";
                return RedirectToAction(nameof(Create));
            }
        }

        item.Quantity += delta;

        if (item.Quantity <= 0)
            cart.Remove(item);

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
            model.CustomerId = activeCustomerId;

        var cart = GetCart();

        if (cart.Count == 0)
        {
            TempData["Error"] = "Sepet boş. Önce ürün ekleyin.";
            return RedirectToAction(nameof(Create));
        }

        model.RestaurantId = cart.First().RestaurantId;

        if (model.CustomerId <= 0 || model.RestaurantId <= 0 || string.IsNullOrWhiteSpace(model.DeliveryAddress))
        {
            TempData["Error"] = "Müşteri, restoran ve teslimat adresi zorunludur.";
            await PopulateSelections(model);
            model.CartItems = cart;
            model.CartSubtotal = cart.Sum(c => c.LineTotal);
            return View(model);
        }

        var customer = await _context.customers
            .FirstOrDefaultAsync(c => c.customer_id == model.CustomerId && c.is_active == true);

        if (customer == null)
        {
            TempData["Error"] = "Müşteri bulunamadı.";
            return RedirectToAction(nameof(Create));
        }

        if (model.UseAskida && customer.is_beneficiary_verified != true)
        {
            TempData["Error"] = "Bu müşteri askıda yemek kullanımı için doğrulanmamış.";
            return RedirectToAction(nameof(Create));
        }

        var restaurantExists = await _context.restaurants
            .AnyAsync(r => r.restaurant_id == model.RestaurantId && r.is_active == true);

        if (!restaurantExists)
        {
            TempData["Error"] = "Restoran bulunamadı.";
            return RedirectToAction(nameof(Create));
        }

        if (cart.Any(c => c.RestaurantId != model.RestaurantId))
        {
            TempData["Error"] = "Sepette farklı restorana ait ürün var.";
            return RedirectToAction(nameof(Create));
        }

        var productIds = cart.Select(c => c.ProductId).Distinct().ToList();

        var dbProducts = await _context.products
            .Where(p => productIds.Contains(p.product_id) && p.is_active == true)
            .ToDictionaryAsync(p => p.product_id);

        foreach (var item in cart)
        {
            if (!dbProducts.TryGetValue(item.ProductId, out var product))
            {
                TempData["Error"] = $"{item.ProductName} ürünü bulunamadı veya pasif.";
                return RedirectToAction(nameof(Create));
            }

            if (product.stock_qty < item.Quantity)
            {
                TempData["Error"] = $"{item.ProductName} için yeterli stok yok.";
                return RedirectToAction(nameof(Create));
            }
        }

        var cartTotal = cart.Sum(c => c.LineTotal);

        if (model.UseAskida)
        {
            var poolCheck = await _context.askida_pools
                .OrderBy(p => p.pool_id)
                .FirstOrDefaultAsync();

            if (poolCheck == null || poolCheck.current_balance < cartTotal)
            {
                TempData["Error"] = "Askıda yemek bakiyesi bu sipariş için yetersiz.";
                return RedirectToAction(nameof(Create));
            }
        }

        var strategy = _context.Database.CreateExecutionStrategy();

        try
        {
            await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var order = new order
                    {
                        customer_id = model.CustomerId,
                        restaurant_id = model.RestaurantId,
                        courier_id = model.CourierId,
                        delivery_address = model.DeliveryAddress.Trim(),
                        order_date = DateTime.Now,
                        status = "PENDING",
                        payment_method = model.UseAskida ? "ASKIDA" : "CARD",
                        total_amount = cartTotal,
                        askida_used_amount = model.UseAskida ? cartTotal : 0m,
                        revenue_recognized = false,
                        is_active = true
                    };

                    _context.orders.Add(order);
                    await _context.SaveChangesAsync();

                    foreach (var item in cart)
                    {
                        var product = dbProducts[item.ProductId];

                        var orderItem = new order_item
                        {
                            order_id = order.order_id,
                            product_id = product.product_id,
                            quantity = item.Quantity,
                            unit_price = product.unit_price
                        };

                        _context.order_items.Add(orderItem);
                        product.stock_qty -= item.Quantity;
                    }

                    if (model.UseAskida)
                    {
                        var pool = await _context.askida_pools
                            .OrderBy(p => p.pool_id)
                            .FirstOrDefaultAsync();

                        if (pool == null || pool.current_balance < cartTotal)
                            throw new Exception("Askıda yemek bakiyesi yetersiz.");

                        pool.current_balance -= cartTotal;
                        pool.total_used_balance += cartTotal;
                        pool.updated_at = DateTime.Now;

                        var redemption = new askida_redemption
                        {
                            pool_id = pool.pool_id,
                            order_id = order.order_id,
                            beneficiary_customer_id = model.CustomerId,
                            amount_used = cartTotal,
                            status = "COMPLETED",
                            created_at = DateTime.Now
                        };

                        _context.askida_redemptions.Add(redemption);
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            });

            HttpContext.Session.Remove(CartSessionKey);
            HttpContext.Session.Remove(CheckoutSessionKey);

            TempData["Success"] = "Sipariş başarıyla oluşturuldu.";
            return RedirectToAction(nameof(History));
        }
        catch (Exception ex)
        {
            TempData["Error"] = ex.InnerException?.Message ?? ex.Message;
            return RedirectToAction(nameof(Create));
        }
    }

    [HttpGet]
    public async Task<IActionResult> History()
    {
        var isAdmin = HttpContext.Session.GetString(AuthSessionKeys.Role) == AuthSessionKeys.RoleAdmin;
        var activeCustomerId = GetActiveCustomerIdFromSession();

        if (!isAdmin && activeCustomerId <= 0)
            return RedirectToAction("Login", "Account");

        var query = _context.orders
            .Include(o => o.customer)
            .Include(o => o.restaurant)
            .Include(o => o.courier)
            .Include(o => o.order_items)
            .ThenInclude(i => i.product)
            .Where(o => o.is_active == true)
            .AsQueryable();

        if (!isAdmin)
            query = query.Where(o => o.customer_id == activeCustomerId);

        var orders = await query
            .OrderByDescending(o => o.order_date)
            .ToListAsync();

        ViewBag.IsAdminView = isAdmin;
        ViewBag.LockedToSessionCustomer = !isAdmin && activeCustomerId > 0;

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

        if (order == null)
        {
            TempData["Error"] = "Sipariş bulunamadı.";
            return RedirectToAction(nameof(History));
        }

        var activeCustomerId = GetActiveCustomerIdFromSession();

        if (activeCustomerId > 0 && order.customer_id != activeCustomerId)
            return Forbid();

        var cart = new List<OrderCartItemViewModel>();

        foreach (var item in order.order_items)
        {
            if (item.product.is_active != true || item.product.stock_qty < item.quantity)
            {
                TempData["Error"] = $"{item.product.product_name} için stok yetersiz veya ürün pasif.";
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

        TempData["Success"] = "Sepet önceki siparişinize göre hazırlandı.";
        return RedirectToAction(nameof(Create));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeAdmin]
    public async Task<IActionResult> Deactivate(long id)
    {
        var order = await _context.orders.FindAsync(id);

        if (order == null)
            return NotFound();

        order.is_active = false;
        order.status = "CANCELLED";

        await _context.SaveChangesAsync();

        TempData["Success"] = "Sipariş pasife alındı.";
        return RedirectToAction(nameof(History));
    }

    [HttpGet]
    [AuthorizeAdmin]
    public async Task<IActionResult> Index()
    {
        var orders = await _context.orders
            .Include(o => o.customer)
            .Include(o => o.restaurant)
            .Include(o => o.courier)
            .OrderByDescending(o => o.order_date)
            .ToListAsync();

        return View(orders);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeAdmin]
    public async Task<IActionResult> UpdateStatus(long id, string status)
    {
        status = status?.Trim().ToUpperInvariant() ?? "";

        if (!AllowedStatuses.Contains(status))
        {
            TempData["Error"] = "Geçersiz sipariş durumu.";
            return RedirectToAction(nameof(History));
        }

        var order = await _context.orders.FirstOrDefaultAsync(o => o.order_id == id);

        if (order == null)
        {
            TempData["Error"] = "Sipariş bulunamadı.";
            return RedirectToAction(nameof(History));
        }

        order.status = status;
        await _context.SaveChangesAsync();

        TempData["Success"] = "Sipariş durumu güncellendi.";
        return RedirectToAction(nameof(History));
    }

    [HttpGet]
    public async Task<IActionResult> Details(long id)
    {
        var isAdmin = HttpContext.Session.GetString(AuthSessionKeys.Role) == AuthSessionKeys.RoleAdmin;
        var activeCustomerId = GetActiveCustomerIdFromSession();

        var order = await _context.orders
            .Include(o => o.customer)
            .Include(o => o.restaurant)
            .Include(o => o.courier)
            .Include(o => o.order_items)
            .ThenInclude(i => i.product)
            .FirstOrDefaultAsync(o => o.order_id == id && o.is_active == true);

        if (order == null)
            return NotFound();

        if (!isAdmin && order.customer_id != activeCustomerId)
            return Forbid();

        return View(order);
    }

    private async Task PopulateSelections(OrderCreateViewModel vm)
    {
        vm.Customers = await _context.customers
            .Where(c => c.is_active == true)
            .OrderBy(c => c.full_name)
            .Select(c => new SelectListItem(c.full_name, c.customer_id.ToString()))
            .ToListAsync();

        vm.Restaurants = await _context.restaurants
            .Where(r => r.is_active == true)
            .OrderBy(r => r.restaurant_name)
            .Select(r => new SelectListItem(r.restaurant_name, r.restaurant_id.ToString()))
            .ToListAsync();

        vm.Couriers = await _context.couriers
            .Where(c => c.is_active == true)
            .OrderBy(c => c.full_name)
            .Select(c => new SelectListItem(c.full_name, c.courier_id.ToString()))
            .ToListAsync();

        vm.Products = await _context.products
            .Include(p => p.restaurant)
            .Where(p => p.is_active == true && p.stock_qty > 0)
            .OrderBy(p => p.restaurant.restaurant_name)
            .ThenBy(p => p.product_name)
            .Select(p => new SelectListItem(
                p.restaurant.restaurant_name + " - " + p.product_name + " - " + p.unit_price + " TL",
                p.product_id.ToString()))
            .ToListAsync();
    }

    private List<OrderCartItemViewModel> GetCart()
    {
        var raw = HttpContext.Session.GetString(CartSessionKey);

        if (string.IsNullOrWhiteSpace(raw))
            return new List<OrderCartItemViewModel>();

        return JsonSerializer.Deserialize<List<OrderCartItemViewModel>>(raw)
            ?? new List<OrderCartItemViewModel>();
    }

    private void SaveCart(List<OrderCartItemViewModel> cart)
    {
        HttpContext.Session.SetString(CartSessionKey, JsonSerializer.Serialize(cart));
    }

    private void LoadCart(OrderCreateViewModel vm)
    {
        var cart = GetCart();

        vm.CartItems = cart;
        vm.CartSubtotal = cart.Sum(c => c.LineTotal);
    }

    private void SaveCheckoutDraft(OrderCheckoutDraftViewModel draft)
    {
        HttpContext.Session.SetString(CheckoutSessionKey, JsonSerializer.Serialize(draft));
    }

    private void LoadCheckoutDraft(OrderCreateViewModel vm)
    {
        var raw = HttpContext.Session.GetString(CheckoutSessionKey);

        if (string.IsNullOrWhiteSpace(raw))
            return;

        var draft = JsonSerializer.Deserialize<OrderCheckoutDraftViewModel>(raw);

        if (draft == null)
            return;

        vm.CustomerId = draft.CustomerId;
        vm.RestaurantId = draft.RestaurantId;
        vm.CourierId = draft.CourierId;
    }

    private void ApplyRestaurantFromCart(OrderCreateViewModel vm)
    {
        var cart = GetCart();

        if (cart.Count > 0)
            vm.RestaurantId = cart.First().RestaurantId;
    }

    private long GetActiveCustomerIdFromSession()
    {
        var raw = HttpContext.Session.GetString(AuthSessionKeys.CustomerId);

        return long.TryParse(raw, out var id) ? id : 0;
    }
}