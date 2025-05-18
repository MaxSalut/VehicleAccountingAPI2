// File: Controllers/TripLogsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleAccountingAPI.Data;
using VehicleAccountingAPI.Models;
using System.Linq; 

namespace VehicleAccountingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripLogsController : ControllerBase
    {
        private readonly VehicleAccountingContext _context;

        public TripLogsController(VehicleAccountingContext context)
        {
            _context = context;
        }

        // GET: api/TripLogs
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TripLog>>> GetTripLogs()
        {
            return await _context.TripLogs
                .Include(tl => tl.Driver)  
                .Include(tl => tl.Vehicle) 
                .Include(tl => tl.Trip)    
                .OrderByDescending(tl => tl.LogDate) 
                .ToListAsync();
        }

        // GET: api/TripLogs/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TripLog>> GetTripLog(int id)
        {
            var tripLog = await _context.TripLogs
                .Include(tl => tl.Driver)
                .Include(tl => tl.Vehicle)
                .Include(tl => tl.Trip)
                .FirstOrDefaultAsync(tl => tl.TripLogId == id); 
            if (tripLog == null)
            {
                return NotFound();
            }

            return tripLog;
        }

        // PUT: api/TripLogs/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTripLog(int id, TripLog tripLog)
        {
            if (id != tripLog.TripLogId)
            {
                return BadRequest("ID у запиті не співпадає з ID об'єкта.");
            }

            if (!ModelState.IsValid) 
            {
                return BadRequest(ModelState);
            }

            
            if (!await _context.Drivers.AnyAsync(d => d.DriverId == tripLog.DriverId))
                ModelState.AddModelError(nameof(tripLog.DriverId), "Вказаний водій не існує.");
            if (!await _context.Vehicles.AnyAsync(v => v.VehicleId == tripLog.VehicleId))
                ModelState.AddModelError(nameof(tripLog.VehicleId), "Вказаний транспортний засіб не існує.");
            if (!await _context.Trips.AnyAsync(t => t.TripId == tripLog.TripId))
                ModelState.AddModelError(nameof(tripLog.TripId), "Вказана поїздка не існує.");

            if (!ModelState.IsValid) 
            {
                return BadRequest(ModelState);
            }

            _context.Entry(tripLog).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TripLogExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Помилка оновлення запису журналу поїздки.");
            }

            return NoContent();
        }

        // POST: api/TripLogs
        [HttpPost]
        public async Task<ActionResult<TripLog>> PostTripLog(TripLog tripLog)
        {
            if (!ModelState.IsValid) 
            {
                return BadRequest(ModelState);
            }

            // Додаткові перевірки існування зовнішніх ключів
            if (!await _context.Drivers.AnyAsync(d => d.DriverId == tripLog.DriverId))
                ModelState.AddModelError(nameof(tripLog.DriverId), "Вказаний водій не існує.");
            if (!await _context.Vehicles.AnyAsync(v => v.VehicleId == tripLog.VehicleId))
                ModelState.AddModelError(nameof(tripLog.VehicleId), "Вказаний транспортний засіб не існує.");
            if (!await _context.Trips.AnyAsync(t => t.TripId == tripLog.TripId))
                ModelState.AddModelError(nameof(tripLog.TripId), "Вказана поїздка не існує.");

            if (!ModelState.IsValid) 
            {
                return BadRequest(ModelState);
            }

            _context.TripLogs.Add(tripLog);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                                return StatusCode(StatusCodes.Status500InternalServerError, "Помилка створення запису журналу поїздки.");
            }

           
            var createdTripLog = await _context.TripLogs
                .Include(tl => tl.Driver)
                .Include(tl => tl.Vehicle)
                .Include(tl => tl.Trip)
                .FirstOrDefaultAsync(tl => tl.TripLogId == tripLog.TripLogId);


            return CreatedAtAction(nameof(GetTripLog), new { id = tripLog.TripLogId }, createdTripLog);
        }

        // DELETE: api/TripLogs/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTripLog(int id)
        {
            var tripLog = await _context.TripLogs.FindAsync(id);
            if (tripLog == null)
            {
                return NotFound();
            }

            _context.TripLogs.Remove(tripLog);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TripLogExists(int id)
        {
            return _context.TripLogs.Any(e => e.TripLogId == id);
        }
    }
}