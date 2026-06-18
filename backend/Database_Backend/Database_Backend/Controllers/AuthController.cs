using Database_Backend.Models;
using Database_Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Database_Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly DatabaseProjectContext _context;
    private readonly IJwtTokenService _jwtTokenService;

    public AuthController(DatabaseProjectContext context, IJwtTokenService jwtTokenService)
    {
        _context = context;
        _jwtTokenService = jwtTokenService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UsernameOrEmail) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest("UsernameOrEmail and Password are required.");
        }

        var usernameOrEmail = request.UsernameOrEmail.Trim();
        var user = await _context.Users
            .FirstOrDefaultAsync(item => item.Username == usernameOrEmail || item.Email == usernameOrEmail, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Unauthorized("Invalid credentials.");
        }

        if (!PasswordHashService.VerifyPassword(request.Password, user.PasswordHash, out var needsRehash))
        {
            return Unauthorized("Invalid credentials.");
        }

        if (needsRehash)
        {
            user.PasswordHash = PasswordHashService.HashPassword(request.Password);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var roles = await _context.UserRoles
            .AsNoTracking()
            .Where(item => item.UserId == user.UserId)
            .Include(item => item.Role)
            .Select(item => item.Role.RoleName)
            .ToListAsync(cancellationToken);

        var tokenResult = _jwtTokenService.GenerateToken(user, roles);

        return Ok(new LoginResponseDto
        {
            AccessToken = tokenResult.AccessToken,
            ExpiresAt = tokenResult.ExpiresAt,
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}",
            Roles = roles
        });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<CurrentUserDto>> Me(CancellationToken cancellationToken)
    {
        var nameId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(nameId, out var userId))
        {
            return Unauthorized();
        }

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);

        if (user is null)
        {
            return Unauthorized();
        }

        var roles = User.FindAll(ClaimTypes.Role).Select(item => item.Value).Distinct().ToList();

        return Ok(new CurrentUserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}",
            Roles = roles
        });
    }
}

public class LoginRequestDto
{
    [Required]
    [MinLength(3)]
    [MaxLength(255)]
    public string UsernameOrEmail { get; set; } = string.Empty;

    [Required]
    [MinLength(4)]
    [MaxLength(200)]
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = new();
}

public class CurrentUserDto
{
    public int UserId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public List<string> Roles { get; set; } = new();
}
