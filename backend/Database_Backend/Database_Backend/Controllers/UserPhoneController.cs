using Database_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Database_Backend.Controllers;

[Route("api/User/{userId:int}/Phone")]
[ApiController]
[Authorize]
public class UserPhoneController : ControllerBase
{
    private readonly DatabaseProjectContext _context;

    public UserPhoneController(DatabaseProjectContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<UserPhoneDto>> AddPhone(
        int userId,
        [FromBody] AddUserPhoneRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanAccessUserAsync(userId, cancellationToken))
        {
            return Forbid();
        }

        if (!await _context.Users.AnyAsync(item => item.UserId == userId, cancellationToken))
        {
            return NotFound($"User {userId} was not found.");
        }

        var phone = request.PhoneNumber.Trim();

        if (await _context.UserPhones.AnyAsync(item => item.UserId == userId && item.Phone == phone, cancellationToken))
        {
            return Conflict("Phone already exists for this user.");
        }

        var entity = new UserPhone
        {
            UserId = userId,
            Phone = phone
        };

        _context.UserPhones.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetPhones), new { userId }, new UserPhoneDto
        {
            UserId = entity.UserId,
            PhoneNumber = entity.Phone
        });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserPhoneDto>>> GetPhones(int userId, CancellationToken cancellationToken)
    {
        if (!await CanAccessUserAsync(userId, cancellationToken))
        {
            return Forbid();
        }

        if (!await _context.Users.AnyAsync(item => item.UserId == userId, cancellationToken))
        {
            return NotFound($"User {userId} was not found.");
        }

        var phones = await _context.UserPhones
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderBy(item => item.Phone)
            .Select(item => new UserPhoneDto
            {
                UserId = item.UserId,
                PhoneNumber = item.Phone
            })
            .ToListAsync(cancellationToken);

        return Ok(phones);
    }

    [HttpPut("{phone}")]
    public async Task<ActionResult<UserPhoneDto>> UpdatePhone(
        int userId,
        string phone,
        [FromBody] UpdateUserPhoneRequest request,
        CancellationToken cancellationToken)
    {
        if (!await CanAccessUserAsync(userId, cancellationToken))
        {
            return Forbid();
        }

        var existing = await _context.UserPhones
            .FirstOrDefaultAsync(item => item.UserId == userId && item.Phone == phone, cancellationToken);

        if (existing is null)
        {
            return NotFound("Phone was not found for this user.");
        }

        var newPhone = request.NewPhoneNumber.Trim();
        if (!string.Equals(existing.Phone, newPhone, StringComparison.Ordinal) &&
            await _context.UserPhones.AnyAsync(item => item.UserId == userId && item.Phone == newPhone, cancellationToken))
        {
            return Conflict("New phone already exists for this user.");
        }

        _context.UserPhones.Remove(existing);
        _context.UserPhones.Add(new UserPhone
        {
            UserId = userId,
            Phone = newPhone
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new UserPhoneDto
        {
            UserId = userId,
            PhoneNumber = newPhone
        });
    }

    [HttpDelete("{phone}")]
    public async Task<IActionResult> DeletePhone(int userId, string phone, CancellationToken cancellationToken)
    {
        if (!await CanAccessUserAsync(userId, cancellationToken))
        {
            return Forbid();
        }

        var existing = await _context.UserPhones
            .FirstOrDefaultAsync(item => item.UserId == userId && item.Phone == phone, cancellationToken);

        if (existing is null)
        {
            return NotFound("Phone was not found for this user.");
        }

        _context.UserPhones.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private Task<bool> CanAccessUserAsync(int userId, CancellationToken cancellationToken)
    {
        if (User.IsInRole("Administrator"))
        {
            return Task.FromResult(true);
        }

        var nameId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(nameId, out var callerUserId))
        {
            return Task.FromResult(false);
        }

        return Task.FromResult(callerUserId == userId);
    }
}

public class AddUserPhoneRequest
{
    [Required]
    [Phone]
    [MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class UpdateUserPhoneRequest
{
    [Required]
    [Phone]
    [MaxLength(30)]
    public string NewPhoneNumber { get; set; } = string.Empty;
}

public class UserPhoneDto
{
    public int UserId { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;
}
