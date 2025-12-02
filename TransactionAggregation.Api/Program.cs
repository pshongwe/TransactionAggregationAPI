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

        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        if (!builder.Environment.IsProduction())
        {
            builder.Logging.AddDebug();
        }

        // Add services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        
        // Health checks
        builder.Services.AddHealthChecks();

        // Adapters
        builder.Services.AddSingleton<ITransactionSourceAdapter, ASourceAdapter>();
        builder.Services.AddSingleton<ITransactionSourceAdapter, BSourceAdapter>();
        builder.Services.AddSingleton<ITransactionSourceAdapter, CSourceAdapter>();

        // Aggregator
        builder.Services.AddScoped<ITransactionAggregationService, TransactionAggregationService>();

        var app = builder.Build();

        // Use exception handling middleware
        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        
        // Map health checks endpoint
        app.MapHealthChecks("/health");

        app.MapControllers();

        app.Run();
    }
}