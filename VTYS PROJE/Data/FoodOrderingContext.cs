using Microsoft.EntityFrameworkCore;
using VTYS_PROJE.Models;

namespace VTYS_PROJE.Data;

public partial class FoodOrderingContext : DbContext
{
    public FoodOrderingContext()
    {
    }

    public FoodOrderingContext(DbContextOptions<FoodOrderingContext> options)
        : base(options)
    {
    }

    public virtual DbSet<admin> admins { get; set; }
    public virtual DbSet<askida_donation> askida_donations { get; set; }
    public virtual DbSet<askida_pool> askida_pools { get; set; }
    public virtual DbSet<askida_redemption> askida_redemptions { get; set; }
    public virtual DbSet<category> categories { get; set; }
    public virtual DbSet<courier> couriers { get; set; }
    public virtual DbSet<customer> customers { get; set; }
    public virtual DbSet<customer_card> customer_cards { get; set; }
    public virtual DbSet<order> orders { get; set; }
    public virtual DbSet<order_item> order_items { get; set; }
    public virtual DbSet<product> products { get; set; }
    public virtual DbSet<restaurant> restaurants { get; set; }
    public virtual DbSet<restaurant_review> restaurant_reviews { get; set; }
    public virtual DbSet<vw_aktif_restoran_menuleri> vw_aktif_restoran_menuleris { get; set; }
    public virtual DbSet<vw_askida_yemek_havuz_durumu> vw_askida_yemek_havuz_durumus { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<admin>(entity =>
        {
            entity.HasKey(e => e.admin_id);
            entity.ToTable("admins");
        });

        modelBuilder.Entity<customer>(entity =>
        {
            entity.HasKey(e => e.customer_id);
            entity.ToTable("customers");
        });

        modelBuilder.Entity<customer_card>(entity =>
        {
            entity.HasKey(e => e.card_id);
            entity.ToTable("customer_cards");

            entity.HasOne(d => d.customer)
                .WithMany(p => p.customer_cards)
                .HasForeignKey(d => d.customer_id);
        });

        modelBuilder.Entity<category>(entity =>
        {
            entity.HasKey(e => e.category_id);
            entity.ToTable("categories");
        });

        modelBuilder.Entity<restaurant>(entity =>
        {
            entity.HasKey(e => e.restaurant_id);
            entity.ToTable("restaurants", tb => tb.HasTrigger("tr_restaurants_dummy"));
        });

        modelBuilder.Entity<courier>(entity =>
        {
            entity.HasKey(e => e.courier_id);
            entity.ToTable("couriers");
        });

        modelBuilder.Entity<product>(entity =>
        {
            entity.HasKey(e => e.product_id);
            entity.ToTable("products", tb => tb.HasTrigger("tr_products_dummy"));

            entity.HasOne(d => d.category)
                .WithMany(p => p.products)
                .HasForeignKey(d => d.category_id);

            entity.HasOne(d => d.restaurant)
                .WithMany(p => p.products)
                .HasForeignKey(d => d.restaurant_id);
        });

        modelBuilder.Entity<order>(entity =>
        {
            entity.HasKey(e => e.order_id);
            entity.ToTable("orders", tb => tb.HasTrigger("tr_orders_dummy"));

            entity.HasOne(d => d.customer)
                .WithMany(p => p.orders)
                .HasForeignKey(d => d.customer_id);

            entity.HasOne(d => d.restaurant)
                .WithMany(p => p.orders)
                .HasForeignKey(d => d.restaurant_id);

            entity.HasOne(d => d.courier)
                .WithMany(p => p.orders)
                .HasForeignKey(d => d.courier_id);
        });

        modelBuilder.Entity<order_item>(entity =>
        {
            entity.HasKey(e => e.order_item_id);
            entity.ToTable("order_items", tb => tb.HasTrigger("tr_order_items_dummy"));

            entity.Property(e => e.unit_price)
                .HasColumnType("decimal(10,2)");

            entity.Property(e => e.line_total)
                .HasComputedColumnSql("[quantity] * [unit_price]", stored: false);

            entity.HasOne(d => d.order)
                .WithMany(p => p.order_items)
                .HasForeignKey(d => d.order_id);

            entity.HasOne(d => d.product)
                .WithMany(p => p.order_items)
                .HasForeignKey(d => d.product_id);
        });

        modelBuilder.Entity<restaurant_review>(entity =>
        {
            entity.HasKey(e => e.review_id);

            entity.ToTable("restaurant_reviews", tb =>
                tb.HasTrigger("tr_restaurant_reviews_dummy"));

            entity.Property(e => e.comment)
                .HasColumnType("text");

            entity.Property(e => e.created_at)
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.customer)
                .WithMany(p => p.restaurant_reviews)
                .HasForeignKey(d => d.customer_id);

            entity.HasOne(d => d.restaurant)
                .WithMany(p => p.restaurant_reviews)
                .HasForeignKey(d => d.restaurant_id);
        });

        modelBuilder.Entity<askida_pool>(entity =>
        {
            entity.HasKey(e => e.pool_id);
            entity.ToTable("askida_pool", tb => tb.HasTrigger("tr_askida_pool_dummy"));
        });

        modelBuilder.Entity<askida_donation>(entity =>
        {
            entity.HasKey(e => e.donation_id);
            entity.ToTable("askida_donations", tb => tb.HasTrigger("tr_askida_donations_dummy"));

            entity.HasOne(d => d.pool)
                .WithMany(p => p.askida_donations)
                .HasForeignKey(d => d.pool_id);

            entity.HasOne(d => d.donor_customer)
                .WithMany(p => p.askida_donations)
                .HasForeignKey(d => d.donor_customer_id);
        });

        modelBuilder.Entity<askida_redemption>(entity =>
        {
            entity.HasKey(e => e.redemption_id);
            entity.ToTable("askida_redemptions", tb => tb.HasTrigger("tr_askida_redemptions_dummy"));

            entity.HasOne(d => d.pool)
                .WithMany(p => p.askida_redemptions)
                .HasForeignKey(d => d.pool_id);

            entity.HasOne(d => d.beneficiary_customer)
                .WithMany(p => p.askida_redemptions)
                .HasForeignKey(d => d.beneficiary_customer_id);

            entity.HasOne(d => d.order)
                .WithOne(p => p.askida_redemption)
                .HasForeignKey<askida_redemption>(d => d.order_id);
        });

        modelBuilder.Entity<vw_aktif_restoran_menuleri>()
            .HasNoKey()
            .ToView("vw_aktif_restoran_menuleri");

        modelBuilder.Entity<vw_askida_yemek_havuz_durumu>()
            .HasNoKey()
            .ToView("vw_askida_yemek_havuz_durumu");

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}