using Microsoft.OpenApi.Models;
using TransactionAggregation.Api.Adapters;
using TransactionAggregation.Api.Middleware;
using TransactionAggregation.Api.Security;
using TransactionAggregation.Api.Services;
using TransactionAggregation.Domain.Abstractions;
using TransactionAggregation.Domain.Services;

namespace TransactionAggregation.Api;

/// <summary>
/// Entry point for the Transaction Aggregation API.
/// </summary>
public partial class Program
{
    /// <summary>
    /// Main entry point of the application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Kestrel must listen on 0.0.0.0:8080 for containers/Fly.io/Render
        builder.WebHost
            .UseKestrel()
            .UseUrls(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
                     ?? "http://0.0.0.0:8080");

        // MVC + controllers
        builder.Services.AddControllers();

        // Swagger + JWT auth in UI
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Transaction Aggregation API",
                Version = "v1"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter: `Bearer {token}`"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });

            // Include XML documentation comments
            var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        builder.Services.AddHealthChecks();

        // Domain services
        builder.Services.AddSingleton<ITransactionSourceAdapter>(sp =>
            new ASourceAdapter(
                sp.GetRequiredService<IWebHostEnvironment>(),
                sp.GetRequiredService<ILogger<ASourceAdapter>>()));
        builder.Services.AddSingleton<ITransactionSourceAdapter>(sp =>
            new BSourceAdapter(
                sp.GetRequiredService<IWebHostEnvironment>(),
                sp.GetRequiredService<ILogger<BSourceAdapter>>()));
        builder.Services.AddSingleton<ITransactionSourceAdapter>(sp =>
            new CSourceAdapter(
                sp.GetRequiredService<IWebHostEnvironment>(),
                sp.GetRequiredService<ILogger<CSourceAdapter>>()));
        builder.Services.AddScoped<ITransactionAggregationService, TransactionAggregationService>();
        builder.Services.Decorate<ITransactionAggregationService, CachedTransactionAggregationService>();
        builder.Services.AddMemoryCache();

        // Enterprise security bundle
        builder.Services.AddEnterpriseSecurity(builder.Configuration);

        var app = builder.Build();

        // Swagger
        app.UseSwagger();
        app.UseSwaggerUI(o =>
        {
            o.SwaggerEndpoint("/swagger/v1/swagger.json", "Transaction Aggregation API v1");
        });

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // Security
        app.UseEnterpriseSecurityPipeline(app.Environment);

        // Routes
        app.MapHealthChecks("/health");
        app.MapControllers();

        app.Run();
    }
}