using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleAccountingAPI.Models
{
    public class MaintenanceRecord
    {
        [Key]
        public int MaintenanceRecordId { get; set; }

        [ForeignKey("Vehicle")]
        public int VehicleId { get; set; }

        [Required(ErrorMessage = "Поле обов'язкове для заповнення")]
        [DataType(DataType.Date, ErrorMessage = "Введіть коректну дату")]
        public DateTime MaintenanceDate { get; set; }

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(0, 10000000)]
        public decimal Cost { get; set; }

        public Vehicle? Vehicle { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var minDate = new DateTime(2000, 1, 1);
            var maxDate = DateTime.Now;

            // Перевірка, чи дата не занадто в минулому
            if (MaintenanceDate < minDate)
            {
                yield return new ValidationResult(
                    $"Дата не може бути раніше {minDate:dd.MM.yyyy}.",
                    new[] { nameof(MaintenanceDate) });
            }

            // Перевірка, чи дата не в майбутньому
            if (MaintenanceDate > maxDate)
            {
                yield return new ValidationResult(
                    "Дата не може бути в майбутньому.",
                    new[] { nameof(MaintenanceDate) });
            }

            yield break;
        }
    }
}