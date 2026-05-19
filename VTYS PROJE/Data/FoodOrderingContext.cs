using System;
using System.Collections.Generic;
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
            entity.HasKey(e => e.admin_id).HasName("PK_admins");

            entity.HasIndex(e => e.email, "UQ_admins_email").IsUnique();

            entity.Property(e => e.created_at).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.email)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.full_name)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.password_hash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.role)
                .HasMaxLength(30)
                .IsUnicode(false)
                .HasDefaultValue("ADMIN");
        });

        modelBuilder.Entity<askida_donation>(entity =>
        {
            entity.HasKey(e => e.donation_id).HasName("PK__askida_d__296B91DCF29266AA");

            entity.ToTable(tb => tb.HasTrigger("trg_add_donation_to_pool"));

            entity.HasIndex(e => new { e.donor_customer_id, e.created_at }, "idx_donations_donor_date").IsDescending(false, true);

            entity.Property(e => e.amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.created_at).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.donation_type)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.is_anonymous).HasDefaultValue(true);
            entity.Property(e => e.note)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.HasOne(d => d.donor_customer).WithMany(p => p.askida_donations)
                .HasForeignKey(d => d.donor_customer_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_donation_customer");

            entity.HasOne(d => d.pool).WithMany(p => p.askida_donations)
                .HasForeignKey(d => d.pool_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_donation_pool");
        });

        modelBuilder.Entity<askida_pool>(entity =>
        {
            entity.HasKey(e => e.pool_id).HasName("PK__askida_p__E3EB7509C08C16EB");

            entity.ToTable("askida_pool");

            entity.Property(e => e.current_balance).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.total_donated_balance).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.total_used_balance).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.updated_at).HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<askida_redemption>(entity =>
        {
            entity.HasKey(e => e.redemption_id).HasName("PK__askida_r__B17E433427ED9DDC");

            entity.ToTable(tb => tb.HasTrigger("trg_consume_askida_pool"));

            entity.HasIndex(e => e.order_id, "UQ__askida_r__4659622806C01507").IsUnique();

            entity.Property(e => e.amount_used).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.created_at).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("APPROVED");

            entity.HasOne(d => d.beneficiary_customer).WithMany(p => p.askida_redemptions)
                .HasForeignKey(d => d.beneficiary_customer_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_redemption_customer");

            entity.HasOne(d => d.order).WithOne(p => p.askida_redemption)
                .HasForeignKey<askida_redemption>(d => d.order_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_redemption_order");

            entity.HasOne(d => d.pool).WithMany(p => p.askida_redemptions)
                .HasForeignKey(d => d.pool_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_redemption_pool");
        });

        modelBuilder.Entity<category>(entity =>
        {
            entity.HasKey(e => e.category_id).HasName("PK__categori__D54EE9B43A3EE3DB");

            entity.HasIndex(e => e.category_name, "UQ__categori__5189E255767DEF83").IsUnique();

            entity.Property(e => e.category_name)
                .HasMaxLength(80)
                .IsUnicode(false);
        });

        modelBuilder.Entity<courier>(entity =>
        {
            entity.HasKey(e => e.courier_id).HasName("PK__couriers__37BB9A77DEEA6D95");

            entity.HasIndex(e => e.email, "UQ__couriers__AB6E61640D42629C").IsUnique();

            entity.HasIndex(e => e.phone, "UQ__couriers__B43B145F5CD0B9F7").IsUnique();

            entity.Property(e => e.created_at).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.email)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.full_name)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.vehicle_type)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<customer>(entity =>
        {
            entity.HasKey(e => e.customer_id).HasName("PK__customer__CD65CB855B80B662");

            entity.HasIndex(e => e.email, "UQ__customer__AB6E6164796C8258").IsUnique();

            entity.HasIndex(e => e.phone, "UQ__customer__B43B145F500CDA94").IsUnique();

            entity.Property(e => e.created_at).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.email)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.full_name)
                .HasMaxLength(120)
                .IsUnicode(false);
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.password_hash)
                .HasMaxLength(255)
                .IsUnicode(false);
            entity.Property(e => e.phone)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        modelBuilder.Entity<order>(entity =>
        {
            entity.HasKey(e => e.order_id).HasName("PK__orders__465962294594A25D");

            entity.HasIndex(e => new { e.customer_id, e.order_date }, "idx_orders_customer_date").IsDescending(false, true);

            entity.HasIndex(e => new { e.status, e.order_date }, "idx_orders_status_date").IsDescending(false, true);

            entity.Property(e => e.askida_used_amount).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.delivery_address).HasColumnType("text");
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.order_date).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.payment_method)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("CARD");
            entity.Property(e => e.status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("PENDING");
            entity.Property(e => e.total_amount).HasColumnType("decimal(12, 2)");

            entity.HasOne(d => d.courier).WithMany(p => p.orders)
                .HasForeignKey(d => d.courier_id)
                .HasConstraintName("fk_orders_courier");

            entity.HasOne(d => d.customer).WithMany(p => p.orders)
                .HasForeignKey(d => d.customer_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_orders_customer");

            entity.HasOne(d => d.restaurant).WithMany(p => p.orders)
                .HasForeignKey(d => d.restaurant_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_orders_restaurant");
        });

        modelBuilder.Entity<order_item>(entity =>
        {
            entity.HasKey(e => e.order_item_id).HasName("PK__order_it__3764B6BCE5578892");

            entity.ToTable(tb => tb.HasTrigger("trg_recalculate_order_total"));

            entity.HasIndex(e => new { e.order_id, e.product_id }, "uq_order_product").IsUnique();

            entity.Property(e => e.line_total)
                .HasComputedColumnSql("([quantity]*[unit_price])", true)
                .HasColumnType("decimal(21, 2)");
            entity.Property(e => e.unit_price).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.order).WithMany(p => p.order_items)
                .HasForeignKey(d => d.order_id)
                .HasConstraintName("fk_order_items_order");

            entity.HasOne(d => d.product).WithMany(p => p.order_items)
                .HasForeignKey(d => d.product_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_order_items_product");
        });

        modelBuilder.Entity<product>(entity =>
        {
            entity.HasKey(e => e.product_id).HasName("PK__products__47027DF5F6B1ACBE");

            entity.HasIndex(e => new { e.restaurant_id, e.is_active }, "idx_products_restaurant_active");

            entity.HasIndex(e => new { e.restaurant_id, e.product_name }, "uq_restaurant_product").IsUnique();

            entity.Property(e => e.created_at).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.description).IsUnicode(false);
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.product_name)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.unit_price).HasColumnType("decimal(10, 2)");

            entity.HasOne(d => d.category).WithMany(p => p.products)
                .HasForeignKey(d => d.category_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_product_category");

            entity.HasOne(d => d.restaurant).WithMany(p => p.products)
                .HasForeignKey(d => d.restaurant_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_product_restaurant");
        });

        modelBuilder.Entity<restaurant_review>(entity =>
        {
            entity.HasKey(e => e.review_id).HasName("PK_restaurant_reviews");

            entity.HasIndex(e => new { e.restaurant_id, e.customer_id }, "UQ_restaurant_reviews_customer_restaurant").IsUnique();

            entity.HasIndex(e => new { e.restaurant_id, e.is_active }, "IX_restaurant_reviews_restaurant");

            entity.Property(e => e.comment)
                .HasMaxLength(1000)
                .IsUnicode(false);
            entity.Property(e => e.created_at).HasDefaultValueSql("(sysdatetime())");
            entity.Property(e => e.is_active).HasDefaultValue(true);

            entity.HasOne(d => d.customer).WithMany(p => p.restaurant_reviews)
                .HasForeignKey(d => d.customer_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_restaurant_reviews_customer");

            entity.HasOne(d => d.restaurant).WithMany(p => p.restaurant_reviews)
                .HasForeignKey(d => d.restaurant_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_restaurant_reviews_restaurant");
        });

        modelBuilder.Entity<restaurant>(entity =>
        {
            entity.HasKey(e => e.restaurant_id).HasName("PK__restaura__3B0FAA9108A2AA0C");

            entity.HasIndex(e => e.email, "UQ__restaura__AB6E6164C4F84A50").IsUnique();

            entity.HasIndex(e => e.phone, "UQ__restaura__B43B145F013137EF").IsUnique();

            entity.Property(e => e.created_at).HasDefaultValueSql("(getdate())");
            entity.Property(e => e.cuisine_type)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.email)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.is_active).HasDefaultValue(true);
            entity.Property(e => e.phone)
                .HasMaxLength(20)
                .IsUnicode(false);
            entity.Property(e => e.rating)
                .HasDefaultValue(4.0m)
                .HasColumnType("decimal(2, 1)");
            entity.Property(e => e.restaurant_name)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.total_revenue).HasColumnType("decimal(12, 2)");
        });

        modelBuilder.Entity<vw_aktif_restoran_menuleri>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_aktif_restoran_menuleri");

            entity.Property(e => e.category_name)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.cuisine_type)
                .HasMaxLength(80)
                .IsUnicode(false);
            entity.Property(e => e.product_name)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.restaurant_name)
                .HasMaxLength(150)
                .IsUnicode(false);
            entity.Property(e => e.unit_price).HasColumnType("decimal(10, 2)");
        });

        modelBuilder.Entity<vw_askida_yemek_havuz_durumu>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("vw_askida_yemek_havuz_durumu");

            entity.Property(e => e.current_balance).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.total_donated_balance).HasColumnType("decimal(12, 2)");
            entity.Property(e => e.total_used_balance).HasColumnType("decimal(12, 2)");
        });

        modelBuilder.Entity<restaurant_review>()
    .ToTable(tb => tb.HasTrigger("trg_restaurant_reviews_update_rating"));

        modelBuilder.Entity<askida_redemption>()
            .ToTable(tb => tb.HasTrigger("trg_consume_askida_pool"));

        modelBuilder.Entity<order_item>()
            .ToTable(tb => tb.HasTrigger("trg_recalculate_order_total"));

        OnModelCreatingPartial(modelBuilder);
    }
    

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
