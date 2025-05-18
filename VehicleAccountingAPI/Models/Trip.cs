// File: Models/Trip.cs
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace VehicleAccountingAPI.Models
{
    public class Trip
    {
        [Key]
        public int TripId { get; set; }

        [Required(ErrorMessage = "Опис маршруту є обов'язковим.")]
        [StringLength(250, ErrorMessage = "Опис маршруту не повинен перевищувати 250 символів.")]
        public string RouteDescription { get; set; } = string.Empty; // Наприклад, "Київ - Львів - Доставка вантажу #123"

        [StringLength(500, ErrorMessage = "Опис вантажу не повинен перевищувати 500 символів.")]
        public string? CargoDescription { get; set; }

        [Required(ErrorMessage = "Планова дата та час початку є обов'язковими.")]
        [DataType(DataType.DateTime)]
        public DateTime PlannedStartDateTime { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? PlannedEndDateTime { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? ActualStartDateTime { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime? ActualEndDateTime { get; set; }

        [Required(ErrorMessage = "Статус поїздки є обов'язковим.")]
        [StringLength(50, ErrorMessage = "Статус поїздки не повинен перевищувати 50 символів.")]
        public string Status { get; set; } = string.Empty; // Наприклад, "Заплановано", "В дорозі", "Завершено", "Скасовано"

        // Навігаційна властивість для зв'язку з TripLog
        public List<TripLog> TripLogs { get; set; } = new();

        // Можна додати кастомну валідацію, наприклад, PlannedEndDateTime > PlannedStartDateTime
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (PlannedEndDateTime.HasValue && PlannedEndDateTime.Value <= PlannedStartDateTime)
            {
                yield return new ValidationResult(
                    "Планова дата завершення повинна бути пізнішою за планову дату початку.",
                    new[] { nameof(PlannedEndDateTime) });
            }
            if (ActualEndDateTime.HasValue && ActualStartDateTime.HasValue && ActualEndDateTime.Value <= ActualStartDateTime.Value)
            {
                yield return new ValidationResult(
                    "Фактична дата завершення повинна бути пізнішою за фактичну дату початку.",
                    new[] { nameof(ActualEndDateTime) });
            }
            yield break;
        }
    }
}