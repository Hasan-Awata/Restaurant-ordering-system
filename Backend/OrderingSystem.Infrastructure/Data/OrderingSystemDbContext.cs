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

        // DbSets representing your tables
        public DbSet<User> Users { get; set; }
        public DbSet<Table> Tables { get; set; }
        public DbSet<TableSession> TableSessions { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<MenuItem> MenuItems { get; set; }
        public DbSet<SessionDevice> SessionDevices { get; set; }
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
            });

            // 2. Tables
            modelBuilder.Entity<Table>(entity =>
            {
                entity.HasKey(e => e.TableId);
            });

            // 3. TableSessions
            modelBuilder.Entity<TableSession>(entity =>
            {
                entity.HasKey(e => e.SessionId);

                // Foreign Key to Table
                entity.HasOne(e => e.Table)
                      .WithMany(t => t.Sessions)
                      .HasForeignKey(e => e.TableId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Foreign Key to Waiter (User)
                entity.HasOne(e => e.Waiter)
                      .WithMany(u => u.TableSessions)
                      .HasForeignKey(e => e.WaiterId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 4. Categories
            modelBuilder.Entity<Category>(entity =>
            {
                entity.HasKey(e => e.CategoryId);
                entity.Property(e => e.NameAr).HasMaxLength(255);
                entity.Property(e => e.NameEn).HasMaxLength(255);
            });

            // 5. MenuItems
            modelBuilder.Entity<MenuItem>(entity =>
            {
                entity.HasKey(e => e.MenuItemId);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)"); // Prevents EF Core warnings
                entity.Property(e => e.NameAr).HasMaxLength(255);
                entity.Property(e => e.NameEn).HasMaxLength(255);

                entity.HasOne(e => e.Category)
                      .WithMany(c => c.MenuItems)
                      .HasForeignKey(e => e.CategoryId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 6. SessionDevices
            modelBuilder.Entity<SessionDevice>(entity =>
            {
                entity.HasKey(e => e.DeviceId);
                entity.Property(e => e.DeviceToken).HasMaxLength(500);

                entity.HasOne(e => e.Session)
                      .WithMany(s => s.Devices)
                      .HasForeignKey(e => e.SessionId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // 7. Orders
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.OrderId);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Session)
                      .WithMany(s => s.Orders)
                      .HasForeignKey(e => e.SessionId)
                      .OnDelete(DeleteBehavior.Restrict); // Prevent cascading cycles

                entity.HasOne(e => e.Device)
                      .WithMany(d => d.Orders)
                      .HasForeignKey(e => e.DeviceId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // 8. OrderItems
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.OrderItemId);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");

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
