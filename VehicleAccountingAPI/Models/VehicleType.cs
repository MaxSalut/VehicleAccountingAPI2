using System.ComponentModel.DataAnnotations;

namespace VehicleAccountingAPI.Models
{
    public class VehicleType
    {
        [Key]
        public int VehicleTypeId { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        public List<Vehicle> Vehicles { get; set; } = new();
    }
}