// File: Data/VehicleAccountingContext.cs
using Microsoft.EntityFrameworkCore;
using VehicleAccountingAPI.Models; // Переконайтеся, що цей namespace правильний для ваших моделей

namespace VehicleAccountingAPI.Data
{
    public class VehicleAccountingContext : DbContext
    {
        public VehicleAccountingContext(DbContextOptions<VehicleAccountingContext> options)
            : base(options)
        {
        }

        // DbSet для всіх ваших сутностей
        public DbSet<VehicleType> VehicleTypes { get; set; }
        public DbSet<Vehicle> Vehicles { get; set; }
        public DbSet<Driver> Drivers { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<TripLog> TripLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Завжди викликайте базовий метод на початку

            // === Конфігурації для VehicleType ===
            modelBuilder.Entity<VehicleType>(entity =>
            {
                entity.HasIndex(vt => vt.Name).IsUnique(); // Назва типу ТЗ має бути унікальною
            });

            // === Конфігурації для Vehicle ===
            modelBuilder.Entity<Vehicle>(entity =>
            {
                entity.HasIndex(v => v.LicensePlate).IsUnique(); // Номерний знак має бути унікальним

                entity.HasOne(v => v.VehicleType)
                      .WithMany(vt => vt.Vehicles)
                      .HasForeignKey(v => v.VehicleTypeId)
                      .OnDelete(DeleteBehavior.Restrict); // Забороняємо видаляти VehicleType, якщо є пов'язані Vehicles
            });

            // === Конфігурації для Driver ===
            modelBuilder.Entity<Driver>(entity =>
            {
                entity.HasIndex(d => d.Email).IsUnique(); // Email водія має бути унікальним
                entity.HasIndex(d => d.LicenseNumber).IsUnique(); // Номер посвідчення водія має бути унікальним
            });

            // === Конфігурації для Assignment (зв'язок M:M між Vehicle та Driver) ===
            modelBuilder.Entity<Assignment>(entity =>
            {
                // Композитний ключ (якщо немає окремого AssignmentId, але у вас є)
                // entity.HasKey(a => new { a.VehicleId, a.DriverId, a.StartDate }); // Приклад, якщо потрібно

                entity.HasOne(a => a.Vehicle)
                      .WithMany(v => v.Assignments)
                      .HasForeignKey(a => a.VehicleId)
                      .OnDelete(DeleteBehavior.Cascade); // Видалення ТЗ видаляє його призначення

                entity.HasOne(a => a.Driver)
                      .WithMany(d => d.Assignments)
                      .HasForeignKey(a => a.DriverId)
                      .OnDelete(DeleteBehavior.Cascade); // Видалення водія видаляє його призначення
            });

            // === Конфігурації для MaintenanceRecord ===
            modelBuilder.Entity<MaintenanceRecord>(entity =>
            {
                entity.Property(m => m.Cost)
                      .HasColumnType("decimal(10,2)"); // Точність для вартості

                entity.HasOne(m => m.Vehicle)
                      .WithMany(v => v.MaintenanceRecords)
                      .HasForeignKey(m => m.VehicleId)
                      .OnDelete(DeleteBehavior.Cascade); // Видалення ТЗ видаляє його записи ТО
            });

            // === Конфігурації для Trip ===
            modelBuilder.Entity<Trip>(entity =>
            {
                // Якщо RouteDescription має бути унікальним (наприклад, для стандартних маршрутів), розкоментуйте:
                // entity.HasIndex(t => t.RouteDescription).IsUnique();
                // Але судячи з прикладу "Київ - Львів - Доставка вантажу #123", це не унікальний ідентифікатор маршруту,
                // а опис конкретної поїздки, тому унікальність тут, ймовірно, не потрібна.
            });

            // === Конфігурації для TripLog (тернарний зв'язок) ===
            modelBuilder.Entity<TripLog>(entity =>
            {
                entity.Property(tl => tl.FuelConsumedLiters)
                      .HasColumnType("decimal(7,2)"); // Точність для пального

                // Зв'язок з Trip
                entity.HasOne(tl => tl.Trip)
                      .WithMany(t => t.TripLogs)
                      .HasForeignKey(tl => tl.TripId)
                      .OnDelete(DeleteBehavior.Cascade); // Видалення поїздки видаляє її записи в журналі

                // Зв'язок з Driver
                entity.HasOne(tl => tl.Driver)
                      .WithMany(d => d.TripLogs)
                      .HasForeignKey(tl => tl.DriverId)
                      .OnDelete(DeleteBehavior.Restrict); // Забороняємо видаляти водія, якщо є записи в журналі

                // Зв'язок з Vehicle
                entity.HasOne(tl => tl.Vehicle)
                      .WithMany(v => v.TripLogs)
                      .HasForeignKey(tl => tl.VehicleId)
                      .OnDelete(DeleteBehavior.Restrict); // Забороняємо видаляти ТЗ, якщо є записи в журналі
            });
        }
    }
}