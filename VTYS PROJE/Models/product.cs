using System;
using System.Collections.Generic;

namespace VTYS_PROJE.Models;

public partial class product
{
    public long product_id { get; set; }

    public long restaurant_id { get; set; }

    public long category_id { get; set; }

    public string product_name { get; set; } = null!;

    public string? description { get; set; }

    public decimal unit_price { get; set; }

    public int stock_qty { get; set; }

    public bool is_active { get; set; }

    public DateTime created_at { get; set; }

    public virtual category category { get; set; } = null!;

    public virtual ICollection<order_item> order_items { get; set; } = new List<order_item>();

    public virtual restaurant restaurant { get; set; } = null!;
}
