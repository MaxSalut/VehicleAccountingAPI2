using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VehicleAccountingAPI.Data;
using VehicleAccountingAPI.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; 

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
        /*
Знайти всіх водіїв, які були призначені на транспортні засоби, що належать до конкретного типу.
DECLARE @НазваТипуТЗ_S1 NVARCHAR(50) = N'Легковий автомобіль'; 
SELECT DISTINCT D.DriverId, D.Name, D.Email, D.LicenseNumber
FROM dbo.Drivers D
INNER JOIN dbo.Assignments A ON D.DriverId = A.DriverId
INNER JOIN dbo.Vehicles V ON A.VehicleId = V.VehicleId
INNER JOIN dbo.VehicleTypes VT ON V.VehicleTypeId = VT.VehicleTypeId
WHERE VT.Name = @НазваТипуТЗ_S1;
        */
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
        /*
         Вивести інформацію про ТЗ, які проходили ТО після вказаної дати, і хоча б один запис ТО для них мав вартість, що перевищує задану суму.
DECLARE @МінімальнаВартістьТО_S2 DECIMAL(10,2) = 1000.00;
DECLARE @ДатаПісляЯкоїПроводилосьТО_S2 DATE = '2024-01-01';
SELECT DISTINCT V.VehicleId, V.LicensePlate, V.Model, V.Year, VT.Name AS VehicleTypeName
FROM dbo.Vehicles V
INNER JOIN dbo.VehicleTypes VT ON V.VehicleTypeId = VT.VehicleTypeId
WHERE V.VehicleId IN (
    SELECT MR.VehicleId
    FROM dbo.MaintenanceRecords MR
    WHERE MR.MaintenanceDate > @ДатаПісляЯкоїПроводилосьТО_S2
      AND MR.Cost > @МінімальнаВартістьТО_S2
);
        */
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

        /*
         Знайти всі поїздки (Trips), в яких брав участь конкретний водій, і які фактично розпочалися у вказаний період.
Параметри: @ID_Водія_S3 (INT), @ДатаПочаткуПеріоду_S3 (DATETIME), @ДатаКінцяПеріоду_S3 (DATETIME)

DECLARE @ID_Водія_S3 INT = 3; -- Приклад ID водія
DECLARE @ДатаПочаткуПеріоду_S3 DATETIME = '2025-01-01T00:00:00';
DECLARE @ДатаКінцяПеріоду_S3 DATETIME = '2025-05-31T23:59:59';

SELECT T.TripId, T.RouteDescription, T.CargoDescription, T.PlannedStartDateTime, T.ActualStartDateTime, T.Status, D.Name AS DriverName
FROM dbo.Trips T
INNER JOIN dbo.TripLogs TL ON T.TripId = TL.TripId
INNER JOIN dbo.Drivers D ON TL.DriverId = D.DriverId -- Додано для відображення імені водія
WHERE TL.DriverId = @ID_Водія_S3
  AND T.ActualStartDateTime >= @ДатаПочаткуПеріоду_S3
  AND T.ActualStartDateTime <= @ДатаКінцяПеріоду_S3;
         */
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
                    
                })
                .ToListAsync();

            if (!result.Any())
            {
                return NotFound($"Поїздки для водія ID={driverId} у вказаний період не знайдені.");
            }
            return Ok(result);
        }

        // Q_S4: Активно призначені ТЗ старші за вказаний рік
        /*Вивести ТЗ, які мають активне призначення (дата завершення відсутня або в майбутньому) і рік випуску яких менший за вказаний.
Параметр: @РікВипускуДо_S4 (INT)

DECLARE @РікВипускуДо_S4 INT = 2022; -- Покаже ТЗ до 2021 року включно

SELECT DISTINCT V.VehicleId, V.LicensePlate, V.Model, V.Year, VT.Name AS VehicleTypeName
FROM dbo.Vehicles V
INNER JOIN dbo.VehicleTypes VT ON V.VehicleTypeId = VT.VehicleTypeId
INNER JOIN dbo.Assignments A ON V.VehicleId = A.VehicleId
WHERE V.Year < @РікВипускуДо_S4
  AND (A.EndDate IS NULL OR A.EndDate > GETDATE());*/
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
        /*
        Вивести записи TripLogs для конкретного ТЗ, де зафіксований пробіг (різниця між кінцевим та початковим) перевищив X км.
Параметри: @ID_ТЗ_S5 (INT), @МінімальнийПробіг_S5 (INT)

DECLARE @ID_ТЗ_S5 INT = 1; -- Приклад ID ТЗ
DECLARE @МінімальнийПробіг_S5 INT = 100;

SELECT TL.TripLogId, TL.LogDate, D.Name AS DriverName, V.LicensePlate, T.RouteDescription, 
       TL.StartMileage, TL.EndMileage, (TL.EndMileage - TL.StartMileage) AS CalculatedMileage,
       TL.FuelConsumedLiters, TL.Notes
FROM dbo.TripLogs TL
INNER JOIN dbo.Drivers D ON TL.DriverId = D.DriverId
INNER JOIN dbo.Vehicles V ON TL.VehicleId = V.VehicleId
INNER JOIN dbo.Trips T ON TL.TripId = T.TripId
WHERE TL.VehicleId = @ID_ТЗ_S5
  AND TL.StartMileage IS NOT NULL AND TL.EndMileage IS NOT NULL
  AND (TL.EndMileage - TL.StartMileage) > @МінімальнийПробіг_S5;
        */
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

        /*Знайти водіїв, які мали призначення на кожен транспортний засіб, що належить до певного типу.

DECLARE @НазваТипуТЗ_C1 NVARCHAR(50) = N'Вантажівка'; -- Приклад параметра

WITH TargetVehicleType AS (
    SELECT vt.VehicleTypeId 
    FROM dbo.VehicleTypes vt 
    WHERE vt.Name = @НазваТипуТЗ_C1
),
TargetVehicles AS (
    SELECT v.VehicleId
    FROM dbo.Vehicles v
    WHERE v.VehicleTypeId IN (SELECT tvt.VehicleTypeId FROM TargetVehicleType tvt)
),
DriverAssignedToTargetVehicles AS (
    SELECT DISTINCT a.DriverId, a.VehicleId
    FROM dbo.Assignments a
    WHERE a.VehicleId IN (SELECT tv.VehicleId FROM TargetVehicles tv)
)
SELECT d.DriverId, d.Name, d.Email
FROM dbo.Drivers d
WHERE 
    -- Перевіряємо, чи взагалі існують ТЗ цільового типу. Якщо ні, то жоден водій не міг ними керувати.
    (SELECT COUNT(*) FROM TargetVehicles) > 0 
    AND
    -- Кількість унікальних ТЗ цільового типу, на які призначений водій,
    -- має дорівнювати загальній кількості ТЗ цільового типу.
    (SELECT COUNT(DISTINCT datv.VehicleId) 
     FROM DriverAssignedToTargetVehicles datv 
     WHERE datv.DriverId = d.DriverId
    ) = (SELECT COUNT(*) FROM TargetVehicles);*/
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
                .Select(d => new { d.DriverId, d.Name, d.Email })
                .ToListAsync();

            if (!result.Any())
            {
                return NotFound($"Водії, що керували всіма ТЗ типу '{vehicleTypeName}', не знайдені.");
            }
            return Ok(result);
        }

        // Q_C2: Транспортні засоби, що проходили ТО тільки протягом вказаного року

        /*
        Знайти ТЗ, всі записи ТО яких припадають виключно на вказаний рік.
Параметр: @РікТО_C2 (INT)

DECLARE @РікТО_C2 INT = 2024; -- Приклад параметра

SELECT V.VehicleId, V.LicensePlate, V.Model, VT.Name AS VehicleTypeName
FROM dbo.Vehicles V
INNER JOIN dbo.VehicleTypes VT ON V.VehicleTypeId = VT.VehicleTypeId
WHERE
    EXISTS ( -- Має хоча б одне ТО у вказаному році
        SELECT 1
        FROM dbo.MaintenanceRecords MR
        WHERE MR.VehicleId = V.VehicleId AND YEAR(MR.MaintenanceDate) = @РікТО_C2
    )
    AND NOT EXISTS ( -- І не має жодного ТО в будь-якому іншому році
        SELECT 1
        FROM dbo.MaintenanceRecords MR
        WHERE MR.VehicleId = V.VehicleId AND YEAR(MR.MaintenanceDate) != @РікТО_C2
    );
         */
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

        /*
        Знайти унікальні пари водіїв, які були призначені на абсолютно однаковий набір ТЗ.
Параметр: Немає.
WITH DriverVehicleAssignments AS (
    -- Отримуємо унікальні пари (Водій, ТЗ) з призначень
    SELECT DISTINCT DriverId, VehicleId
    FROM dbo.Assignments
)
SELECT 
    D1.DriverId AS Driver1_Id, 
    DR1.Name AS Driver1_Name,
    D2.DriverId AS Driver2_Id, 
    DR2.Name AS Driver2_Name
FROM dbo.Drivers DR1
INNER JOIN DriverVehicleAssignments DVA1_Agg ON DR1.DriverId = DVA1_Agg.DriverId -- Допоміжна для агрегації
INNER JOIN (
    SELECT DriverId, COUNT(VehicleId) AS NumVehicles,
           STRING_AGG(CAST(VehicleId AS VARCHAR(10)), ',') WITHIN GROUP (ORDER BY VehicleId) AS VehicleSet
    FROM DriverVehicleAssignments
    GROUP BY DriverId
) D1 ON DR1.DriverId = D1.DriverId
INNER JOIN dbo.Drivers DR2 ON DR1.DriverId < DR2.DriverId -- Унікальні пари, D1.ID < D2.ID
INNER JOIN (
    SELECT DriverId, COUNT(VehicleId) AS NumVehicles,
           STRING_AGG(CAST(VehicleId AS VARCHAR(10)), ',') WITHIN GROUP (ORDER BY VehicleId) AS VehicleSet
    FROM DriverVehicleAssignments
    GROUP BY DriverId
) D2 ON DR2.DriverId = D2.DriverId
WHERE D1.NumVehicles > 0 -- Переконуємося, що водії мають хоча б одне призначення
  AND D1.NumVehicles = D2.NumVehicles -- Кількість ТЗ однакова
  AND D1.VehicleSet = D2.VehicleSet; -- І набори ТЗ (представлені як рядки) однакові

*/
        [HttpGet("driver-pairs-same-vehicles")]
        public async Task<IActionResult> GetDriverPairsSameVehicles()
        {

            var driverAssignments = await _context.Drivers
                .Select(d => new {
                    d.DriverId,
                    d.Name,
                    AssignedVehicleIds = d.Assignments.Select(a => a.VehicleId).OrderBy(id => id).ToList()
                })
                .Where(d => d.AssignedVehicleIds.Any())
                .ToListAsync();

            var resultPairs = new List<object>();

            for (int i = 0; i < driverAssignments.Count; i++)
            {
                for (int j = i + 1; j < driverAssignments.Count; j++)
                {
                    var driver1 = driverAssignments[i];
                    var driver2 = driverAssignments[j];

                    if (driver1.AssignedVehicleIds.SequenceEqual(driver2.AssignedVehicleIds))
                    {
                        resultPairs.Add(new
                        {
                            Driver1_Id = driver1.DriverId,
                            Driver1_Name = driver1.Name,
                            Driver2_Id = driver2.DriverId,
                            Driver2_Name = driver2.Name,
                            VehicleIds = driver1.AssignedVehicleIds 
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