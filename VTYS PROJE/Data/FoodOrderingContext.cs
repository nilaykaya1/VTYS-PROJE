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

    // EKLENDİ
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

            entity.Property(e => e.is_beneficiary_verified).HasDefaultValue(false);

            entity.Property(e => e.password_hash)
                .HasMaxLength(255)
                .IsUnicode(false);

            entity.Property(e => e.phone)
                .HasMaxLength(20)
                .IsUnicode(false);
        });

        // EKLENDİ
        modelBuilder.Entity<customer_card>(entity =>
        {
            entity.HasKey(e => e.card_id);

            entity.ToTable("customer_cards");

            entity.Property(e => e.card_holder_name)
                .HasMaxLength(120)
                .IsUnicode(false);

            entity.Property(e => e.card_number)
                .HasMaxLength(16)
                .IsUnicode(false);

            entity.Property(e => e.cvv)
                .HasMaxLength(4)
                .IsUnicode(false);

            entity.Property(e => e.created_at)
                .HasDefaultValueSql("(getdate())");

            entity.HasOne(d => d.customer)
                .WithMany(p => p.customer_cards)
                .HasForeignKey(d => d.customer_id)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_customer_cards_customers");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}