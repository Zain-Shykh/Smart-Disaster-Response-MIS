using Database_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text;
using System.ComponentModel.DataAnnotations;

namespace Database_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Administrator,Warehouse Manager,WarehouseManager")]
public class InventoryHistoryController : ControllerBase
{
    private readonly DatabaseProjectContext _context;

    public InventoryHistoryController(DatabaseProjectContext context)
    {
        _context = context;
    }

    [HttpGet("inventory/{inventoryId:int}/history")]
    public async Task<ActionResult<IEnumerable<InventoryMovementDto>>> GetInventoryHistory(
        int inventoryId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        CancellationToken cancellationToken)
    {
        if (startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value)
        {
            return BadRequest("endDate must be greater than or equal to startDate.");
        }

        if (!await _context.Inventories.AnyAsync(item => item.InventoryId == inventoryId, cancellationToken))
        {
            return NotFound($"Inventory {inventoryId} was not found.");
        }

        var movements = await BuildInventoryMovementsAsync(inventoryId, startDate, endDate, cancellationToken);
        return Ok(movements);
    }

    [HttpGet("warehouse/{warehouseId:int}/history")]
    public async Task<ActionResult<IEnumerable<WarehouseInventoryHistoryDto>>> GetWarehouseInventoryHistory(
        int warehouseId,
        CancellationToken cancellationToken)
    {
        if (!await _context.Warehouses.AnyAsync(item => item.WarehouseId == warehouseId, cancellationToken))
        {
            return NotFound($"Warehouse {warehouseId} was not found.");
        }

        var inventorySummaries = await _context.Inventories
            .AsNoTracking()
            .Where(item => item.WarehouseId == warehouseId)
            .Include(item => item.Resource)
            .Include(item => item.ResourceAllocations)
            .Select(item => new WarehouseInventoryHistoryDto
            {
                InventoryId = item.InventoryId,
                ResourceName = item.Resource.ResourceName,
                CurrentQuantity = item.Quantity,
                TotalAllocations = item.ResourceAllocations.Count,
                TotalRequestedQuantity = item.ResourceAllocations.Sum(allocation => allocation.Quantity),
                TotalDispatchedQuantity = item.ResourceAllocations
                    .Where(allocation => allocation.DispatchedAt.HasValue)
                    .Sum(allocation => allocation.Quantity),
                TotalConsumedQuantity = item.ResourceAllocations
                    .Where(allocation => allocation.ConsumedAt.HasValue)
                    .Sum(allocation => allocation.Quantity)
            })
            .OrderBy(item => item.ResourceName)
            .ToListAsync(cancellationToken);

        return Ok(inventorySummaries);
    }

    [HttpGet("inventory/{inventoryId:int}/history/export")]
    public async Task<IActionResult> ExportInventoryHistory(
        int inventoryId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string format = "csv",
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest("Only csv format is supported.");
        }

        if (startDate.HasValue && endDate.HasValue && endDate.Value < startDate.Value)
        {
            return BadRequest("endDate must be greater than or equal to startDate.");
        }

        if (!await _context.Inventories.AnyAsync(item => item.InventoryId == inventoryId, cancellationToken))
        {
            return NotFound($"Inventory {inventoryId} was not found.");
        }

        var movements = await BuildInventoryMovementsAsync(inventoryId, startDate, endDate, cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine("AllocationId,InventoryId,ResourceName,EventName,RequestedBy,MovementType,Quantity,MovementTime,Status");

        foreach (var movement in movements)
        {
            csv.AppendLine(string.Join(",",
                movement.AllocationId,
                movement.InventoryId,
                CsvEscape(movement.ResourceName),
                CsvEscape(movement.EventName),
                CsvEscape(movement.RequestedByName),
                CsvEscape(movement.MovementType),
                movement.Quantity.ToString(CultureInfo.InvariantCulture),
                movement.MovementTime.ToString("O", CultureInfo.InvariantCulture),
                CsvEscape(movement.Status)));
        }

        var fileName = $"inventory_{inventoryId}_history_{DateTime.Now:yyyyMMddHHmmss}.csv";
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", fileName);
    }

