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

        public DbSet<Company> Companies { get; set; }
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
        public DbSet<QRCodeProduct> QRCodeProducts { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configure entity relationships and constraints
            
            // Company relationships
            builder.Entity<Company>()
                .HasOne(c => c.OwnerUser)
                .WithMany()
                .HasForeignKey(c => c.OwnerUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Distributor relationships
            builder.Entity<Distributor>()
                .HasOne(d => d.User)
                .WithMany(u => u.Distributors)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Distributor>()
                .HasOne(d => d.Company)
                .WithMany(c => c.Distributors)
                .HasForeignKey(d => d.CompanyId)
                .OnDelete(DeleteBehavior.SetNull);

            // Configure Distributor parent-child relationship
            builder.Entity<Distributor>()
                .HasOne(d => d.ParentDistributor)
                .WithMany(d => d.ChildDistributors)
                .HasForeignKey(d => d.ParentDistributorId)
                .OnDelete(DeleteBehavior.Restrict);

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
                .OnDelete(DeleteBehavior.Restrict);

            // QRCode relationships (1拠点1QRコード設計)
            builder.Entity<QRCode>()
                .HasOne(qr => qr.Distributor)
                .WithOne(d => d.QRCode)
                .HasForeignKey<QRCode>(qr => qr.DistributorId)
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
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Settlement>()
                .HasOne(s => s.Distributor)
                .WithMany()
                .HasForeignKey(s => s.DistributorId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SampleOrder>()
                .HasOne(so => so.Distributor)
                .WithMany()
                .HasForeignKey(so => so.DistributorId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<SampleOrder>()
                .HasOne(so => so.Product)
                .WithMany(p => p.SampleOrders)
                .HasForeignKey(so => so.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<QRCodeProduct>()
                .HasOne(qcp => qcp.QRCode)
                .WithMany(qr => qr.QRCodeProducts)
                .HasForeignKey(qcp => qcp.QRCodeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<QRCodeProduct>()
                .HasOne(qcp => qcp.Product)
                .WithMany(p => p.QRCodeProducts)
                .HasForeignKey(qcp => qcp.ProductId)
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

            // SystemSetting unique constraint
            builder.Entity<SystemSetting>()
                .HasIndex(s => new { s.Category, s.Key })
                .IsUnique();
        }
    }
}