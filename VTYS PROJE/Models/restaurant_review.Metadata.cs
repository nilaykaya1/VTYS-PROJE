using System;
using System.ComponentModel.DataAnnotations;

namespace VTYS_PROJE.Models;

public class RestaurantReviewMetadata
{
    [Display(Name = "Puan (1-5)")]
    public byte rating { get; set; }

    [Display(Name = "Yorum")]
    public string comment { get; set; } = null!;

    [Display(Name = "Tarih")]
    public DateTime created_at { get; set; }
}

[MetadataType(typeof(RestaurantReviewMetadata))]
public partial class restaurant_review
{
}
