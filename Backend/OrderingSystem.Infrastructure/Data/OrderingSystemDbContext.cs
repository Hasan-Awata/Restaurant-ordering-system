using Microsoft.EntityFrameworkCore;
using OrderingSystem.Domain.Entities;

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

                entity.Property<uint>("Version")     // 1. Creates a shadow property in EF memory
                      .IsRowVersion()                // 2. Tells EF Core to use this for concurrency checks
                      .HasColumnName("xmin");        // 3. Maps it to the physical PostgreSQL system column

                entity.HasMany(t => t.Sessions)
                      .WithOne(ts => ts.Table)
                      .HasForeignKey(ts => ts.TableId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasQueryFilter(e => !e.IsDeleted);
            });

            // 3. TableSessions
            modelBuilder.Entity<TableSession>(entity =>
            {
                entity.HasKey(e => e.TableSessionId);

                entity.Property<uint>("Version")     // 1. Creates a shadow property in EF memory
                      .IsRowVersion()                // 2. Tells EF Core to use this for concurrency checks
                      .HasColumnName("xmin");        // 3. Maps it to the physical PostgreSQL system column

                entity.HasOne(e => e.Table)
                      .WithMany(t => t.Sessions)
                      .HasForeignKey(e => e.TableId)
                      .OnDelete(DeleteBehavior.Restrict);
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
                entity.Property(e => e.IsDeleted).IsRequired();
             
                entity.HasOne(e => e.Category)
                      .WithMany(c => c.MenuItems)
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.Restrict);
                
                entity.HasQueryFilter(e => !e.IsDeleted);
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

                entity.Property<uint>("Version")     // 1. Creates a shadow property in EF memory
                      .IsRowVersion()                // 2. Tells EF Core to use this for concurrency checks
                      .HasColumnName("xmin");        // 3. Maps it to the physical PostgreSQL system column

                entity.HasOne(e => e.Session)
                      .WithMany(s => s.Orders)
                      .HasForeignKey(e => e.TableSessionId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Device)
                      .WithMany(d => d.Orders)
                      .HasForeignKey(e => e.DeviceSessionId)
                      .OnDelete(DeleteBehavior.Restrict);
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
            });
        }
    }
}