// File: Models/Driver.cs
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic; // Додано для List

namespace VehicleAccountingAPI.Models
{
    public class Driver
    {
        [Key]
        public int DriverId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(20)] // Довжина номера посвідчення водія
        public string LicenseNumber { get; set; } = string.Empty;

        public List<Assignment> Assignments { get; set; } = new();
        public List<TripLog> TripLogs { get; set; } = new(); // <--- ДОДАНО ЦЕЙ РЯДОК
    }
}