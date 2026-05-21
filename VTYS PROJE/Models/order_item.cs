using System.ComponentModel.DataAnnotations.Schema;

namespace VTYS_PROJE.Models;

public partial class order_item
{
    public long order_item_id { get; set; }

    public long order_id { get; set; }

    public long product_id { get; set; }

    public int quantity { get; set; }

    public decimal unit_price { get; set; }

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public decimal? line_total { get; set; }

    public virtual order order { get; set; } = null!;

    public virtual product product { get; set; } = null!;
}