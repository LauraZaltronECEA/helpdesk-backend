using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using api.models.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace api.services.Handlers;

// Creates JWT bearer tokens using settings from the "Jwt" configuration section.
public class JwtHandler
{
    private readonly IConfiguration _configuration;

    public JwtHandler(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    // Creates and returns a signed JWT string with user identity and role claims.
    // Claims include: Sub (user ID), NameIdentifier, Name, GivenName, and Role.
    public string GenerateToken(User user, string roleName)
    {
        var jwt = _configuration.GetSection("Jwt");
        var secret = jwt["Secret"] ?? throw new InvalidOperationException("Jwt:Secret is not configured");
        var issuer = jwt["Issuer"] ?? "api.helpdesk";
        var audience = jwt["Audience"] ?? "helpdesk";
        var minutes = int.TryParse(jwt["ExpirationMinutes"], out var parsed) ? parsed : 120;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username ?? string.Empty),
            new(ClaimTypes.GivenName, user.Fullname ?? string.Empty),
            new(ClaimTypes.Role, roleName)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.UtcNow.AddMinutes(minutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
