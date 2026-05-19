using Microsoft.EntityFrameworkCore;
using VTYS_PROJE.Data;
using VTYS_PROJE.Models;

namespace VTYS_PROJE.Infrastructure;

public static class ReviewTableHelper
{
    public static async Task<bool> TableExistsAsync(FoodOrderingContext context)
    {
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                SELECT 1
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = 'restaurant_reviews'
                """;
            var result = await command.ExecuteScalarAsync();
            return result is not null;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public static async Task<List<restaurant_review>> LoadReviewsAsync(
        FoodOrderingContext context,
        long restaurantId)
    {
        if (!await TableExistsAsync(context))
        {
            return [];
        }

        return await context.restaurant_reviews
            .Include(r => r.customer)
            .Where(r => r.restaurant_id == restaurantId && r.is_active)
            .OrderByDescending(r => r.created_at)
            .ToListAsync();
    }
}
