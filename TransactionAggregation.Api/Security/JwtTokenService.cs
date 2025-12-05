using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace TransactionAggregation.Api.Security;

/// <summary>
/// Service for generating and validating JWT tokens.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a JWT token for the specified user.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="role">Optional role to include in the token claims.</param>
    /// <param name="lifetime">Optional token lifetime; defaults to 1 hour if not specified.</param>
    /// <returns>A JWT token string.</returns>
    string GenerateToken(string userId, string? role = null, TimeSpan? lifetime = null);
}

/// <summary>
/// Implementation of the JWT token service.
/// </summary>
public class JwtTokenService : IJwtTokenService
{
    private readonly string _issuer;
    private readonly SymmetricSecurityKey _signingKey;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtTokenService"/> class.
    /// </summary>
    /// <param name="config">The application configuration.</param>
    public JwtTokenService(IConfiguration config)
    {
        _issuer = config["Jwt:Issuer"] ?? throw new Exception("Missing configuration: Jwt:Issuer");
        var key = config["Jwt:Key"] ?? throw new Exception("Missing configuration: Jwt:Key");
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
    }

    public string GenerateToken(string userId, string? role = null, TimeSpan? lifetime = null)
    {
        lifetime ??= TimeSpan.FromHours(1);
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        if (!string.IsNullOrWhiteSpace(role))
        {
            claims.Add(new Claim("role", role));
        }

        var creds = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: null,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.Add(lifetime.Value),
            signingCredentials: creds
        );
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}