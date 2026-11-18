using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using eCommerce.Domain.Entities;

namespace eCommerce.DL;

public partial class CartDbContext : DbContext
{
    public CartDbContext(DbContextOptions<CartDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Cart> Carts { get; set; }

    public virtual DbSet<CartItem> CartItems { get; set; }

    public virtual DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Cart>(entity =>
        {
            entity.HasIndex(e => e.ExpiresAt, "IX_Carts_ExpiresAt");

            entity.HasIndex(e => e.OwnerId, "IX_Carts_OwnerId");

            entity.Property(e => e.Id).HasDefaultValueSql("(newsequentialid())", "DF_Carts_Id");
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength()
                .HasDefaultValue("EUR", "DF_Carts_Currency");
            entity.Property(e => e.ExpiresAt).HasDefaultValueSql("(dateadd(day,(30),sysdatetimeoffset()))", "DF_Carts_ExpiresAt");
            entity.Property(e => e.OwnerId)
                .HasMaxLength(200)
                .UseCollation("Latin1_General_100_BIN2");
            entity.Property(e => e.Version).HasDefaultValue(1, "DF_Carts_Version");
        });

        modelBuilder.Entity<CartItem>(entity =>
        {
            entity.HasKey(e => new { e.CartId, e.ProductId });

            entity.HasIndex(e => e.ProductId, "IX_CartItems_ProductId");

            entity.Property(e => e.ProductId).HasMaxLength(100);

            entity.HasOne(d => d.Cart).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.CartId)
                .HasConstraintName("FK_CartItems_Carts");

            entity.HasOne(d => d.Product).WithMany(p => p.CartItems)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_CartItems_Products");
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.Property(e => e.Id).HasMaxLength(100);
            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.Name).HasMaxLength(200);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
