// File: Controllers/AssignmentsController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleAccountingAPI.Data;
using VehicleAccountingAPI.Models;
using System.Linq; 

namespace VehicleAccountingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssignmentsController : ControllerBase
    {
        private readonly VehicleAccountingContext _context;

        public AssignmentsController(VehicleAccountingContext context)
        {
            _context = context;
        }

        // GET: api/Assignments
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Assignment>>> GetAssignments()
        {
            return await _context.Assignments
                .Include(a => a.Vehicle) 
                .Include(a => a.Driver)  
                .ToListAsync();
        }

        // GET: api/Assignments/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Assignment>> GetAssignment(int id)
        {
            var assignment = await _context.Assignments
                .Include(a => a.Vehicle)
                .Include(a => a.Driver)
                .FirstOrDefaultAsync(a => a.AssignmentId == id);

            if (assignment == null)
            {
                return NotFound();
            }

            return assignment;
        }

        // PUT: api/Assignments/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutAssignment(int id, Assignment assignment)
        {
            if (id != assignment.AssignmentId)
            {
                return BadRequest("ID у запиті не співпадає з ID об'єкта.");
            }

            // ModelState.IsValid перевірить як атрибути DataAnnotations,
            // так і метод Validate, якщо Assignment реалізує IValidatableObject.
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Entry(assignment).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AssignmentExists(id))
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
                return StatusCode(StatusCodes.Status500InternalServerError, "Помилка оновлення даних. Можливо, порушено обмеження бази даних.");
            }


            return NoContent();
        }

        // POST: api/Assignments
        [HttpPost]
        public async Task<ActionResult<Assignment>> PostAssignment(Assignment assignment)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _context.Assignments.Add(assignment);
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                
                return StatusCode(StatusCodes.Status500InternalServerError, "Помилка збереження даних. Можливо, порушено обмеження бази даних.");
            }

            return CreatedAtAction(nameof(GetAssignment), new { id = assignment.AssignmentId }, assignment);
        }

        // DELETE: api/Assignments/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAssignment(int id)
        {
            var assignment = await _context.Assignments.FindAsync(id);
            if (assignment == null)
            {
                return NotFound();
            }

            _context.Assignments.Remove(assignment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool AssignmentExists(int id)
        {
            return _context.Assignments.Any(e => e.AssignmentId == id);
        }

        // GET: api/Assignments/status-count
        [HttpGet("status-count")]
        public async Task<ActionResult<object>> GetAssignmentStatusCount()
        {
            var activeCount = await _context.Assignments
                .CountAsync(a => a.EndDate == null || a.EndDate >= DateTime.UtcNow); 
            var inactiveCount = await _context.Assignments
                .CountAsync(a => a.EndDate != null && a.EndDate < DateTime.UtcNow); 

            return new
            {
                Active = activeCount,
                Inactive = inactiveCount
            };
        }
    }
}