using Microsoft.EntityFrameworkCore;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;

namespace OrderingSystem.Infrastructure.Data
{
    public class OrderingSystemDbContext : DbContext
    {
        public OrderingSystemDbContext(DbContextOptions<OrderingSystemDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Table> Tables { get; set; }
        public DbSet<TableSession> TableSessions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<DeviceSession> SessionDevices { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Tax> Taxes { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<BillItem> BillItems { get; set; }
        public DbSet<BillTax> BillTaxes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 1. Users
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);
                entity.Property(e => e.FullName).IsRequired().HasMaxLength(255);
                entity.Property(e => e.PasswordHash).IsRequired();
                entity.Property(e => e.Role).IsRequired();
                entity.Property(e => e.RefreshToken)
                      .HasMaxLength(255)
                      .IsRequired(false);
                entity.Property(e => e.RefreshTokenExpiryTime)
                      .IsRequired(false);
                entity.Property(e => e.AbsoluteRefreshTokenExpiryTime)
                      .IsRequired(false);
            });

            // 2. Tables
            modelBuilder.Entity<Table>(entity =>
            {
                entity.HasKey(e => e.TableId);
                entity.Property(e => e.TableNumber).IsRequired();
                entity.Property(e => e.FloorNumber).IsRequired();
                entity.Property(e => e.Status).IsRequired();
                entity.Property(e => e.QrCode).IsRequired().HasMaxLength(255);
                entity.Property(e => e.IsDeleted).IsRequired();

                entity.Property(e => e.Version)
                      .IsRowVersion()
                      .HasColumnName("xmin");

                entity.HasMany(t => t.Sessions)
                      .WithOne(ts => ts.Table)
                      .HasForeignKey(ts => ts.TableId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(e => !e.IsDeleted);

                entity.HasIndex(t => new { t.TableNumber, t.FloorNumber })
                      .IsUnique()
                      .HasFilter("\"IsDeleted\" = false");
            });

            // 3. TableSessions
            modelBuilder.Entity<TableSession>(entity =>
            {
                entity.HasKey(e => e.TableSessionId);

                entity.HasOne(e => e.Table)
                      .WithMany(t => t.Sessions)
                      .HasForeignKey(e => e.TableId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasIndex(s => new { s.TableId, s.ClosedAt }, "IX_TableSessions_TableId")
                      .HasFilter(@"""ClosedAt"" IS NULL")
                      .IsUnique();
            });

            // 4. Categories
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.CategoryId);
                entity.Property(e => e.NameAr).HasMaxLength(255);
                entity.Property(e => e.NameEn).HasMaxLength(255);
                entity.Property(e => e.IsDeleted).IsRequired();

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // 5. MenuItems
            modelBuilder.Entity<MenuItem>(entity =>
            {
                entity.HasKey(e => e.MenuItemId);
                entity.Property(e => e.Price).HasPrecision(18, 2);
                entity.Property(e => e.NameAr).HasMaxLength(255);
                entity.Property(e => e.NameEn).HasMaxLength(255);

                entity.Property(e => e.Emoji).HasMaxLength(50).IsRequired(false);

                entity.Property(e => e.IsDeleted).IsRequired();

                entity.HasOne(e => e.Category)
                      .WithMany(c => c.MenuItems)
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(e => !e.IsDeleted);

                entity.HasIndex(m => new { m.CategoryId, m.IsAvailable, m.IsDeleted });
            });

            // 6. DeviceSession
            modelBuilder.Entity<DeviceSession>(entity =>
            {
                entity.HasKey(e => e.DeviceSessionId);

                entity.HasOne(e => e.TableSession)
                      .WithMany(s => s.Devices)
                      .HasForeignKey(e => e.TableSessionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 7. Orders
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.OrderId);
                entity.Property(e => e.TotalAmount).HasPrecision(18, 2);

                entity.HasOne(e => e.Session)
                      .WithMany(s => s.Orders)
                      .HasForeignKey(e => e.TableSessionId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Device)
                      .WithMany(d => d.Orders)
                      .HasForeignKey(e => e.DeviceSessionId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(e => e.OrderStatus != enOrderStatus.Cancelled);

                entity.HasIndex(o => new { o.OrderStatus, o.CreatedAt });

                entity.ToTable(t => t.HasCheckConstraint("CK_Order_TotalAmount_NonNegative", "\"TotalAmount\" >= 0"));
            });

            // 8. OrderItems
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.OrderItemId);
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
                entity.Property(e => e.Notes);

                entity.HasOne(e => e.Order)
                      .WithMany(o => o.OrderItems)
                      .HasForeignKey(e => e.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.MenuItem)
                      .WithMany(m => m.OrderItems)
                      .HasForeignKey(e => e.MenuItemId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.ToTable(t =>
                {
                    t.HasCheckConstraint("CK_OrderItem_Quantity_Positive", "\"Quantity\" > 0");
                    t.HasCheckConstraint("CK_OrderItem_UnitPrice_NonNegative", "\"UnitPrice\" >= 0");
                });
            });

            modelBuilder.Entity<Tax>(entity =>
            {
                entity.HasKey(e => e.TaxId);
                entity.Property(e => e.NameAr).HasMaxLength(255).IsRequired();
                entity.Property(e => e.NameEn).HasMaxLength(255).IsRequired();
                entity.Property(e => e.Amount).HasPrecision(18, 2).IsRequired();
                entity.Property(e => e.IsDeleted).IsRequired();
                entity.HasQueryFilter(e => !e.IsDeleted);
                entity.ToTable(t => t.HasCheckConstraint("CK_Tax_Amount_NonNegative", "\"Amount\" >= 0"));
            });

            modelBuilder.Entity<Bill>(entity => {
                entity.HasKey(e => e.BillId);
                entity.Property(e => e.TotalSubTotal).HasPrecision(18, 2);
                entity.Property(e => e.TotalTax).HasPrecision(18, 2);
                entity.Property(e => e.GrandTotal).HasPrecision(18, 2);
                entity.HasOne(e => e.TableSession).WithOne(ts => ts.FinalBill)
                      .HasForeignKey<Bill>(e => e.TableSessionId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<BillItem>(entity => {
                entity.HasKey(e => e.BillItemId);
                entity.Property(e => e.UnitPrice).HasPrecision(18, 2);
                entity.Property(e => e.TotalPrice).HasPrecision(18, 2);
                entity.HasOne(e => e.Bill).WithMany(b => b.BillItems)
                      .HasForeignKey(e => e.BillId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BillTax>(entity => {
                entity.HasKey(e => e.BillTaxId);
                entity.Property(e => e.AppliedAmount).HasPrecision(18, 2);
                entity.HasOne(e => e.Bill).WithMany(b => b.BillTaxes)
                      .HasForeignKey(e => e.BillId).OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}