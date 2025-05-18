// File: Controllers/DriversController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleAccountingAPI.Data;
using VehicleAccountingAPI.Models;
using System.Linq; // Для .Any()

namespace VehicleAccountingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DriversController : ControllerBase
    {
        private readonly VehicleAccountingContext _context;

        public DriversController(VehicleAccountingContext context)
        {
            _context = context;
        }

        // GET: api/Drivers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Driver>>> GetDrivers()
        {
            // Для списку водіїв зазвичай достатньо основної інформації.
            // Якщо потрібні пов'язані дані, їх можна додати через Include.
            return await _context.Drivers.ToListAsync();
        }

        // GET: api/Drivers/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Driver>> GetDriver(int id)
        {
            var driver = await _context.Drivers
                .Include(d => d.Assignments) // Включаємо призначення
                    .ThenInclude(a => a.Vehicle) // В призначеннях можна завантажити деталі ТЗ
                .Include(d => d.TripLogs)    // Включаємо історію поїздок
                    .ThenInclude(tl => tl.Trip) // В записах журналу можна завантажити деталі поїздки
                .FirstOrDefaultAsync(d => d.DriverId == id);

            if (driver == null)
            {
                return NotFound();
            }

            return driver;
        }

        // PUT: api/Drivers/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutDriver(int id, Driver driver)
        {
            if (id != driver.DriverId)
            {
                return BadRequest("ID у запиті не співпадає з ID об'єкта.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Entry(driver).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!DriverExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (DbUpdateException ex) // Обробка можливих конфліктів унікальності (наприклад, Email, LicenseNumber)
            {
                // Додайте логування помилки ex
                // Перевірка на конкретний тип помилки (наприклад, унікальний індекс) може бути складною без аналізу ex.InnerException
                return StatusCode(StatusCodes.Status500InternalServerError, "Помилка оновлення даних водія. Можливо, такий Email або номер посвідчення вже існує.");
            }

            return NoContent();
        }

        // POST: api/Drivers
        [HttpPost]
        public async Task<ActionResult<Driver>> PostDriver(Driver driver)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Drivers.Add(driver);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) // Обробка можливих конфліктів унікальності
            {
                // Додайте логування помилки ex
                return StatusCode(StatusCodes.Status500InternalServerError, "Помилка створення водія. Можливо, такий Email або номер посвідчення вже існує.");
            }


            return CreatedAtAction(nameof(GetDriver), new { id = driver.DriverId }, driver);
        }

        // DELETE: api/Drivers/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDriver(int id)
        {
            var driver = await _context.Drivers.FindAsync(id);
            if (driver == null)
            {
                return NotFound();
            }

            // Перевірка залежностей перед видаленням, якщо OnDelete=Restrict/NoAction
            if (await _context.Assignments.AnyAsync(a => a.DriverId == id) ||
                await _context.TripLogs.AnyAsync(tl => tl.DriverId == id))
            {
                return BadRequest("Неможливо видалити водія, оскільки він має пов'язані призначення або записи в журналі поїздок. Спочатку видаліть або перепризначте їх.");
            }

            _context.Drivers.Remove(driver);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool DriverExists(int id)
        {
            return _context.Drivers.Any(e => e.DriverId == id);
        }
    }
}