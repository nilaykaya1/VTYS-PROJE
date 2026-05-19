namespace VTYS_PROJE.ViewModels;

public class OrderCartItemViewModel
{
    public long ProductId { get; set; }

    public string ProductName { get; set; } = string.Empty;

    public long RestaurantId { get; set; }

    public string RestaurantName { get; set; } = string.Empty;

    public decimal UnitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}
