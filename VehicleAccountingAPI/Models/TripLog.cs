// File: Models/TripLog.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System; // Required for DateTime
using System.Collections.Generic; // Required for IEnumerable

namespace VehicleAccountingAPI.Models
{
    public class TripLog
    {
        [Key]
        public int TripLogId { get; set; }

        [Required]
        [ForeignKey("Driver")]
        public int DriverId { get; set; }
        public Driver? Driver { get; set; }

        [Required]
        [ForeignKey("Vehicle")]
        public int VehicleId { get; set; }
        public Vehicle? Vehicle { get; set; }

        [Required]
        [ForeignKey("Trip")]
        public int TripId { get; set; }
        public Trip? Trip { get; set; }

        [Required(ErrorMessage = "Дата запису є обов'язковою.")]
        [DataType(DataType.Date, ErrorMessage = "Введіть коректну дату запису.")]
        public DateTime LogDate { get; set; } // Дата конкретного запису в журналі для поїздки

        [Range(0, 2000000, ErrorMessage = "Пробіг повинен бути невід'ємним числом до 2,000,000 км.")]
        public int? StartMileage { get; set; } // Початковий пробіг для цього запису/етапу

        [Range(0, 2000000, ErrorMessage = "Пробіг повинен бути невід'ємним числом до 2,000,000 км.")]
        public int? EndMileage { get; set; } // Кінцевий пробіг

        [Range(0, 10000, ErrorMessage = "Витрата пального повинна бути в межах від 0 до 10,000 літрів.")]
        public decimal? FuelConsumedLiters { get; set; } // Витрачено пального

        [StringLength(1000, ErrorMessage = "Примітки не повинні перевищувати 1000 символів.")]
        public string? Notes { get; set; } // Додаткові примітки

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var minDate = new DateTime(2000, 1, 1);
            var maxDate = DateTime.Now.AddDays(1); // Дозволяємо сьогоднішню + завтрашню дату для записів, що можуть створюватись наперед за день

            if (LogDate < minDate)
            {
                yield return new ValidationResult(
                    $"Дата запису не може бути раніше {minDate:dd.MM.yyyy}.",
                    new[] { nameof(LogDate) });
            }

            if (LogDate > maxDate)
            {
                yield return new ValidationResult(
                    "Дата запису не може бути в майбутньому (більш ніж на 1 день).",
                    new[] { nameof(LogDate) });
            }

            if (StartMileage.HasValue && EndMileage.HasValue && EndMileage < StartMileage)
            {
                yield return new ValidationResult(
                    "Кінцевий пробіг не може бути меншим за початковий.",
                    new[] { nameof(EndMileage), nameof(StartMileage) });
            }
            yield break;
        }
    }
}
