using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using shelf_project.Models;

namespace shelf_project.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Distributor> Distributors { get; set; }
        public DbSet<Manufacturer> Manufacturers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<DistributorSubscription> DistributorSubscriptions { get; set; }
        public DbSet<DistributorProduct> DistributorProducts { get; set; }
        public DbSet<QRCode> QRCodes { get; set; }
        public DbSet<Sale> Sales { get; set; }
        public DbSet<Settlement> Settlements { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<SampleOrder> SampleOrders { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure entity relationships and constraints
            builder.Entity<Distributor>()
                .HasOne(d => d.User)
                .WithMany(u => u.Distributors)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Manufacturer>()
                .HasOne(m => m.User)
                .WithMany(u => u.Manufacturers)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Product>()
                .HasOne(p => p.Manufacturer)
                .WithMany(m => m.Products)
                .HasForeignKey(p => p.ManufacturerId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Order>()
                .HasOne(o => o.User)
                .WithMany()
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Order>()
                .HasOne(o => o.Distributor)
                .WithMany()
                .HasForeignKey(o => o.DistributorId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<DistributorSubscription>()
                .HasOne(ds => ds.Distributor)
                .WithMany(d => d.Subscriptions)
                .HasForeignKey(ds => ds.DistributorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DistributorProduct>()
                .HasOne(dp => dp.Distributor)
                .WithMany(d => d.DistributorProducts)
                .HasForeignKey(dp => dp.DistributorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<DistributorProduct>()
                .HasOne(dp => dp.Product)
                .WithMany(p => p.DistributorProducts)
                .HasForeignKey(dp => dp.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<QRCode>()
                .HasOne(qr => qr.Distributor)
                .WithMany(d => d.QRCodes)
                .HasForeignKey(qr => qr.DistributorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Sale>()
                .HasOne(s => s.Order)
                .WithMany()
                .HasForeignKey(s => s.OrderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Sale>()
                .HasOne(s => s.Distributor)
                .WithMany(d => d.Sales)
                .HasForeignKey(s => s.DistributorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Sale>()
                .HasOne(s => s.QRCode)
                .WithMany(qr => qr.Sales)
                .HasForeignKey(s => s.QRCodeId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Settlement>()
                .HasOne(s => s.Manufacturer)
                .WithMany(m => m.Settlements)
                .HasForeignKey(s => s.ManufacturerId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Settlement>()
                .HasOne(s => s.Distributor)
                .WithMany()
                .HasForeignKey(s => s.DistributorId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SampleOrder>()
                .HasOne(so => so.Distributor)
                .WithMany()
                .HasForeignKey(so => so.DistributorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<SampleOrder>()
                .HasOne(so => so.Product)
                .WithMany(p => p.SampleOrders)
                .HasForeignKey(so => so.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure decimal precision
            builder.Entity<Product>()
                .Property(p => p.WholesalePrice)
                .HasPrecision(18, 2);

            builder.Entity<Product>()
                .Property(p => p.RetailPrice)
                .HasPrecision(18, 2);

            builder.Entity<Order>()
                .Property(o => o.TotalAmount)
                .HasPrecision(18, 2);

            // Add unique constraints
            builder.Entity<QRCode>()
                .HasIndex(qr => qr.Code)
                .IsUnique();

            builder.Entity<Order>()
                .HasIndex(o => o.OrderNumber)
                .IsUnique();
        }
    }
}