using Database_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Database_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Administrator")]
public class PermissionController : ControllerBase
{
    private readonly DatabaseProjectContext _context;

    public PermissionController(DatabaseProjectContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<PermissionDetailDto>> CreatePermission([FromBody] CreatePermissionRequest request, CancellationToken cancellationToken)
    {
        // Check if permission already exists
        if (await _context.Permissions.AnyAsync(p => p.PermissionName == request.PermissionName, cancellationToken))
        {
            return BadRequest(new { error = "Permission name already exists" });
        }

        var permission = new Permission
        {
            PermissionName = request.PermissionName,
            Module = request.Module,
            Action = request.Action
        };

        _context.Permissions.Add(permission);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetPermissionById), new { id = permission.PermissionId }, new PermissionDetailDto
        {
            PermissionId = permission.PermissionId,
            PermissionName = permission.PermissionName,
            Module = permission.Module,
            Action = permission.Action,
            RoleCount = 0
        });
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedPermissionListDto>> GetPermissions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? module = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        IQueryable<Permission> query = _context.Permissions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(module))
        {
            query = query.Where(p => p.Module == module);
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var permissions = await query
            .Include(p => p.Roles)
            .OrderBy(p => p.Module)
            .ThenBy(p => p.Action)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new PermissionListItemDto
            {
                PermissionId = p.PermissionId,
                PermissionName = p.PermissionName,
                Module = p.Module,
                Action = p.Action,
                RoleCount = p.Roles.Count
            })
            .ToListAsync(cancellationToken);

        return Ok(new PaginatedPermissionListDto
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Permissions = permissions
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PermissionDetailDto>> GetPermissionById(int id, CancellationToken cancellationToken)
    {
        var permission = await _context.Permissions
            .AsNoTracking()
            .Include(p => p.Roles)
            .FirstOrDefaultAsync(p => p.PermissionId == id, cancellationToken);

        if (permission is null)
        {
            return NotFound(new { error = $"Permission {id} not found" });
        }

        return Ok(new PermissionDetailDto
        {
            PermissionId = permission.PermissionId,
            PermissionName = permission.PermissionName,
            Module = permission.Module,
            Action = permission.Action,
            RoleCount = permission.Roles.Count
        });
    }
}

public class CreatePermissionRequest
{
    [Required(ErrorMessage = "Permission name is required")]
    [MaxLength(100, ErrorMessage = "Permission name cannot exceed 100 characters")]
    public string PermissionName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Module is required")]
    [MaxLength(100, ErrorMessage = "Module cannot exceed 100 characters")]
    public string Module { get; set; } = string.Empty;

    [Required(ErrorMessage = "Action is required")]
    [MaxLength(100, ErrorMessage = "Action cannot exceed 100 characters")]
    public string Action { get; set; } = string.Empty;
}

public class PermissionDetailDto
{
    public int PermissionId { get; set; }

    public string PermissionName { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public int RoleCount { get; set; }
}

public class PermissionListItemDto
{
    public int PermissionId { get; set; }

    public string PermissionName { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;

    public int RoleCount { get; set; }
}

public class PaginatedPermissionListDto
{
    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public List<PermissionListItemDto> Permissions { get; set; } = new();
}
