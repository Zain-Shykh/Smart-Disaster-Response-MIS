using Database_Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Database_Backend.Controllers;

[Route("api/Donor/{donorId:int}/Phone")]
[ApiController]
[Authorize(Roles = "Administrator,FinanceOfficer,Finance Officer")]
public class DonorPhoneController : ControllerBase
{
    private readonly DatabaseProjectContext _context;

    public DonorPhoneController(DatabaseProjectContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<ActionResult<DonorPhoneDto>> AddPhone(
        int donorId,
        [FromBody] AddDonorPhoneRequest request,
        CancellationToken cancellationToken)
    {
        if (!await _context.Donors.AnyAsync(item => item.DonorId == donorId, cancellationToken))
        {
            return NotFound($"Donor {donorId} was not found.");
        }

        var phone = request.PhoneNumber.Trim();
        if (await _context.DonorPhones.AnyAsync(item => item.DonorId == donorId && item.Phone == phone, cancellationToken))
        {
            return Conflict("Phone already exists for this donor.");
        }

        var entity = new DonorPhone
        {
            DonorId = donorId,
            Phone = phone
        };

        _context.DonorPhones.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetPhones), new { donorId }, new DonorPhoneDto
        {
            DonorId = donorId,
            PhoneNumber = phone
        });
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DonorPhoneDto>>> GetPhones(int donorId, CancellationToken cancellationToken)
    {
        if (!await _context.Donors.AnyAsync(item => item.DonorId == donorId, cancellationToken))
        {
            return NotFound($"Donor {donorId} was not found.");
        }

        var phones = await _context.DonorPhones
            .AsNoTracking()
            .Where(item => item.DonorId == donorId)
            .OrderBy(item => item.Phone)
            .Select(item => new DonorPhoneDto
            {
                DonorId = donorId,
                PhoneNumber = item.Phone
            })
            .ToListAsync(cancellationToken);

        return Ok(phones);
    }

    [HttpPut("{phone}")]
    public async Task<ActionResult<DonorPhoneDto>> UpdatePhone(
        int donorId,
        string phone,
        [FromBody] UpdateDonorPhoneRequest request,
        CancellationToken cancellationToken)
    {
        var existing = await _context.DonorPhones
            .FirstOrDefaultAsync(item => item.DonorId == donorId && item.Phone == phone, cancellationToken);

        if (existing is null)
        {
            return NotFound("Phone was not found for this donor.");
        }

        var newPhone = request.NewPhoneNumber.Trim();
        if (!string.Equals(existing.Phone, newPhone, StringComparison.Ordinal) &&
            await _context.DonorPhones.AnyAsync(item => item.DonorId == donorId && item.Phone == newPhone, cancellationToken))
        {
            return Conflict("New phone already exists for this donor.");
        }

        _context.DonorPhones.Remove(existing);
        _context.DonorPhones.Add(new DonorPhone
        {
            DonorId = donorId,
            Phone = newPhone
        });

        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new DonorPhoneDto
        {
            DonorId = donorId,
            PhoneNumber = newPhone
        });
    }

    [HttpDelete("{phone}")]
    public async Task<IActionResult> DeletePhone(int donorId, string phone, CancellationToken cancellationToken)
    {
        var existing = await _context.DonorPhones
            .FirstOrDefaultAsync(item => item.DonorId == donorId && item.Phone == phone, cancellationToken);

        if (existing is null)
        {
            return NotFound("Phone was not found for this donor.");
        }

        _context.DonorPhones.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);

        return NoContent();
    }
}

public class AddDonorPhoneRequest
{
    [Required]
    [Phone]
    [MaxLength(30)]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class UpdateDonorPhoneRequest
{
    [Required]
    [Phone]
    [MaxLength(30)]
    public string NewPhoneNumber { get; set; } = string.Empty;
}

public class DonorPhoneDto
{
    public int DonorId { get; set; }

    public string PhoneNumber { get; set; } = string.Empty;
}
