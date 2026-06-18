using Database_Backend.Models;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Database_Backend.Services;

public interface IJwtTokenService
{
    JwtTokenResult GenerateToken(User user, IReadOnlyCollection<string> roles);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly IConfiguration _configuration;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public JwtTokenResult GenerateToken(User user, IReadOnlyCollection<string> roles)
    {
        var jwtSection = _configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"] ?? "DatabaseBackend";
        var audience = jwtSection["Audience"] ?? "DatabaseBackendClient";
        var key = jwtSection["Key"] ?? "change-this-super-secret-key-for-production-min-32-chars";
        var expiresMinutes = int.TryParse(jwtSection["ExpiresMinutes"], out var parsedMinutes) ? parsedMinutes : 120;

        var now = DateTime.Now;
        var expiresAt = now.AddMinutes(expiresMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.UserId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.Username),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("full_name", $"{user.FirstName} {user.LastName}"),
            new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new(ClaimTypes.Name, user.Username)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            notBefore: now,
            expires: expiresAt,
            signingCredentials: credentials);

        var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);

        return new JwtTokenResult(token, expiresAt);
    }
}

public record JwtTokenResult(string AccessToken, DateTime ExpiresAt);
