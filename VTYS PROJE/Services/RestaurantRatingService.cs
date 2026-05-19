using Microsoft.EntityFrameworkCore;
using VTYS_PROJE.Data;

namespace VTYS_PROJE.Services;

public class RestaurantRatingService
{
    public static async Task RecalculateAsync(FoodOrderingContext context, long restaurantId)
    {
        var average = await context.restaurant_reviews
            .Where(r => r.restaurant_id == restaurantId && r.is_active)
            .Select(r => (decimal?)r.rating)
            .AverageAsync() ?? 0m;

        var restaurant = await context.restaurants.FindAsync(restaurantId);
        if (restaurant is null)
        {
            return;
        }

        restaurant.rating = Math.Round(average, 1);
        await context.SaveChangesAsync();
    }
}
