using TransactionAggregation.Api.Adapters;
using TransactionAggregation.Api.Middleware;
using TransactionAggregation.Domain.Abstractions;
using TransactionAggregation.Domain.Services;

namespace TransactionAggregation.Api;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Explicitly listen on Fly.io port (8080)
        builder.WebHost.UseUrls("http://0.0.0.0:8080");

        // Services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks();

        builder.Services.AddSingleton<ITransactionSourceAdapter, ASourceAdapter>();
        builder.Services.AddSingleton<ITransactionSourceAdapter, BSourceAdapter>();
        builder.Services.AddSingleton<ITransactionSourceAdapter, CSourceAdapter>();
        builder.Services.AddScoped<ITransactionAggregationService, TransactionAggregationService>();

        var app = builder.Build();

        // *** IMPORTANT ***
        // Never redirect HTTP → HTTPS on Fly.io containers
        // Fly proxy handles TLS termination at the edge.
        // Redirecting breaks internal /health checks.
        // Only enable redirect in DEVELOPMENT.
        if (app.Environment.IsDevelopment())
        {
            app.UseHttpsRedirection();
        }

        // Enable Swagger ALWAYS (Fly prod servers hide UI unless explicitly enabled)
        app.UseSwagger();
        app.UseSwaggerUI();

        // Health endpoint for Fly.io
        app.MapHealthChecks("/health");

        // Controllers
        app.MapControllers();

        app.Run();
    }
}
