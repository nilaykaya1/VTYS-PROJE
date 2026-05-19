using System.ComponentModel.DataAnnotations;

namespace VTYS_PROJE.ViewModels;

public class RestaurantReviewCreateViewModel
{
    public long RestaurantId { get; set; }

    public string RestaurantName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Puan seciniz.")]
    [Range(1, 5, ErrorMessage = "Puan 1 ile 5 arasinda olmalidir.")]
    [Display(Name = "Puan (1-5)")]
    public int Rating { get; set; } = 5;

    [Required(ErrorMessage = "Yorum yaziniz.")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Yorum en az 10, en fazla 1000 karakter olmalidir.")]
    [Display(Name = "Yorumunuz")]
    public string Comment { get; set; } = string.Empty;
}
