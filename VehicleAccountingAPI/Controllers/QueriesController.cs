using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleAccountingAPI.Data;
using VehicleAccountingAPI.Models; // Якщо повертаєте моделі
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // Для IEnumerable

namespace VehicleAccountingAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class QueriesController : ControllerBase
    {
        private readonly VehicleAccountingContext _context;

        public QueriesController(VehicleAccountingContext context)
        {
            _context = context;
        }

        // --- Прості параметризовані запити ---

        // Q_S1: Водії, призначені на транспортні засоби певного типу
        [HttpGet("drivers-by-vehicle-type")]
        public async Task<IActionResult> GetDriversByVehicleType([FromQuery] string vehicleTypeName)
        {
            if (string.IsNullOrWhiteSpace(vehicleTypeName))
            {
                return BadRequest("Параметр 'НазваТипуТЗ' є обов'язковим.");
            }

            var query = from driver in _context.Drivers
                        join assignment in _context.Assignments on driver.DriverId equals assignment.DriverId
                        join vehicle in _context.Vehicles on assignment.VehicleId equals vehicle.VehicleId
                        join vehicleType in _context.VehicleTypes on vehicle.VehicleTypeId equals vehicleType.VehicleTypeId
                        where vehicleType.Name == vehicleTypeName
                        select new
                        {
                            driver.DriverId,
                            driver.Name,
                            driver.Email,
                            driver.LicenseNumber
                        };

            var result = await query.Distinct().ToListAsync();

            if (!result.Any())
            {
                return NotFound($"Водії для типу ТЗ '{vehicleTypeName}' не знайдені.");
            }
            return Ok(result);
        }

        // Q_S2: Транспортні засоби з ТО дорожчим за X та після дати Y
        [HttpGet("vehicles-by-maintenance")]
        public async Task<IActionResult> GetVehiclesByMaintenance([FromQuery] decimal minCost, [FromQuery] DateTime dateAfter)
        {
            if (minCost <= 0)
            {
                return BadRequest("Мінімальна вартість ТО повинна бути більшою за нуль.");
            }

            var vehicleIdsWithMatchingMaintenance = await _context.MaintenanceRecords
                .Where(mr => mr.MaintenanceDate > dateAfter && mr.Cost > minCost)
                .Select(mr => mr.VehicleId)
                .Distinct()
                .ToListAsync();

            if (!vehicleIdsWithMatchingMaintenance.Any())
            {
                return NotFound("Транспортні засоби, що відповідають критеріям ТО, не знайдені.");
            }

            var result = await _context.Vehicles
                .Where(v => vehicleIdsWithMatchingMaintenance.Contains(v.VehicleId))
                .Include(v => v.VehicleType)
                .Select(v => new {
                    v.VehicleId,
                    v.LicensePlate,
                    v.Model,
                    v.Year,
                    VehicleTypeName = v.VehicleType.Name
                })
                .ToListAsync();

            return Ok(result);
        }

        // Q_S3: Поїздки певного водія за вказаний період фактичного початку
        [HttpGet("trips-by-driver-period")]
        public async Task<IActionResult> GetTripsByDriverPeriod([FromQuery] int driverId, [FromQuery] DateTime periodStartDate, [FromQuery] DateTime periodEndDate)
        {
            if (driverId <= 0) return BadRequest("ID Водія має бути позитивним числом.");
            if (periodEndDate < periodStartDate) return BadRequest("Дата кінця періоду не може бути раніше дати початку.");

            var result = await _context.Trips
                .Where(t => t.TripLogs.Any(tl => tl.DriverId == driverId) &&
                            t.ActualStartDateTime >= periodStartDate &&
                            t.ActualStartDateTime <= periodEndDate)
                .Select(t => new {
                    t.TripId,
                    t.RouteDescription,
                    t.CargoDescription,
                    t.PlannedStartDateTime,
                    t.PlannedEndDateTime,
                    t.ActualStartDateTime,
                    t.ActualEndDateTime,
                    t.Status
                    // Можна додати ім'я водія, якщо потрібно, але він вже є параметром
                })
                .ToListAsync();

            if (!result.Any())
            {
                return NotFound($"Поїздки для водія ID={driverId} у вказаний період не знайдені.");
            }
            return Ok(result);
        }

        // Q_S4: Активно призначені ТЗ старші за вказаний рік
        [HttpGet("active-vehicles-older-than")]
        public async Task<IActionResult> GetActiveVehiclesOlderThan([FromQuery] int yearOlderThan)
        {
            if (yearOlderThan <= 1900 || yearOlderThan > DateTime.Now.Year + 1)
                return BadRequest($"Рік випуску має бути в розумних межах (наприклад, 1901 - {DateTime.Now.Year + 1}).");

            var currentDate = DateTime.UtcNow; // Або DateTime.Now, залежно від зберігання дат
            var result = await _context.Vehicles
                .Where(v => v.Year < yearOlderThan &&
                            v.Assignments.Any(a => a.EndDate == null || a.EndDate > currentDate))
                .Include(v => v.VehicleType)
                .Select(v => new {
                    v.VehicleId,
                    v.LicensePlate,
                    v.Model,
                    v.Year,
                    VehicleTypeName = v.VehicleType.Name
                })
                .ToListAsync();

            if (!result.Any())
            {
                return NotFound($"Активно призначені ТЗ, старші за {yearOlderThan} рік, не знайдені.");
            }
            return Ok(result);
        }

        // Q_S5: Деталі журналу поїздок з пробігом більше X для певного ТЗ
        [HttpGet("triplogs-by-mileage")]
        public async Task<IActionResult> GetTripLogsByMileage([FromQuery] int vehicleId, [FromQuery] int minMileage)
        {
            if (vehicleId <= 0) return BadRequest("ID ТЗ має бути позитивним числом.");
            if (minMileage < 0) return BadRequest("Мінімальний пробіг не може бути від'ємним.");

            var result = await _context.TripLogs
                .Where(tl => tl.VehicleId == vehicleId &&
                             tl.StartMileage != null && tl.EndMileage != null &&
                             (tl.EndMileage - tl.StartMileage) > minMileage)
                .Include(tl => tl.Driver)
                .Include(tl => tl.Vehicle)
                .Include(tl => tl.Trip)
                .Select(tl => new {
                    tl.TripLogId,
                    tl.LogDate,
                    DriverName = tl.Driver.Name,
                    VehicleLicensePlate = tl.Vehicle.LicensePlate,
                    TripRoute = tl.Trip.RouteDescription,
                    tl.StartMileage,
                    tl.EndMileage,
                    CalculatedMileage = tl.EndMileage - tl.StartMileage,
                    tl.FuelConsumedLiters,
                    tl.Notes
                })
                .ToListAsync();

            if (!result.Any())
            {
                return NotFound($"Записи журналу для ТЗ ID={vehicleId} з пробігом більше {minMileage} км не знайдені.");
            }
            return Ok(result);
        }

        // --- Запити з множинними порівняннями ---

        // Q_C1: Водії, які керували всіма ТЗ вказаного типу
        [HttpGet("drivers-all-vehicles-of-type")]
        public async Task<IActionResult> GetDriversManagingAllVehiclesOfType([FromQuery] string vehicleTypeName)
        {
            if (string.IsNullOrWhiteSpace(vehicleTypeName))
            {
                return BadRequest("Параметр 'НазваТипуТЗ' є обов'язковим.");
            }

            var targetVehicleIds = await _context.Vehicles
                .Where(v => v.VehicleType.Name == vehicleTypeName)
                .Select(v => v.VehicleId)
                .ToListAsync();

            if (!targetVehicleIds.Any())
            {
                return NotFound($"Транспортні засоби типу '{vehicleTypeName}' не знайдені.");
            }

            var result = await _context.Drivers
                .Where(d => targetVehicleIds.All(tvId => d.Assignments.Any(a => a.VehicleId == tvId)))
                // Додаткова умова, щоб водій мав хоча б одне призначення на ТЗ цього типу (щоб не повертати водіїв, якщо targetVehicleIds порожній)
                // .Where(d => d.Assignments.Any(a => targetVehicleIds.Contains(a.VehicleId))) 
                // Ця умова вже покривається .All(), якщо targetVehicleIds не порожній.
                // Якщо targetVehicleIds порожній, .All() поверне true для всіх водіїв, тому перевірка targetVehicleIds.Any() вище важлива.
                .Select(d => new { d.DriverId, d.Name, d.Email })
                .ToListAsync();

            if (!result.Any())
            {
                return NotFound($"Водії, що керували всіма ТЗ типу '{vehicleTypeName}', не знайдені.");
            }
            return Ok(result);
        }

        // Q_C2: Транспортні засоби, що проходили ТО тільки протягом вказаного року
        [HttpGet("vehicles-maintenance-only-in-year")]
        public async Task<IActionResult> GetVehiclesWithMaintenanceOnlyInYear([FromQuery] int maintenanceYear)
        {
            if (maintenanceYear < 1900 || maintenanceYear > DateTime.Now.Year + 5)
                return BadRequest("Вкажіть коректний рік для ТО.");

            var result = await _context.Vehicles
                .Where(v => v.MaintenanceRecords.Any(mr => mr.MaintenanceDate.Year == maintenanceYear) &&
                             !v.MaintenanceRecords.Any(mr => mr.MaintenanceDate.Year != maintenanceYear))
                .Include(v => v.VehicleType)
                .Select(v => new {
                    v.VehicleId,
                    v.LicensePlate,
                    v.Model,
                    v.Year,
                    VehicleTypeName = v.VehicleType.Name
                })
                .ToListAsync();

            if (!result.Any())
            {
                return NotFound($"ТЗ, що проходили ТО тільки у {maintenanceYear} році, не знайдені.");
            }
            return Ok(result);
        }

        // Q_C3: Пари водіїв, які були призначені на точно таку саму множину ТЗ
        [HttpGet("driver-pairs-same-vehicles")]
        public async Task<IActionResult> GetDriverPairsSameVehicles()
        {

            var driverAssignments = await _context.Drivers
                .Select(d => new {
                    d.DriverId,
                    d.Name,
                    AssignedVehicleIds = d.Assignments.Select(a => a.VehicleId).OrderBy(id => id).ToList()
                })
                .Where(d => d.AssignedVehicleIds.Any()) // Розглядаємо тільки водіїв з призначеннями
                .ToListAsync();

            var resultPairs = new List<object>();

            for (int i = 0; i < driverAssignments.Count; i++)
            {
                for (int j = i + 1; j < driverAssignments.Count; j++)
                {
                    var driver1 = driverAssignments[i];
                    var driver2 = driverAssignments[j];

                    // Порівнюємо відсортовані списки ID транспортних засобів
                    if (driver1.AssignedVehicleIds.SequenceEqual(driver2.AssignedVehicleIds))
                    {
                        resultPairs.Add(new
                        {
                            Driver1_Id = driver1.DriverId,
                            Driver1_Name = driver1.Name,
                            Driver2_Id = driver2.DriverId,
                            Driver2_Name = driver2.Name,
                            // Можна додати самі VehicleIds, якщо потрібно
                            // VehicleIds = driver1.AssignedVehicleIds 
                        });
                    }
                }
            }

            if (!resultPairs.Any())
            {
                return NotFound("Пари водіїв з однаковою множиною призначених ТЗ не знайдені.");
            }
            return Ok(resultPairs);
        }
    }
}