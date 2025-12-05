using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;

namespace TransactionAggregation.Api.Security;

/// <summary>
/// Extension methods for configuring enterprise security features.
/// </summary>
public static class SecurityExtensions
{
    /// <summary>
    /// Adds enterprise security services including JWT authentication, authorization policies, and rate limiting.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="config">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddEnterpriseSecurity(this IServiceCollection services, IConfiguration config)
    {
        var jwtKey = config["Jwt:Key"]
                     ?? throw new Exception("Missing configuration: Jwt:Key");
        var jwtIssuer = config["Jwt:Issuer"]
                        ?? throw new Exception("Missing configuration: Jwt:Issuer");

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

        services.AddScoped<IJwtTokenService>(sp => new JwtTokenService(config));

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,

                    ValidateAudience = false,

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,

                    RequireSignedTokens = true,
                    RequireExpirationTime = true
                };
            });

        services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.RequireUser, p =>
                p.RequireAuthenticatedUser());

            options.AddPolicy(Policies.AdminOnly, p =>
                p.RequireClaim("role", "admin"));

            options.AddPolicy(Policies.InternalService, p =>
                p.RequireAssertion(ctx =>
                {
                    var http = ctx.Resource as HttpContext;
                    if (http == null) return false;

                    var providedKey = http.Request.Headers["X-Internal-Api-Key"].FirstOrDefault();
                    var expectedKey = Environment.GetEnvironmentVariable("INTERNAL_API_KEY");
                    return !string.IsNullOrEmpty(expectedKey)
                           && !string.IsNullOrEmpty(providedKey)
                           && providedKey == expectedKey;
                }));
        });

        services.AddRateLimiter(options =>
        {
            options.AddFixedWindowLimiter("default", opt =>
            {
                opt.PermitLimit = 100;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 50;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            options.AddFixedWindowLimiter("summary-heavy", opt =>
            {
                opt.PermitLimit = 20;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.QueueLimit = 20;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        services.AddHttpContextAccessor();
        services.AddSingleton<ICorrelationIdProvider, CorrelationIdProvider>();

        return services;
    }

    /// <summary>
    /// Configures the application security pipeline including authentication, authorization, and rate limiting middleware.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="env">The web host environment.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseEnterpriseSecurityPipeline(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        app.Use(async (ctx, next) =>
        {
            var provider = ctx.RequestServices.GetRequiredService<ICorrelationIdProvider>();
            provider.AttachCorrelationId(ctx);
            await next();
        });

        app.Use(async (ctx, next) =>
        {
            ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
            ctx.Response.Headers["X-Frame-Options"] = "DENY";
            ctx.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            ctx.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            ctx.Response.Headers["Content-Security-Policy"] = "default-src 'self'";

            await next();
        });

        app.Use(async (ctx, next) =>
        {
            try
            {
                await next();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] {ex.Message}");

                ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
                ctx.Response.ContentType = "application/json";

                var traceId = ctx.TraceIdentifier;
                await ctx.Response.WriteAsync($$"""
                { "error": "Internal server error", "traceId": "{{traceId}}" }
                """);
            }
        });

        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }
}

/// <summary>
/// Provides functionality for managing correlation IDs across HTTP requests.
/// </summary>
public interface ICorrelationIdProvider
{
    /// <summary>
    /// Attaches a correlation ID to the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    void AttachCorrelationId(HttpContext context);

    /// <summary>
    /// Retrieves the correlation ID from the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The correlation ID.</returns>
    string GetCorrelationId(HttpContext context);
}

/// <summary>
/// Implementation of correlation ID provider.
/// </summary>
public class CorrelationIdProvider : ICorrelationIdProvider
{
    private const string HeaderName = "X-Correlation-ID";

    /// <summary>
    /// Attaches a correlation ID to the HTTP context for request tracing.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public void AttachCorrelationId(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(HeaderName, out var cid) ||
            string.IsNullOrWhiteSpace(cid))
        {
            cid = Guid.NewGuid().ToString("N");
            context.Request.Headers[HeaderName] = cid;
        }

        context.Response.Headers[HeaderName] = cid!;
    }

    /// <summary>
    /// Retrieves the correlation ID from the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>The correlation ID if present; otherwise a new GUID.</returns>
    public string GetCorrelationId(HttpContext context)
        => context.Request.Headers.TryGetValue(HeaderName, out var cid) && !string.IsNullOrWhiteSpace(cid)
            ? cid!
            : Guid.NewGuid().ToString("N");
}