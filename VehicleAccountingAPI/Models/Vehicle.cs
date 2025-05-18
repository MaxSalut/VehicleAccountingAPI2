// File: Models/Vehicle.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic; // Додано для List

namespace VehicleAccountingAPI.Models
{
    public class Vehicle
    {
        [Key]
        public int VehicleId { get; set; }

        [Required]
        [StringLength(20, ErrorMessage = "Довжина має бути до 20 символів")] // Змінено з 100 на 20 для номерного знаку, як у коментарі
        public string LicensePlate { get; set; } = string.Empty;

        [Required]
        [StringLength(100, ErrorMessage = "Довжина має бути до 100 символів")]
        public string Model { get; set; } = string.Empty;

        [Range(1900, 2025, ErrorMessage = "Рік повинен бути між 1900 і 2025")] // Поточний рік 2025, можливо, варто оновити верхню межу
        public int Year { get; set; }

        [ForeignKey("VehicleType")]
        public int VehicleTypeId { get; set; }
        public VehicleType? VehicleType { get; set; }

        public List<MaintenanceRecord> MaintenanceRecords { get; set; } = new();
        public List<Assignment> Assignments { get; set; } = new();
        public List<TripLog> TripLogs { get; set; } = new(); // <--- ДОДАНО ЦЕЙ РЯДОК
    }
}