using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransactionAggregation.Api.Security;

namespace TransactionAggregation.Api.Controllers;

/// <summary>
/// Controller for handling authentication and token generation.
/// </summary>
[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IJwtTokenService _jwt;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthController"/> class.
    /// </summary>
    /// <param name="jwt">The JWT token service.</param>
    public AuthController(IJwtTokenService jwt)
    {
        _jwt = jwt;
    }

    /// <summary>
    /// Request model for login endpoint.
    /// </summary>
    /// <param name="Username">The username for authentication.</param>
    /// <param name="Password">The password for authentication.</param>
    public record LoginRequest(string Username, string Password);

    /// <summary>
    /// Authenticates a user and returns a JWT token.
    /// </summary>
    /// <param name="req">The login request containing username and password.</param>
    /// <param name="config">The application configuration.</param>
    /// <returns>An action result containing the JWT token or unauthorized response.</returns>
    [HttpPost("token")]
    public IActionResult Login([FromBody] LoginRequest req, IConfiguration config)
    {
        var expectedUser = config["Auth:Username"];
        var expectedPass = config["Auth:Password"];

        if (req.Username != expectedUser || req.Password != expectedPass)
            return Unauthorized();

        var token = _jwt.GenerateToken(req.Username);

        return Ok(new { token });
    }

}