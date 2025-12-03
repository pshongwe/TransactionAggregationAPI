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

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks();

        builder.Services.AddSingleton<ITransactionSourceAdapter, ASourceAdapter>();
        builder.Services.AddSingleton<ITransactionSourceAdapter, BSourceAdapter>();
        builder.Services.AddSingleton<ITransactionSourceAdapter, CSourceAdapter>();
        builder.Services.AddScoped<ITransactionAggregationService, TransactionAggregationService>();

        var app = builder.Build();

        if (!app.Environment.IsProduction())
        {
            app.UseHttpsRedirection();
        }


        app.UseSwagger();
        app.UseSwaggerUI();

        app.MapHealthChecks("/health");
        app.MapControllers();

        app.Run();
    }
}