    /// <summary>
    /// Get current inventory levels from vw_Inventory_Current view.
    /// Shows warehouse inventory snapshot with quantity and thresholds.
    /// </summary>
    [HttpGet("current-stock")]
    public async Task<ActionResult<IEnumerable<InventoryCurrentDto>>> GetCurrentInventoryFromView(
        CancellationToken cancellationToken)
    {
        var result = await _context.VwInventoryCurrent
            .AsNoTracking()
            .Select(v => new InventoryCurrentDto
            {
                InventoryId = v.InventoryId,
                WarehouseId = v.WarehouseId,
                ResourceId = v.ResourceId,
                Quantity = v.Quantity,
                MinThreshold = v.MinThreshold,
                MaxCapacity = v.MaxCapacity,
                LastUpdated = v.LastUpdated
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get active inventory alerts from vw_Inventory_Alerts view.
    /// Shows low-stock and alert status notifications.
    /// </summary>
    [HttpGet("alerts")]
    public async Task<ActionResult<IEnumerable<InventoryAlertDto>>> GetInventoryAlertsFromView(
        CancellationToken cancellationToken)
    {
        var result = await _context.VwInventoryAlerts
            .AsNoTracking()
            .Select(v => new InventoryAlertDto
            {
                InventoryId = v.InventoryId,
                AlertId = v.AlertId,
                AlertType = v.AlertType,
                AlertTime = v.AlertTime,
                Status = v.Status,
                ResolvedAt = v.ResolvedAt,
                Quantity = v.Quantity,
                MinThreshold = v.MinThreshold
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get resource allocation status from vw_ResourceAllocation_Status view.
    /// Shows current state of resource allocations (requested/dispatched/consumed).
    /// </summary>
    [HttpGet("allocations/status")]
    public async Task<ActionResult<IEnumerable<ResourceAllocationStatusDto>>> GetResourceAllocationStatusFromView(
        CancellationToken cancellationToken)
    {
        var result = await _context.VwResourceAllocationStatus
            .AsNoTracking()
            .Select(v => new ResourceAllocationStatusDto
            {
                AllocationId = v.AllocationId,
                InventoryId = v.InventoryId,
                EventId = v.EventId,
                Quantity = v.Quantity,
                Status = v.Status,
                RequestedBy = v.RequestedBy,
                RequestTime = v.RequestTime,
                DispatchedAt = v.DispatchedAt,
                ConsumedAt = v.ConsumedAt
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Get inventory movement history from vw_Inventory_Movement_History view.
    /// Shows detailed movement tracking for audit purposes.
    /// </summary>
    [HttpGet("{inventoryId:int}/movement-history")]
    public async Task<ActionResult<IEnumerable<InventoryMovementDto>>> GetInventoryMovementHistoryFromView(
        int inventoryId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var allocations = await _context.ResourceAllocations
            .AsNoTracking()
            .Where(item => item.InventoryId == inventoryId)
            .Include(item => item.Inventory)
                .ThenInclude(item => item.Resource)
            .Include(item => item.Event)
            .Include(item => item.RequestedByNavigation)
            .ToListAsync(cancellationToken);

        var movementEvents = new List<InventoryMovementDto>();

        foreach (var allocation in allocations)
        {
            AddMovement(movementEvents, allocation, "Requested", allocation.RequestTime, startDate, endDate);

            if (allocation.DispatchedAt.HasValue)
            {
                AddMovement(movementEvents, allocation, "Dispatched", allocation.DispatchedAt.Value, startDate, endDate);
            }

            if (allocation.ConsumedAt.HasValue)
            {
                AddMovement(movementEvents, allocation, "Consumed", allocation.ConsumedAt.Value, startDate, endDate);
            }
        }

        return movementEvents
            .OrderByDescending(item => item.MovementTime)
            .ThenByDescending(item => item.AllocationId)
            .ToList();
    }

    private async Task<List<InventoryMovementDto>> BuildInventoryMovementsAsync(
        int inventoryId,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken)
    {
        var allocations = await _context.ResourceAllocations
            .AsNoTracking()
            .Where(item => item.InventoryId == inventoryId)
            .Include(item => item.Inventory)
                .ThenInclude(item => item.Resource)
            .Include(item => item.Event)
            .Include(item => item.RequestedByNavigation)
            .ToListAsync(cancellationToken);

        var movementEvents = new List<InventoryMovementDto>();

        foreach (var allocation in allocations)
        {
            AddMovement(movementEvents, allocation, "Requested", allocation.RequestTime, startDate, endDate);

            if (allocation.DispatchedAt.HasValue)
            {
                AddMovement(movementEvents, allocation, "Dispatched", allocation.DispatchedAt.Value, startDate, endDate);
            }

            if (allocation.ConsumedAt.HasValue)
            {
                AddMovement(movementEvents, allocation, "Consumed", allocation.ConsumedAt.Value, startDate, endDate);
            }
        }

        return movementEvents
            .OrderByDescending(item => item.MovementTime)
            .ThenByDescending(item => item.AllocationId)
            .ToList();
    }

    private static void AddMovement(
        List<InventoryMovementDto> list,
        ResourceAllocation allocation,
        string movementType,
        DateTime movementTime,
        DateTime? startDate,
        DateTime? endDate)
    {
        if (startDate.HasValue && movementTime < startDate.Value)
        {
            return;
        }

        if (endDate.HasValue && movementTime > endDate.Value)
        {
            return;
        }

        list.Add(new InventoryMovementDto
        {
            AllocationId = allocation.AllocationId,
            InventoryId = allocation.InventoryId,
            ResourceName = allocation.Inventory.Resource.ResourceName,
            EventName = allocation.Event.EventName,
            RequestedByName = $"{allocation.RequestedByNavigation.FirstName} {allocation.RequestedByNavigation.LastName}",
            MovementType = movementType,
            Quantity = allocation.Quantity,
            MovementTime = movementTime,
            Status = allocation.Status
        });
    }

    private static string CsvEscape(string input)
    {
        if (input.Contains(',') || input.Contains('"') || input.Contains('\n') || input.Contains('\r'))
        {
            return $"\"{input.Replace("\"", "\"\"")}\"";
        }

        return input;
    }

    /// <summary>
    /// Check inventory stock level using stored procedure (sp_CheckInventoryLevel).
    /// Provides ACID-compliant transaction handling at the database level.
    /// </summary>
    [HttpGet("{itemId:int}/check-level-sp")]
    public async Task<ActionResult<InventoryCheckResult>> CheckInventoryLevelStoredProc(
        int itemId,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await _context.Inventories.AnyAsync(i => i.InventoryId == itemId, cancellationToken))
            {
                return NotFound($"Inventory {itemId} was not found.");
            }

            var parameters = new[] { new SqlParameter("@InventoryID", itemId) };
            var result = await _context.ExecuteStoredProcedureScalarAsync("dbo.sp_CheckInventoryLevel", parameters);

            if (result.ContainsKey("ResultStatus"))
            {
                var resultObj = new InventoryCheckResult
                {
                    ResultStatus = result["ResultStatus"]?.ToString() ?? "Unknown",
                    CurrentQuantity = result.ContainsKey("CurrentQuantity") ? (decimal)(result["CurrentQuantity"] ?? 0m) : 0m,
                    Status = result.ContainsKey("Status") ? result["Status"]?.ToString() ?? "Unknown" : "Unknown"
                };
                return Ok(resultObj);
            }

            return StatusCode(500, "Stored procedure did not return expected result.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, details = ex.InnerException?.Message });
        }
    }

    /// <summary>
    /// Update inventory stock using stored procedure (sp_UpdateInventoryStock).
    /// Provides ACID-compliant transaction handling at the database level.
    /// </summary>
    [HttpPost("{itemId:int}/update-stock-sp")]
    public async Task<ActionResult<InventoryUpdateResult>> UpdateInventoryStockStoredProc(
        int itemId,
        [FromBody] InventoryUpdateSpRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await _context.Inventories.AnyAsync(i => i.InventoryId == itemId, cancellationToken))
            {
                return NotFound($"Inventory {itemId} was not found.");
            }

            if (!await _context.Users.AnyAsync(u => u.UserId == request.UpdatedBy, cancellationToken))
            {
                return NotFound($"User {request.UpdatedBy} was not found.");
            }

            var parameters = new[]
            {
                new SqlParameter("@InventoryID", itemId),
                new SqlParameter("@Quantity", request.Quantity),
                new SqlParameter("@TransactionType", request.TransactionType),
                new SqlParameter("@UpdatedBy", request.UpdatedBy),
                new SqlParameter("@Reason", (object?)request.Reason ?? DBNull.Value)
            };

            var result = await _context.ExecuteStoredProcedureScalarAsync("dbo.sp_UpdateInventoryStock", parameters);

            if (result.ContainsKey("ResultStatus"))
            {
                var resultObj = new InventoryUpdateResult
                {
                    ResultStatus = result["ResultStatus"]?.ToString() ?? "Unknown",
                    NewQuantity = result.ContainsKey("NewQuantity") ? (decimal)(result["NewQuantity"] ?? 0m) : 0m
                };
                return Ok(resultObj);
            }

            return StatusCode(500, "Stored procedure did not return expected result.");
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, details = ex.InnerException?.Message });
        }
    }
}

public class InventoryUpdateSpRequestDto
{
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Required]
    [MaxLength(20)]
    public string TransactionType { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int UpdatedBy { get; set; }

    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class InventoryCheckResult
{
    public string ResultStatus { get; set; } = string.Empty;
    public decimal CurrentQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class InventoryUpdateResult
{
    public string ResultStatus { get; set; } = string.Empty;
    public decimal NewQuantity { get; set; }
}
