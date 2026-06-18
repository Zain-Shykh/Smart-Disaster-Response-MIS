using Database_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Database_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Administrator")]
public class RbacController : ControllerBase
{
    private readonly DatabaseProjectContext _context;

    public RbacController(DatabaseProjectContext context)
    {
        _context = context;
    }

    [HttpGet("roles")]
    public async Task<ActionResult<IEnumerable<RoleDto>>> GetRoles(CancellationToken cancellationToken)
    {
        var roles = await _context.Roles
            .AsNoTracking()
            .OrderBy(item => item.RoleName)
            .Select(item => new RoleDto
            {
                RoleId = item.RoleId,
                RoleName = item.RoleName,
                Description = item.Description
            })
            .ToListAsync(cancellationToken);

        return Ok(roles);
    }

    [HttpGet("users/{userId:int}/roles")]
    public async Task<ActionResult<IEnumerable<UserRoleDto>>> GetUserRoles(int userId, CancellationToken cancellationToken)
    {
        if (!await _context.Users.AnyAsync(item => item.UserId == userId, cancellationToken))
        {
            return NotFound($"User {userId} was not found.");
        }

        var userRoles = await _context.UserRoles
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .Include(item => item.Role)
            .Select(item => new UserRoleDto
            {
                UserId = item.UserId,
                RoleId = item.RoleId,
                RoleName = item.Role.RoleName,
                AssignedAt = item.AssignedAt,
                AssignedBy = item.AssignedBy
            })
            .ToListAsync(cancellationToken);

        return Ok(userRoles);
    }

    [HttpPost("users/{userId:int}/roles")]
    public async Task<ActionResult<UserRoleDto>> AssignRoleToUser(
        int userId,
        [FromBody] AssignRoleDto request,
        CancellationToken cancellationToken)
    {
        if (!await _context.Users.AnyAsync(item => item.UserId == userId, cancellationToken))
        {
            return NotFound($"User {userId} was not found.");
        }

        var role = await _context.Roles.FirstOrDefaultAsync(item => item.RoleId == request.RoleId, cancellationToken);
        if (role is null)
        {
            return NotFound($"Role {request.RoleId} was not found.");
        }

        if (request.AssignedBy.HasValue && !await _context.Users.AnyAsync(item => item.UserId == request.AssignedBy.Value, cancellationToken))
        {
            return NotFound($"AssignedBy user {request.AssignedBy.Value} was not found.");
        }

        if (await _context.UserRoles.AnyAsync(item => item.UserId == userId && item.RoleId == request.RoleId, cancellationToken))
        {
            return Conflict("User already has this role.");
        }

        var userRole = new UserRole
        {
            UserId = userId,
            RoleId = request.RoleId,
            AssignedAt = DateTime.Now,
            AssignedBy = request.AssignedBy
        };

        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetUserRoles), new { userId }, new UserRoleDto
        {
            UserId = userId,
            RoleId = request.RoleId,
            RoleName = role.RoleName,
            AssignedAt = userRole.AssignedAt,
            AssignedBy = userRole.AssignedBy
        });
    }

    [HttpDelete("users/{userId:int}/roles/{roleId:int}")]
    public async Task<IActionResult> RemoveRoleFromUser(int userId, int roleId, CancellationToken cancellationToken)
    {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(item => item.UserId == userId && item.RoleId == roleId, cancellationToken);

        if (userRole is null)
        {
            return NotFound();
        }

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    [HttpGet("roles/{roleId:int}/permissions")]
    public async Task<ActionResult<IEnumerable<PermissionDto>>> GetRolePermissions(int roleId, CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .AsNoTracking()
            .Include(item => item.Permissions)
            .FirstOrDefaultAsync(item => item.RoleId == roleId, cancellationToken);

        if (role is null)
        {
            return NotFound();
        }

        var permissions = role.Permissions
            .OrderBy(item => item.Module)
            .ThenBy(item => item.Action)
            .Select(item => new PermissionDto
            {
                PermissionId = item.PermissionId,
                PermissionName = item.PermissionName,
                Module = item.Module,
                Action = item.Action
            })
            .ToList();

        return Ok(permissions);
    }

    [HttpPost("role-permission")]
    public async Task<ActionResult<RolePermissionDto>> MapPermissionToRole(
        [FromBody] MapRolePermissionDto request,
        CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.RoleId == request.RoleId, cancellationToken);

        if (role is null)
        {
            return NotFound(new { error = $"Role {request.RoleId} not found" });
        }

        var permission = await _context.Permissions
            .FirstOrDefaultAsync(p => p.PermissionId == request.PermissionId, cancellationToken);

        if (permission is null)
        {
            return NotFound(new { error = $"Permission {request.PermissionId} not found" });
        }

        // Check if permission is already mapped to role
        if (role.Permissions.Any(p => p.PermissionId == request.PermissionId))
        {
            return Conflict(new { error = "Permission is already mapped to this role" });
        }

        role.Permissions.Add(permission);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new RolePermissionDto
        {
            RoleId = role.RoleId,
            RoleName = role.RoleName,
            PermissionId = permission.PermissionId,
            PermissionName = permission.PermissionName,
            Module = permission.Module,
            Action = permission.Action
        });
    }

    [HttpDelete("role-permission/{roleId:int}/{permissionId:int}")]
    public async Task<IActionResult> UnmapPermissionFromRole(
        int roleId,
        int permissionId,
        CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .Include(r => r.Permissions)
            .FirstOrDefaultAsync(r => r.RoleId == roleId, cancellationToken);

        if (role is null)
        {
            return NotFound(new { error = $"Role {roleId} not found" });
        }

        var permission = role.Permissions.FirstOrDefault(p => p.PermissionId == permissionId);

        if (permission is null)
        {
            return NotFound(new { error = "Permission is not mapped to this role" });
        }

        role.Permissions.Remove(permission);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

}

public class AssignRoleDto
{
    public int RoleId { get; set; }

    public int? AssignedBy { get; set; }
}

public class RoleDto
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class UserRoleDto
{
    public int UserId { get; set; }

    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public DateTime AssignedAt { get; set; }

    public int? AssignedBy { get; set; }
}

public class PermissionDto
{
    public int PermissionId { get; set; }

    public string PermissionName { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;
}

public class MapRolePermissionDto
{
    public int RoleId { get; set; }

    public int PermissionId { get; set; }
}

public class RolePermissionDto
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public int PermissionId { get; set; }

    public string PermissionName { get; set; } = string.Empty;

    public string Module { get; set; } = string.Empty;

    public string Action { get; set; } = string.Empty;
}
