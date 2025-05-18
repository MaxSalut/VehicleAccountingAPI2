// File: Controllers/TripsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleAccountingAPI.Data;
using VehicleAccountingAPI.Models;
using System.Linq; 

namespace VehicleAccountingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly VehicleAccountingContext _context;

        public TripsController(VehicleAccountingContext context)
        {
            _context = context;
        }

        // GET: api/Trips
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Trip>>> GetTrips()
        {
            return await _context.Trips
                .OrderByDescending(t => t.PlannedStartDateTime) 
                .ToListAsync();
        }

        // GET: api/Trips/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Trip>> GetTrip(int id)
        {
            var trip = await _context.Trips
                .Include(t => t.TripLogs) 
                    .ThenInclude(tl => tl.Driver) 
                .Include(t => t.TripLogs)
                    .ThenInclude(tl => tl.Vehicle) 
                .FirstOrDefaultAsync(t => t.TripId == id);

            if (trip == null)
            {
                return NotFound();
            }

            return trip;
        }

        // PUT: api/Trips/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutTrip(int id, Trip trip)
        {
            if (id != trip.TripId)
            {
                return BadRequest("ID у запиті не співпадає з ID об'єкта.");
            }

            if (!ModelState.IsValid) // Перевіряє DataAnnotations та IValidatableObject (метод Validate в моделі Trip)
            {
                return BadRequest(ModelState);
            }

            _context.Entry(trip).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TripExists(id))
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
                 
                return StatusCode(StatusCodes.Status500InternalServerError, "Помилка оновлення даних поїздки.");
            }

            return NoContent();
        }

        // POST: api/Trips
        [HttpPost]
        public async Task<ActionResult<Trip>> PostTrip(Trip trip)
        {
            if (!ModelState.IsValid) 
            {
                return BadRequest(ModelState);
            }

            _context.Trips.Add(trip);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                 
                return StatusCode(StatusCodes.Status500InternalServerError, "Помилка створення поїздки.");
            }

            return CreatedAtAction(nameof(GetTrip), new { id = trip.TripId }, trip);
        }

        // DELETE: api/Trips/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTrip(int id)
        {
            var trip = await _context.Trips.FindAsync(id);
            if (trip == null)
            {
                return NotFound();
            }

            // Завдяки OnDelete(DeleteBehavior.Cascade) для TripLogs, пов'язані TripLogs будуть видалені автоматично.
            
            _context.Trips.Remove(trip);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TripExists(int id)
        {
            return _context.Trips.Any(e => e.TripId == id);
        }
    }
}