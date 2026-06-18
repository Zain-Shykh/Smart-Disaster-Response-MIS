using Database_Backend.Models;
using Database_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.ComponentModel.DataAnnotations;

namespace Database_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Administrator,EmergencyOperator,FieldOfficer,Field Officer,WarehouseManager,Warehouse Manager")]
public class ResourceLogisticsController : ControllerBase
{
    private static readonly HashSet<string> AllowedResourceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Food",
        "Water",
        "Medicine",
        "Shelter"
    };

    private static readonly HashSet<string> AllowedAllocationStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending",
        "Approved",
        "Dispatched",
        "Consumed",
        "Rejected"
    };

    private static readonly HashSet<string> AllowedAlertStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Active",
        "Resolved"
    };

    private readonly DatabaseProjectContext _context;

    public ResourceLogisticsController(DatabaseProjectContext context)
    {
        _context = context;
    }

    [HttpGet("resources")]
    public async Task<ActionResult<IEnumerable<ResourceDto>>> GetResources([FromQuery] string? resourceType, CancellationToken cancellationToken)
    {
        IQueryable<Resource> resources = _context.Resources.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(resourceType))
        {
            var normalizedType = NormalizeResourceType(resourceType);
            if (normalizedType is null)
            {
                return BadRequest("ResourceType must be one of: Food, Water, Medicine, Shelter.");
            }

            resources = resources.Where(item => item.ResourceType == normalizedType);
        }

        var result = await resources
            .OrderBy(item => item.ResourceId)
            .Select(item => new ResourceDto
            {
                ResourceId = item.ResourceId,
                ResourceName = item.ResourceName,
                ResourceType = item.ResourceType,
                Unit = item.Unit,
                Description = item.Description
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost("resources")]
    public async Task<ActionResult<ResourceDto>> CreateResource([FromBody] ResourceCreateDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ResourceName) || string.IsNullOrWhiteSpace(request.Unit))
        {
            return BadRequest("ResourceName and Unit are required.");
        }

        var resourceType = NormalizeResourceType(request.ResourceType);
        if (resourceType is null)
        {
            return BadRequest("ResourceType must be one of: Food, Water, Medicine, Shelter.");
        }

        var resource = new Resource
        {
            ResourceName = request.ResourceName.Trim(),
            ResourceType = resourceType,
            Unit = request.Unit.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim()
        };

        _context.Resources.Add(resource);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetResources), new { resourceType = resource.ResourceType }, new ResourceDto
        {
            ResourceId = resource.ResourceId,
            ResourceName = resource.ResourceName,
            ResourceType = resource.ResourceType,
            Unit = resource.Unit,
            Description = resource.Description
        });
    }

    [HttpGet("warehouses")]
    public async Task<ActionResult<IEnumerable<WarehouseDto>>> GetWarehouses([FromQuery] string? city, CancellationToken cancellationToken)
    {
        IQueryable<Warehouse> warehouses = _context.Warehouses.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(city))
        {
            var cityValue = city.Trim();
            warehouses = warehouses.Where(item => item.City == cityValue);
        }

        var result = await warehouses
            .OrderBy(item => item.WarehouseId)
            .Select(item => new WarehouseDto
            {
                WarehouseId = item.WarehouseId,
                WarehouseName = item.WarehouseName,
                Street = item.Street,
                Area = item.Area,
                City = item.City,
                Province = item.Province,
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                Capacity = item.Capacity,
                ManagerId = item.ManagerId,
                ContactPhone = item.ContactPhone,
                ContactEmail = item.ContactEmail
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost("warehouses")]
    public async Task<ActionResult<WarehouseDto>> CreateWarehouse([FromBody] WarehouseCreateDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.WarehouseName) || string.IsNullOrWhiteSpace(request.Street) || string.IsNullOrWhiteSpace(request.Area) || string.IsNullOrWhiteSpace(request.City) || string.IsNullOrWhiteSpace(request.Province))
        {
            return BadRequest("WarehouseName, Street, Area, City, and Province are required.");
        }

        if (request.Capacity < 0)
        {
            return BadRequest("Capacity cannot be negative.");
        }

        if (!await _context.Users.AnyAsync(user => user.UserId == request.ManagerId, cancellationToken))
        {
            return NotFound($"Manager user {request.ManagerId} was not found.");
        }

        var warehouse = new Warehouse
        {
            WarehouseName = request.WarehouseName.Trim(),
            Street = request.Street.Trim(),
            Area = request.Area.Trim(),
            City = request.City.Trim(),
            Province = request.Province.Trim(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            Capacity = request.Capacity,
            ManagerId = request.ManagerId,
            ContactPhone = string.IsNullOrWhiteSpace(request.ContactPhone) ? null : request.ContactPhone.Trim(),
            ContactEmail = string.IsNullOrWhiteSpace(request.ContactEmail) ? null : request.ContactEmail.Trim()
        };

        _context.Warehouses.Add(warehouse);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetWarehouses), new { city = warehouse.City }, MapWarehouse(warehouse));
    }

    [HttpGet("inventories")]
    public async Task<ActionResult<IEnumerable<InventoryDto>>> GetInventories(
        [FromQuery] int? warehouseId,
        [FromQuery] int? resourceId,
        [FromQuery] bool? lowStockOnly,
        CancellationToken cancellationToken)
    {
        IQueryable<Inventory> inventories = _context.Inventories
            .AsNoTracking()
            .Include(item => item.Warehouse)
            .Include(item => item.Resource);

        if (warehouseId.HasValue)
        {
            inventories = inventories.Where(item => item.WarehouseId == warehouseId.Value);
        }

        if (resourceId.HasValue)
        {
            inventories = inventories.Where(item => item.ResourceId == resourceId.Value);
        }

        if (lowStockOnly == true)
        {
            inventories = inventories.Where(item => item.Quantity <= item.MinThreshold);
        }

        var result = await inventories
            .OrderBy(item => item.WarehouseId)
            .ThenBy(item => item.ResourceId)
            .Select(item => new InventoryDto
            {
                InventoryId = item.InventoryId,
                WarehouseId = item.WarehouseId,
                WarehouseName = item.Warehouse.WarehouseName,
                ResourceId = item.ResourceId,
                ResourceName = item.Resource.ResourceName,
                ResourceType = item.Resource.ResourceType,
                Quantity = item.Quantity,
                MinThreshold = item.MinThreshold,
                MaxCapacity = item.MaxCapacity,
                LastUpdated = item.LastUpdated
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost("inventories")]
    public async Task<ActionResult<InventoryDto>> CreateInventory([FromBody] InventoryCreateDto request, CancellationToken cancellationToken)
    {
        if (request.Quantity < 0 || request.MinThreshold < 0 || request.MaxCapacity < 0)
        {
            return BadRequest("Quantity, MinThreshold, and MaxCapacity cannot be negative.");
        }

        if (request.Quantity > request.MaxCapacity)
        {
            return BadRequest("Quantity cannot be greater than MaxCapacity.");
        }

        if (!await _context.Warehouses.AnyAsync(item => item.WarehouseId == request.WarehouseId, cancellationToken))
        {
            return NotFound($"Warehouse {request.WarehouseId} was not found.");
        }

        if (!await _context.Resources.AnyAsync(item => item.ResourceId == request.ResourceId, cancellationToken))
        {
            return NotFound($"Resource {request.ResourceId} was not found.");
        }

        if (await _context.Inventories.AnyAsync(item => item.WarehouseId == request.WarehouseId && item.ResourceId == request.ResourceId, cancellationToken))
        {
            return Conflict("Inventory already exists for this warehouse-resource pair.");
        }

        var inventory = new Inventory
        {
            WarehouseId = request.WarehouseId,
            ResourceId = request.ResourceId,
            Quantity = request.Quantity,
            MinThreshold = request.MinThreshold,
            MaxCapacity = request.MaxCapacity,
            LastUpdated = DateTime.Now
        };

        _context.Inventories.Add(inventory);
        await _context.SaveChangesAsync(cancellationToken);

        var created = await _context.Inventories
            .AsNoTracking()
            .Include(item => item.Warehouse)
            .Include(item => item.Resource)
            .FirstAsync(item => item.InventoryId == inventory.InventoryId, cancellationToken);

        return CreatedAtAction(nameof(GetInventories), new { warehouseId = created.WarehouseId }, MapInventory(created));
    }

    [HttpPatch("inventories/{inventoryId:int}")]
    public async Task<ActionResult<InventoryDto>> UpdateInventoryLevels(
        int inventoryId,
        [FromBody] InventoryUpdateDto request,
        CancellationToken cancellationToken)
    {
        var inventory = await _context.Inventories.FirstOrDefaultAsync(item => item.InventoryId == inventoryId, cancellationToken);
        if (inventory is null)
        {
            return NotFound();
        }

        var currentToken = ComputeInventoryVersionToken(inventory);
        if (!string.IsNullOrWhiteSpace(request.VersionToken) && !string.Equals(request.VersionToken, currentToken, StringComparison.Ordinal))
        {
            return Conflict(new ResourceLogisticsConcurrencyConflictDto
            {
                Message = "Inventory was modified by another operation.",
                CurrentVersionToken = currentToken
            });
        }

        if (request.Quantity.HasValue)
        {
            if (request.Quantity.Value < 0)
            {
                return BadRequest("Quantity cannot be negative.");
            }

            inventory.Quantity = request.Quantity.Value;
        }

        if (request.MinThreshold.HasValue)
        {
            if (request.MinThreshold.Value < 0)
            {
                return BadRequest("MinThreshold cannot be negative.");
            }

            inventory.MinThreshold = request.MinThreshold.Value;
        }

        if (request.MaxCapacity.HasValue)
        {
            if (request.MaxCapacity.Value < 0)
            {
                return BadRequest("MaxCapacity cannot be negative.");
            }

            inventory.MaxCapacity = request.MaxCapacity.Value;
        }

        if (inventory.Quantity > inventory.MaxCapacity)
        {
            return BadRequest("Quantity cannot be greater than MaxCapacity.");
        }

        inventory.LastUpdated = DateTime.Now;
        await _context.SaveChangesAsync(cancellationToken);

        var updated = await _context.Inventories
            .AsNoTracking()
            .Include(item => item.Warehouse)
            .Include(item => item.Resource)
            .FirstAsync(item => item.InventoryId == inventoryId, cancellationToken);

        return Ok(MapInventory(updated));
    }

    [HttpGet("alerts")]
    public async Task<ActionResult<IEnumerable<InventoryAlertDto>>> GetInventoryAlerts(
        [FromQuery] int? inventoryId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        IQueryable<InventoryAlert> alerts = _context.InventoryAlerts
            .AsNoTracking()
            .Include(item => item.Inventory)
            .ThenInclude(item => item.Resource)
            .Include(item => item.Inventory)
            .ThenInclude(item => item.Warehouse);

        if (inventoryId.HasValue)
        {
            alerts = alerts.Where(item => item.InventoryId == inventoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = NormalizeAlertStatus(status);
            if (normalizedStatus is null)
            {
                return BadRequest("Alert status must be one of: Active, Resolved.");
            }

            alerts = alerts.Where(item => item.Status == normalizedStatus);
        }

        var result = await alerts
            .OrderByDescending(item => item.AlertTime)
            .Select(item => new InventoryAlertDto
            {
                InventoryId = item.InventoryId,
                AlertId = item.AlertId,
                AlertType = item.AlertType,
                AlertTime = item.AlertTime,
                Status = item.Status,
                ResolvedAt = item.ResolvedAt,
                ResourceName = item.Inventory.Resource.ResourceName,
                WarehouseName = item.Inventory.Warehouse.WarehouseName
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("allocations")]
    public async Task<ActionResult<IEnumerable<ResourceAllocationDto>>> GetAllocations(
        [FromQuery] int? eventId,
        [FromQuery] int? inventoryId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        IQueryable<ResourceAllocation> allocations = _context.ResourceAllocations
            .AsNoTracking()
            .Include(item => item.Event)
            .Include(item => item.Inventory)
            .ThenInclude(item => item.Resource)
            .Include(item => item.Inventory)
            .ThenInclude(item => item.Warehouse)
            .Include(item => item.RequestedByNavigation);

        if (eventId.HasValue)
        {
            allocations = allocations.Where(item => item.EventId == eventId.Value);
        }

        if (inventoryId.HasValue)
        {
            allocations = allocations.Where(item => item.InventoryId == inventoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalizedStatus = NormalizeAllocationStatus(status);
            if (normalizedStatus is null)
            {
                return BadRequest("Allocation status must be one of: Pending, Approved, Dispatched, Consumed, Rejected.");
            }

            allocations = allocations.Where(item => item.Status == normalizedStatus);
        }

        var result = await allocations
            .OrderByDescending(item => item.RequestTime)
            .Select(item => MapAllocation(item))
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpPost("allocations")]
    public async Task<ActionResult<ResourceAllocationDto>> CreateAllocation(
        [FromBody] ResourceAllocationCreateDto request,
        CancellationToken cancellationToken)
    {
        if (request.Quantity <= 0)
        {
            return BadRequest("Quantity must be greater than zero.");
        }

        var status = NormalizeAllocationStatus(request.Status);
        if (status is null)
        {
            return BadRequest("Allocation status must be one of: Pending, Approved, Dispatched, Consumed, Rejected.");
        }

        if (!await _context.Inventories.AnyAsync(item => item.InventoryId == request.InventoryId, cancellationToken))
        {
            return NotFound($"Inventory {request.InventoryId} was not found.");
        }

        if (!await _context.DisasterEvents.AnyAsync(item => item.EventId == request.EventId, cancellationToken))
        {
            return NotFound($"Disaster event {request.EventId} was not found.");
        }

        if (!await _context.Users.AnyAsync(item => item.UserId == request.RequestedBy, cancellationToken))
        {
            return NotFound($"User {request.RequestedBy} was not found.");
        }

        var approvalRequestedBy = request.ApprovalRequestedBy ?? request.RequestedBy;
        if (request.RequiresApproval && !await _context.Users.AnyAsync(item => item.UserId == approvalRequestedBy, cancellationToken))
        {
            return NotFound($"Approval requested-by user {approvalRequestedBy} was not found.");
        }

        if (request.RequiresApproval && !status.Equals("Pending", StringComparison.OrdinalIgnoreCase))
        {
            status = "Pending";
        }

        var allocation = new ResourceAllocation
        {
            InventoryId = request.InventoryId,
            EventId = request.EventId,
            RequestedBy = request.RequestedBy,
            Quantity = request.Quantity,
            RequestTime = request.RequestTime ?? DateTime.Now,
            Status = status,
            DispatchedAt = request.DispatchedAt,
            ConsumedAt = request.ConsumedAt
        };

        _context.ResourceAllocations.Add(allocation);

        IDbContextTransaction? transaction = null;
        try
        {
            if (!string.Equals(_context.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
            {
                transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            }

            await _context.SaveChangesAsync(cancellationToken);

            if (request.RequiresApproval)
            {
                var approvalRequest = new ApprovalRequest
                {
                    RequestedBy = approvalRequestedBy,
                    RequestType = "ResourceDistribution",
                    RequestTime = DateTime.Now,
                    Status = "Pending",
                    Description = $"Approval required for allocation {allocation.AllocationId}.",
                    AllocationId = allocation.AllocationId
                };

                _context.ApprovalRequests.Add(approvalRequest);
                await _context.SaveChangesAsync(cancellationToken);
            }

            if (transaction is not null)
            {
                await transaction.CommitAsync(cancellationToken);
            }
        }
        catch (DbUpdateException)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }

            return Conflict("The allocation could not be saved. This may be due to trigger-based inventory validation.");
        }
        finally
        {
            if (transaction is not null)
            {
                await transaction.DisposeAsync();
            }
        }

        var created = await _context.ResourceAllocations
            .AsNoTracking()
            .Include(item => item.Event)
            .Include(item => item.Inventory)
            .ThenInclude(item => item.Resource)
            .Include(item => item.Inventory)
            .ThenInclude(item => item.Warehouse)
            .Include(item => item.RequestedByNavigation)
            .FirstAsync(item => item.AllocationId == allocation.AllocationId, cancellationToken);

        return CreatedAtAction(nameof(GetAllocations), new { eventId = created.EventId }, MapAllocation(created));
    }

    [HttpPatch("allocations/{allocationId:int}/status")]
    public async Task<ActionResult<ResourceAllocationDto>> UpdateAllocationStatus(
        int allocationId,
        [FromBody] ResourceAllocationStatusUpdateDto request,
        CancellationToken cancellationToken)
    {
        var allocation = await _context.ResourceAllocations.FirstOrDefaultAsync(item => item.AllocationId == allocationId, cancellationToken);
        if (allocation is null)
        {
            return NotFound();
        }

        var status = NormalizeAllocationStatus(request.Status);
        if (status is null)
        {
            return BadRequest("Allocation status must be one of: Pending, Approved, Dispatched, Consumed, Rejected.");
        }

        if ((status.Equals("Dispatched", StringComparison.OrdinalIgnoreCase) || status.Equals("Consumed", StringComparison.OrdinalIgnoreCase))
            && !await HasApprovedAllocationRequestAsync(allocationId, cancellationToken))
        {
            return BadRequest("Allocation cannot be dispatched/consumed until an approval request is approved.");
        }

        allocation.Status = status;

        if (status.Equals("Dispatched", StringComparison.OrdinalIgnoreCase) && allocation.DispatchedAt is null)
        {
            allocation.DispatchedAt = DateTime.Now;
        }

        if (status.Equals("Consumed", StringComparison.OrdinalIgnoreCase) && allocation.ConsumedAt is null)
        {
            allocation.ConsumedAt = DateTime.Now;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Conflict("The allocation status could not be updated. This may be due to trigger-based inventory validation.");
        }

        var updated = await _context.ResourceAllocations
            .AsNoTracking()
            .Include(item => item.Event)
            .Include(item => item.Inventory)
            .ThenInclude(item => item.Resource)
            .Include(item => item.Inventory)
            .ThenInclude(item => item.Warehouse)
            .Include(item => item.RequestedByNavigation)
            .FirstAsync(item => item.AllocationId == allocationId, cancellationToken);

        return Ok(MapAllocation(updated));
    }

    private async Task<bool> HasApprovedAllocationRequestAsync(int allocationId, CancellationToken cancellationToken)
    {
        return await _context.ApprovalRequests
            .AsNoTracking()
            .AnyAsync(item => item.AllocationId == allocationId
                && item.RequestType == "ResourceDistribution"
                && item.Status == "Approved", cancellationToken);
    }

    private static string? NormalizeResourceType(string? resourceType)
    {
        if (string.IsNullOrWhiteSpace(resourceType) || !AllowedResourceTypes.Contains(resourceType.Trim()))
        {
            return null;
        }

        return AllowedResourceTypes.First(item => item.Equals(resourceType.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeAllocationStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || !AllowedAllocationStatuses.Contains(status.Trim()))
        {
            return null;
        }

        return AllowedAllocationStatuses.First(item => item.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static string? NormalizeAlertStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || !AllowedAlertStatuses.Contains(status.Trim()))
        {
            return null;
        }

        return AllowedAlertStatuses.First(item => item.Equals(status.Trim(), StringComparison.OrdinalIgnoreCase));
    }

    private static WarehouseDto MapWarehouse(Warehouse warehouse)
    {
        return new WarehouseDto
        {
            WarehouseId = warehouse.WarehouseId,
            WarehouseName = warehouse.WarehouseName,
            Street = warehouse.Street,
            Area = warehouse.Area,
            City = warehouse.City,
            Province = warehouse.Province,
            Latitude = warehouse.Latitude,
            Longitude = warehouse.Longitude,
            Capacity = warehouse.Capacity,
            ManagerId = warehouse.ManagerId,
            ContactPhone = warehouse.ContactPhone,
            ContactEmail = warehouse.ContactEmail
        };
    }

    private static InventoryDto MapInventory(Inventory inventory)
    {
        return new InventoryDto
        {
            InventoryId = inventory.InventoryId,
            WarehouseId = inventory.WarehouseId,
            WarehouseName = inventory.Warehouse.WarehouseName,
            ResourceId = inventory.ResourceId,
            ResourceName = inventory.Resource.ResourceName,
            ResourceType = inventory.Resource.ResourceType,
            Quantity = inventory.Quantity,
            MinThreshold = inventory.MinThreshold,
            MaxCapacity = inventory.MaxCapacity,
            LastUpdated = inventory.LastUpdated,
            VersionToken = ComputeInventoryVersionToken(inventory)
        };
    }

    private static ResourceAllocationDto MapAllocation(ResourceAllocation allocation)
    {
        return new ResourceAllocationDto
        {
            AllocationId = allocation.AllocationId,
            InventoryId = allocation.InventoryId,
            EventId = allocation.EventId,
            EventName = allocation.Event.EventName,
            RequestedBy = allocation.RequestedBy,
            RequestedByName = $"{allocation.RequestedByNavigation.FirstName} {allocation.RequestedByNavigation.LastName}",
            Quantity = allocation.Quantity,
            RequestTime = allocation.RequestTime,
            Status = allocation.Status,
            DispatchedAt = allocation.DispatchedAt,
            ConsumedAt = allocation.ConsumedAt,
            ResourceName = allocation.Inventory.Resource.ResourceName,
            WarehouseName = allocation.Inventory.Warehouse.WarehouseName,
            VersionToken = ComputeAllocationVersionToken(allocation)
        };
    }

    private static string ComputeInventoryVersionToken(Inventory inventory)
    {
        return ConcurrencyTokenService.Compute(
            inventory.InventoryId,
            inventory.WarehouseId,
            inventory.ResourceId,
            inventory.Quantity,
            inventory.MinThreshold,
            inventory.MaxCapacity,
            inventory.LastUpdated);
    }

    private static string ComputeAllocationVersionToken(ResourceAllocation allocation)
    {
        return ConcurrencyTokenService.Compute(
            allocation.AllocationId,
            allocation.InventoryId,
            allocation.EventId,
            allocation.RequestedBy,
            allocation.Quantity,
            allocation.RequestTime,
            allocation.Status,
            allocation.DispatchedAt,
            allocation.ConsumedAt);
    }

    /// <summary>
    /// Approve an allocation using stored procedure (sp_ApproveAllocation).
    /// Provides ACID-compliant transaction handling at the database level.
    /// </summary>
    [HttpPost("allocations/{allocationId:int}/approve-sp")]
    public async Task<ActionResult<AllocationApprovalResult>> ApproveAllocationStoredProc(
        int allocationId,
        [FromBody] ApprovalDecisionDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await _context.Users.AnyAsync(item => item.UserId == request.ActionBy, cancellationToken))
            {
                return NotFound($"ActionBy user {request.ActionBy} was not found.");
            }

            var parameters = new[]
            {
                new SqlParameter("@AllocationID", allocationId),
                new SqlParameter("@ReviewedBy", request.ActionBy),
                new SqlParameter("@Comments", (object?)request.Comments ?? DBNull.Value)
            };

            var result = await _context.ExecuteStoredProcedureScalarAsync("dbo.sp_ApproveAllocation", parameters);

            if (result.ContainsKey("ResultStatus") && result.ContainsKey("AllocationID") && result.ContainsKey("RequestID"))
            {
                var resultObj = new AllocationApprovalResult
                {
                    ResultStatus = result["ResultStatus"]?.ToString() ?? "Unknown",
                    AllocationID = (int)(result["AllocationID"] ?? 0),
                    RequestID = (int)(result["RequestID"] ?? 0)
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
    /// Reject an allocation using stored procedure (sp_RejectAllocation).
    /// Provides ACID-compliant transaction handling at the database level.
    /// </summary>
    [HttpPost("allocations/{allocationId:int}/reject-sp")]
    public async Task<ActionResult<AllocationApprovalResult>> RejectAllocationStoredProc(
        int allocationId,
        [FromBody] ApprovalDecisionDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await _context.Users.AnyAsync(item => item.UserId == request.ActionBy, cancellationToken))
            {
                return NotFound($"ActionBy user {request.ActionBy} was not found.");
            }

            var parameters = new[]
            {
                new SqlParameter("@AllocationID", allocationId),
                new SqlParameter("@ReviewedBy", request.ActionBy),
                new SqlParameter("@Comments", (object?)request.Comments ?? DBNull.Value)
            };

            var result = await _context.ExecuteStoredProcedureScalarAsync("dbo.sp_RejectAllocation", parameters);

            if (result.ContainsKey("ResultStatus") && result.ContainsKey("AllocationID") && result.ContainsKey("RequestID"))
            {
                var resultObj = new AllocationApprovalResult
                {
                    ResultStatus = result["ResultStatus"]?.ToString() ?? "Unknown",
                    AllocationID = (int)(result["AllocationID"] ?? 0),
                    RequestID = (int)(result["RequestID"] ?? 0)
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
    /// Dispatch an approved allocation using stored procedure (sp_DispatchResources).
    /// Provides ACID-compliant transaction handling at the database level.
    /// </summary>
    [HttpPost("allocations/{allocationId:int}/dispatch-sp")]
    public async Task<ActionResult<DispatchResult>> DispatchResourcesStoredProc(
        int allocationId,
        [FromBody] DispatchRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await _context.Users.AnyAsync(item => item.UserId == request.DispatchedByUserID, cancellationToken))
            {
                return NotFound($"User {request.DispatchedByUserID} was not found.");
            }

            var parameters = new[]
            {
                new SqlParameter("@AllocationID", allocationId),
                new SqlParameter("@DispatchedByUserID", request.DispatchedByUserID)
            };

            var result = await _context.ExecuteStoredProcedureScalarAsync("dbo.sp_DispatchResources", parameters);

            if (result.ContainsKey("ResultStatus") && result.ContainsKey("AllocationID"))
            {
                var resultObj = new DispatchResult
                {
                    ResultStatus = result["ResultStatus"]?.ToString() ?? "Unknown",
                    AllocationID = (int)(result["AllocationID"] ?? 0),
                    DispatchedAt = result.ContainsKey("DispatchedAt") ? (DateTime?)result["DispatchedAt"] : null
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
    /// Get resource allocation status from database view (vw_ResourceAllocation_Status).
    /// Shows current status of all resource allocations with timing and request information.
    /// </summary>
    [HttpGet("allocations-status")]
    public async Task<ActionResult<IEnumerable<ResourceAllocationStatusDto>>> GetAllocationStatus(CancellationToken cancellationToken)
    {
        try
        {
            var allocations = await _context.VwResourceAllocationStatus
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

            return Ok(allocations);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, details = ex.InnerException?.Message });
        }
    }
}

public class ResourceDto
{
    public int ResourceId { get; set; }

    public string ResourceName { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public string Unit { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class ResourceCreateDto
{
    [Required]
    [MaxLength(150)]
    public string ResourceName { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string ResourceType { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Unit { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class WarehouseDto
{
    public int WarehouseId { get; set; }

    public string WarehouseName { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public string Area { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Province { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    public int Capacity { get; set; }

    public int ManagerId { get; set; }

    public string? ContactPhone { get; set; }

    public string? ContactEmail { get; set; }
}

public class WarehouseCreateDto
{
    [Required]
    [MaxLength(150)]
    public string WarehouseName { get; set; } = string.Empty;

    [Required]
    [MaxLength(150)]
    public string Street { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Area { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string City { get; set; } = string.Empty;

    [Required]
    [MaxLength(120)]
    public string Province { get; set; } = string.Empty;

    public decimal? Latitude { get; set; }

    public decimal? Longitude { get; set; }

    [Range(1, int.MaxValue)]
    public int Capacity { get; set; }

    [Range(1, int.MaxValue)]
    public int ManagerId { get; set; }

    [MaxLength(30)]
    public string? ContactPhone { get; set; }

    [EmailAddress]
    [MaxLength(255)]
    public string? ContactEmail { get; set; }
}

public class InventoryDto
{
    public int InventoryId { get; set; }

    public int WarehouseId { get; set; }

    public string WarehouseName { get; set; } = string.Empty;

    public int ResourceId { get; set; }

    public string ResourceName { get; set; } = string.Empty;

    public string ResourceType { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public decimal MinThreshold { get; set; }

    public decimal MaxCapacity { get; set; }

    public DateTime LastUpdated { get; set; }

    public string VersionToken { get; set; } = string.Empty;
}

public class InventoryCreateDto
{
    [Range(1, int.MaxValue)]
    public int WarehouseId { get; set; }

    [Range(1, int.MaxValue)]
    public int ResourceId { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Quantity { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal MinThreshold { get; set; }

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal MaxCapacity { get; set; }
}

public class InventoryUpdateDto
{
    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? Quantity { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? MinThreshold { get; set; }

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal? MaxCapacity { get; set; }

    public string? VersionToken { get; set; }
}

public class ResourceAllocationDto
{
    public int AllocationId { get; set; }

    public int InventoryId { get; set; }

    public int EventId { get; set; }

    public string EventName { get; set; } = string.Empty;

    public int RequestedBy { get; set; }

    public string RequestedByName { get; set; } = string.Empty;

    public decimal Quantity { get; set; }

    public DateTime RequestTime { get; set; }

    public string Status { get; set; } = string.Empty;

    public DateTime? DispatchedAt { get; set; }

    public DateTime? ConsumedAt { get; set; }

    public string ResourceName { get; set; } = string.Empty;

    public string WarehouseName { get; set; } = string.Empty;

    public string VersionToken { get; set; } = string.Empty;
}

public class ResourceAllocationCreateDto
{
    [Range(1, int.MaxValue)]
    public int InventoryId { get; set; }

    [Range(1, int.MaxValue)]
    public int EventId { get; set; }

    [Range(1, int.MaxValue)]
    public int RequestedBy { get; set; }

    [Range(typeof(decimal), "0.0001", "79228162514264337593543950335")]
    public decimal Quantity { get; set; }

    public DateTime? RequestTime { get; set; }

    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = "Pending";

    public bool RequiresApproval { get; set; }

    public int? ApprovalRequestedBy { get; set; }

    public DateTime? DispatchedAt { get; set; }

    public DateTime? ConsumedAt { get; set; }
}

public class ResourceAllocationStatusUpdateDto
{
    [Required]
    [MaxLength(30)]
    public string Status { get; set; } = string.Empty;

    public string? VersionToken { get; set; }
}

public class ResourceLogisticsConcurrencyConflictDto
{
    public string Message { get; set; } = string.Empty;

    public string CurrentVersionToken { get; set; } = string.Empty;
}

public class DispatchRequestDto
{
    [Range(1, int.MaxValue)]
    public int DispatchedByUserID { get; set; }
}
