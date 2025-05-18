// File: Controllers/VehicleTypesController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleAccountingAPI.Data;
using VehicleAccountingAPI.Models;
using System.Linq; // Для .Any()

namespace VehicleAccountingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class VehicleTypesController : ControllerBase
    {
        private readonly VehicleAccountingContext _context;

        public VehicleTypesController(VehicleAccountingContext context)
        {
            _context = context;
        }

        // GET: api/VehicleTypes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<VehicleType>>> GetVehicleTypes()
        {
            return await _context.VehicleTypes.OrderBy(vt => vt.Name).ToListAsync();
        }

        // GET: api/VehicleTypes/5
        [HttpGet("{id}")]
        public async Task<ActionResult<VehicleType>> GetVehicleType(int id)
        {
            var vehicleType = await _context.VehicleTypes
                .Include(vt => vt.Vehicles) // Включаємо пов'язані ТЗ
                .FirstOrDefaultAsync(vt => vt.VehicleTypeId == id);

            if (vehicleType == null)
            {
                return NotFound();
            }

            return vehicleType;
        }

        // PUT: api/VehicleTypes/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutVehicleType(int id, VehicleType vehicleType)
        {
            if (id != vehicleType.VehicleTypeId)
            {
                return BadRequest("ID у запиті не співпадає з ID об'єкта.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Додаткова перевірка на унікальність назви, якщо вона змінилася
            var existingTypeWithSameName = await _context.VehicleTypes
                .FirstOrDefaultAsync(vt => vt.Name == vehicleType.Name && vt.VehicleTypeId != id);
            if (existingTypeWithSameName != null)
            {
                ModelState.AddModelError(nameof(vehicleType.Name), "Тип транспортного засобу з такою назвою вже існує.");
                return BadRequest(ModelState);
            }

            _context.Entry(vehicleType).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VehicleTypeExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (DbUpdateException ex) // Обробка можливих конфліктів унікальності (якщо індекс на Name)
            {
                // Додайте логування помилки ex
                // Цей блок може бути зайвим, якщо ми вже перевіряємо унікальність назви вище,
                // але залишено для обробки інших можливих DbUpdateException.
                return StatusCode(StatusCodes.Status500InternalServerError, "Помилка оновлення типу ТЗ. Можливо, така назва вже використовується.");
            }

            return NoContent();
        }

        // POST: api/VehicleTypes
        [HttpPost]
        public async Task<ActionResult<VehicleType>> PostVehicleType(VehicleType vehicleType)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Перевірка на унікальність назви перед додаванням
            if (await _context.VehicleTypes.AnyAsync(vt => vt.Name == vehicleType.Name))
            {
                ModelState.AddModelError(nameof(vehicleType.Name), "Тип транспортного засобу з такою назвою вже існує.");
                return BadRequest(ModelState);
            }

            _context.VehicleTypes.Add(vehicleType);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex) // На випадок, якщо перевірка вище не спрацювала через race condition
            {
                // Додайте логування помилки ex
                return StatusCode(StatusCodes.Status500InternalServerError, "Помилка створення типу ТЗ. Можливо, така назва вже використовується.");
            }

            return CreatedAtAction(nameof(GetVehicleType), new { id = vehicleType.VehicleTypeId }, vehicleType);
        }

        // DELETE: api/VehicleTypes/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteVehicleType(int id)
        {
            var vehicleType = await _context.VehicleTypes.FindAsync(id);
            if (vehicleType == null)
            {
                return NotFound();
            }

            // Перевірка, чи є пов'язані ТЗ, якщо OnDelete=Restrict
            if (await _context.Vehicles.AnyAsync(v => v.VehicleTypeId == id))
            {
                return BadRequest("Неможливо видалити тип ТЗ, оскільки існують транспортні засоби цього типу.");
            }

            _context.VehicleTypes.Remove(vehicleType);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool VehicleTypeExists(int id)
        {
            return _context.VehicleTypes.Any(e => e.VehicleTypeId == id);
        }
    }
}