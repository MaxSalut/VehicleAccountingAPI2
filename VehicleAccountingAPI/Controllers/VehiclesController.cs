// File: Controllers/VehiclesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleAccountingAPI.Data;
using VehicleAccountingAPI.Models;
using System.Linq; // Для .Any()

namespace VehicleAccountingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehiclesController : ControllerBase
    {
        private readonly VehicleAccountingContext _context;

        public VehiclesController(VehicleAccountingContext context)
        {
            _context = context;
        }

        // GET: api/Vehicles
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Vehicle>>> GetVehicles()
        {
            return await _context.Vehicles
                .Include(v => v.VehicleType) // Включаємо тип ТЗ
                .ToListAsync();
        }

        // GET: api/Vehicles/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Vehicle>> GetVehicle(int id)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.VehicleType)
                .Include(v => v.MaintenanceRecords) // Включаємо записи ТО
                .Include(v => v.Assignments)       // Включаємо призначення
                    .ThenInclude(a => a.Driver)    // В призначеннях завантажуємо водія
                .Include(v => v.TripLogs)          // Включаємо журнал поїздок
                    .ThenInclude(tl => tl.Trip)    // В журналі завантажуємо деталі поїздки
                .FirstOrDefaultAsync(v => v.VehicleId == id);

            if (vehicle == null)
            {
                return NotFound();
            }

            return vehicle;
        }

        // PUT: api/Vehicles/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVehicle(int id, Vehicle vehicle)
        {
            if (id != vehicle.VehicleId)
            {
                return BadRequest("ID у запиті не співпадає з ID об'єкта.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Перевірка існування VehicleTypeId
            if (!await _context.VehicleTypes.AnyAsync(vt => vt.VehicleTypeId == vehicle.VehicleTypeId))
            {
                ModelState.AddModelError(nameof(vehicle.VehicleTypeId), "Вказаний тип транспортного засобу не існує.");
                return BadRequest(ModelState);
            }

            _context.Entry(vehicle).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VehicleExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (DbUpdateException ex) // Обробка можливих конфліктів унікальності (наприклад, LicensePlate)
            {
                 
                return StatusCode(StatusCodes.Status500InternalServerError, "Помилка оновлення ТЗ. Можливо, такий номерний знак вже існує.");
            }

            return NoContent();
        }

        // POST: api/Vehicles
        [HttpPost]
        public async Task<ActionResult<Vehicle>> PostVehicle(Vehicle vehicle)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Перевірка існування VehicleTypeId
            if (!await _context.VehicleTypes.AnyAsync(vt => vt.VehicleTypeId == vehicle.VehicleTypeId))
            {
                ModelState.AddModelError(nameof(vehicle.VehicleTypeId), "Вказаний тип транспортного засобу не існує.");
                return BadRequest(ModelState);
            }

            _context.Vehicles.Add(vehicle);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) // Обробка можливих конфліктів унікальності
            {
                 
                return StatusCode(StatusCodes.Status500InternalServerError, "Помилка створення ТЗ. Можливо, такий номерний знак вже існує.");
            }

            // Повертаємо створений об'єкт з включеним VehicleType для повноти
            var createdVehicle = await _context.Vehicles
                .Include(v => v.VehicleType)
                .FirstOrDefaultAsync(v => v.VehicleId == vehicle.VehicleId);

            return CreatedAtAction(nameof(GetVehicle), new { id = vehicle.VehicleId }, createdVehicle);
        }

        // DELETE: api/Vehicles/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicle(int id)
        {
            var vehicle = await _context.Vehicles.FindAsync(id);
            if (vehicle == null)
            {
                return NotFound();
            }

            // Перевірка залежностей перед видаленням
            if (await _context.MaintenanceRecords.AnyAsync(mr => mr.VehicleId == id) ||
                await _context.Assignments.AnyAsync(a => a.VehicleId == id) ||
                await _context.TripLogs.AnyAsync(tl => tl.VehicleId == id))
            {
                // Якщо OnDelete для TripLogs встановлено в Restrict, ця перевірка для TripLogs потрібна.
                // Якщо для MaintenanceRecords та Assignments встановлено Cascade, то їх перевіряти не обов'язково тут,
                // але для інформативності відповіді API це може бути корисно.
                return BadRequest("Неможливо видалити ТЗ, оскільки він має пов'язані записи ТО, призначення або записи в журналі поїздок.");
            }

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool VehicleExists(int id)
        {
            return _context.Vehicles.Any(e => e.VehicleId == id);
        }
    }
}