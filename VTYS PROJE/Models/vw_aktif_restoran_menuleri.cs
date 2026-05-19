using System;
using System.Collections.Generic;

namespace VTYS_PROJE.Models;

public partial class vw_aktif_restoran_menuleri
{
    public long restaurant_id { get; set; }

    public string restaurant_name { get; set; } = null!;

    public string cuisine_type { get; set; } = null!;

    public long product_id { get; set; }

    public string product_name { get; set; } = null!;

    public string category_name { get; set; } = null!;

    public decimal unit_price { get; set; }

    public int stock_qty { get; set; }
}
