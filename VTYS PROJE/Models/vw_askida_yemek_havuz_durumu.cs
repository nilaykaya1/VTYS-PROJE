using System;
using System.Collections.Generic;

namespace VTYS_PROJE.Models;

public partial class vw_askida_yemek_havuz_durumu
{
    public long pool_id { get; set; }

    public decimal current_balance { get; set; }

    public decimal total_donated_balance { get; set; }

    public decimal total_used_balance { get; set; }

    public int? total_donation_count { get; set; }

    public int? total_redemption_count { get; set; }

    public DateTime updated_at { get; set; }
}
