using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VehicleAccountingAPI.Models
{
    public class Assignment
    {
        [Key]
        public int AssignmentId { get; set; }

        [ForeignKey("Vehicle")]
        public int VehicleId { get; set; }

        [ForeignKey("Driver")]
        public int DriverId { get; set; }

        [Required]
        [DataType(DataType.Date, ErrorMessage = "Введіть коректну дату")]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date, ErrorMessage = "Введіть коректну дату")]
        public DateTime? EndDate { get; set; }

        public Vehicle? Vehicle { get; set; }
        public Driver? Driver { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            var minDate = new DateTime(2000, 1, 1);
            var maxDate = DateTime.Now;

            // Перевірка StartDate
            if (StartDate < minDate)
            {
                yield return new ValidationResult(
                    $"Дата початку не може бути раніше {minDate:dd.MM.yyyy}.",
                    new[] { nameof(StartDate) });
            }

            if (StartDate > maxDate)
            {
                yield return new ValidationResult(
                    "Дата початку не може бути в майбутньому.",
                    new[] { nameof(StartDate) });
            }

            // Перевірка EndDate (якщо вона вказана)
            if (EndDate.HasValue)
            {
                if (EndDate.Value < minDate)
                {
                    yield return new ValidationResult(
                        $"Дата завершення не може бути раніше {minDate:dd.MM.yyyy}.",
                        new[] { nameof(EndDate) });
                }

                if (EndDate.Value > maxDate)
                {
                    yield return new ValidationResult(
                        "Дата завершення не може бути в майбутньому.",
                        new[] { nameof(EndDate) });
                }

                if (EndDate.Value < StartDate)
                {
                    yield return new ValidationResult(
                        "Дата завершення не може бути раніше дати початку.",
                        new[] { nameof(EndDate) });
                }
            }

            yield break;
        }
    }
}