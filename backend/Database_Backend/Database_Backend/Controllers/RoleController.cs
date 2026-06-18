using Database_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Database_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Administrator")]
public class RoleController : ControllerBase
{
    private readonly DatabaseProjectContext _context;

    public RoleController(DatabaseProjectContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<RoleDetailDto>> CreateRole([FromBody] CreateRoleRequest request, CancellationToken cancellationToken)
    {
        // Check if role name already exists
        if (await _context.Roles.AnyAsync(r => r.RoleName == request.RoleName, cancellationToken))
        {
            return BadRequest(new { error = "Role name already exists" });
        }

        var role = new Role
        {
            RoleName = request.RoleName,
            Description = request.Description
        };

        _context.Roles.Add(role);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetRoleById), new { id = role.RoleId }, new RoleDetailDto
        {
            RoleId = role.RoleId,
            RoleName = role.RoleName,
            Description = role.Description,
            PermissionCount = 0
        });
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedRoleListDto>> GetRoles(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        var totalCount = await _context.Roles.CountAsync(cancellationToken);

        var roles = await _context.Roles
            .AsNoTracking()
            .Include(r => r.Permissions)
            .OrderBy(r => r.RoleName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new RoleListItemDto
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName,
                Description = r.Description,
                PermissionCount = r.Permissions.Count,
                UserCount = r.UserRoles.Count
            })
            .ToListAsync(cancellationToken);

        return Ok(new PaginatedRoleListDto
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Roles = roles
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<RoleDetailDto>> GetRoleById(int id, CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.RoleId == id, cancellationToken);

        if (role is null)
        {
            return NotFound(new { error = $"Role {id} not found" });
        }

        return Ok(new RoleDetailDto
        {
            RoleId = role.RoleId,
            RoleName = role.RoleName,
            Description = role.Description,
            PermissionCount = role.Permissions.Count
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<RoleDetailDto>> UpdateRole(
        int id,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.RoleId == id, cancellationToken);

        if (role is null)
        {
            return NotFound(new { error = $"Role {id} not found" });
        }

        // Check if new name is unique (if being updated)
        if (!string.IsNullOrWhiteSpace(request.RoleName) && request.RoleName != role.RoleName)
        {
            if (await _context.Roles.AnyAsync(r => r.RoleName == request.RoleName && r.RoleId != id, cancellationToken))
            {
                return BadRequest(new { error = "Role name already exists" });
            }
            role.RoleName = request.RoleName;
        }

        if (!string.IsNullOrWhiteSpace(request.Description))
        {
            role.Description = request.Description;
        }

        _context.Roles.Update(role);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new RoleDetailDto
        {
            RoleId = role.RoleId,
            RoleName = role.RoleName,
            Description = role.Description,
            PermissionCount = role.Permissions.Count
        });
    }
}

public class CreateRoleRequest
{
    [Required(ErrorMessage = "Role name is required")]
    [MaxLength(100, ErrorMessage = "Role name cannot exceed 100 characters")]
    [MinLength(3, ErrorMessage = "Role name must be at least 3 characters")]
    public string RoleName { get; set; } = string.Empty;

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
}

public class UpdateRoleRequest
{
    [MaxLength(100, ErrorMessage = "Role name cannot exceed 100 characters")]
    [MinLength(3, ErrorMessage = "Role name must be at least 3 characters")]
    public string? RoleName { get; set; }

    [MaxLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
}

public class RoleDetailDto
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int PermissionCount { get; set; }
}

public class RoleListItemDto
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int PermissionCount { get; set; }

    public int UserCount { get; set; }
}

public class PaginatedRoleListDto
{
    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public List<RoleListItemDto> Roles { get; set; } = new();
}
