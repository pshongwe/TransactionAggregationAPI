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

        // Force Kestrel to listen on 0.0.0.0:8080 — REQUIRED for Fly.io
        builder.WebHost
            .UseKestrel()
            .UseUrls(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")
                     ?? "http://0.0.0.0:8080");

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

        // ❌ NEVER redirect to HTTPS on Fly.io
        // Fly handles HTTPS termination outside the VM
        //
        // REMOVE THIS COMPLETELY:
        // if (!app.Environment.IsProduction())
        // {
        //     app.UseHttpsRedirection();
        // }

        // Swagger always on
        app.UseSwagger();
        app.UseSwaggerUI();

        // Routes
        app.MapHealthChecks("/health");
        app.MapControllers();

        app.Run();
    }
}
