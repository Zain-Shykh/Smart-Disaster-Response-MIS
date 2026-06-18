using Database_Backend.Models;
using Database_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Database_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Administrator")]
public class UserController : ControllerBase
{
    private readonly DatabaseProjectContext _context;

    public UserController(DatabaseProjectContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<UserDetailDto>> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        // Check if username already exists
        if (await _context.Users.AnyAsync(u => u.Username == request.Username, cancellationToken))
        {
            return BadRequest(new { error = "Username already exists" });
        }

        // Check if email already exists
        if (await _context.Users.AnyAsync(u => u.Email == request.Email, cancellationToken))
        {
            return BadRequest(new { error = "Email already exists" });
        }

        // Validate roles exist
        if (request.RoleIds != null && request.RoleIds.Any())
        {
            var roleCount = await _context.Roles
                .CountAsync(r => request.RoleIds.Contains(r.RoleId), cancellationToken);

            if (roleCount != request.RoleIds.Count)
            {
                return BadRequest(new { error = "One or more roles do not exist" });
            }
        }

        // Create user
        var passwordHash = PasswordHashService.HashPassword(request.Password);
        var user = new User
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Username = request.Username,
            Email = request.Email,
            PasswordHash = passwordHash,
            CreatedAt = DateTime.Now,
            IsActive = true
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        // Assign roles
        if (request.RoleIds != null && request.RoleIds.Any())
        {
            foreach (var roleId in request.RoleIds)
            {
                var userRole = new UserRole
                {
                    UserId = user.UserId,
                    RoleId = roleId,
                    AssignedAt = DateTime.Now,
                    AssignedBy = null
                };
                _context.UserRoles.Add(userRole);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }

        // Get assigned roles
        var roles = await _context.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == user.UserId)
            .Include(ur => ur.Role)
            .Select(ur => new RoleInfo { RoleId = ur.RoleId, RoleName = ur.Role.RoleName })
            .ToListAsync(cancellationToken);

        return CreatedAtAction(nameof(GetUserById), new { id = user.UserId }, new UserDetailDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = roles
        });
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedUserListDto>> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] int? roleId = null,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 10;

        IQueryable<User> query = _context.Users.AsNoTracking();

        if (roleId.HasValue)
        {
            query = query.Where(u => u.UserRoleUsers.Any(ur => ur.RoleId == roleId.Value));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var users = await query
            .OrderBy(u => u.Username)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserListItemDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                IsActive = u.IsActive,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                Roles = u.UserRoleUsers
                    .Select(ur => new RoleInfo { RoleId = ur.RoleId, RoleName = ur.Role.RoleName })
                    .ToList()
            })
            .ToListAsync(cancellationToken);

        return Ok(new PaginatedUserListDto
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            Users = users
        });
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserDetailDto>> GetUserById(int id, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.UserRoleUsers)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == id, cancellationToken);

        if (user is null)
        {
            return NotFound(new { error = $"User {id} not found" });
        }

        var roles = user.UserRoleUsers
            .Select(ur => new RoleInfo { RoleId = ur.RoleId, RoleName = ur.Role.RoleName })
            .ToList();

        return Ok(new UserDetailDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = roles
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<UserDetailDto>> UpdateUser(
        int id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.UserRoleUsers)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.UserId == id, cancellationToken);

        if (user is null)
        {
            return NotFound(new { error = $"User {id} not found" });
        }

        // Check if new email is unique (if being updated)
        if (!string.IsNullOrWhiteSpace(request.Email) && request.Email != user.Email)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.UserId != id, cancellationToken))
            {
                return BadRequest(new { error = "Email already exists" });
            }
            user.Email = request.Email;
        }

        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            user.FirstName = request.FirstName;
        }

        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            user.LastName = request.LastName;
        }

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);

        var roles = user.UserRoleUsers
            .Select(ur => new RoleInfo { RoleId = ur.RoleId, RoleName = ur.Role.RoleName })
            .ToList();

        return Ok(new UserDetailDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            Roles = roles
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == id, cancellationToken);

        if (user is null)
        {
            return NotFound(new { error = $"User {id} not found" });
        }

        // Soft delete - just mark as inactive
        user.IsActive = false;
        _context.Users.Update(user);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Get user roles and permissions from vw_User_Roles_Permissions view.
    /// Shows complete RBAC mapping for access control auditing.
    /// </summary>
    [HttpGet("roles-permissions")]
    public async Task<ActionResult<IEnumerable<UserRolesPermissionsDto>>> GetUserRolesPermissionsFromView(
        CancellationToken cancellationToken)
    {
        var result = await _context.VwUserRolesPermissions
            .AsNoTracking()
            .Select(v => new UserRolesPermissionsDto
            {
                UserId = v.UserId,
                Username = v.Username,
                RoleId = v.RoleId,
                RoleName = v.RoleName,
                PermissionId = v.PermissionId,
                PermissionName = v.PermissionName,
                Module = v.Module,
                Action = v.Action
            })
            .ToListAsync(cancellationToken);

        return Ok(result);
    }
}

public class CreateUserRequest
{
    [Required(ErrorMessage = "Username is required")]
    [MaxLength(100, ErrorMessage = "Username cannot exceed 100 characters")]
    [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required")]
    [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public string LastName { get; set; } = string.Empty;

    public List<int> RoleIds { get; set; } = new();
}

public class UpdateUserRequest
{
    [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    public string? Email { get; set; }

    [MaxLength(100, ErrorMessage = "First name cannot exceed 100 characters")]
    public string? FirstName { get; set; }

    [MaxLength(100, ErrorMessage = "Last name cannot exceed 100 characters")]
    public string? LastName { get; set; }

    public bool? IsActive { get; set; }
}

public class UserDetailDto
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public List<RoleInfo> Roles { get; set; } = new();
}

public class UserListItemDto
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public List<RoleInfo> Roles { get; set; } = new();
}

public class PaginatedUserListDto
{
    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }

    public List<UserListItemDto> Users { get; set; } = new();
}

public class RoleInfo
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;
}

// ============================================================================
// DATABASE VIEW DTOs
// ============================================================================

public class UserRolesPermissionsDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int PermissionId { get; set; }
    public string PermissionName { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
}
