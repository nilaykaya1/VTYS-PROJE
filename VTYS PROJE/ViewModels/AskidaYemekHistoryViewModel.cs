using VTYS_PROJE.Models;

namespace VTYS_PROJE.ViewModels;

public class AskidaYemekHistoryViewModel
{
    public decimal PoolBalance { get; set; }

    public decimal TotalDonated { get; set; }

    public decimal TotalUsed { get; set; }

    public List<askida_donation> Donations { get; set; } = new();

    public List<askida_redemption> Redemptions { get; set; } = new();
}
