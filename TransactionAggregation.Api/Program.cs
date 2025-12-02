public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost.ConfigureKestrel(options =>
        {
            options.ListenAnyIP(8080);
        });

        builder.WebHost.UseSetting(WebHostDefaults.PreferHostingUrlsKey, "true");
        builder.WebHost.UseUrls("http://0.0.0.0:8080");

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        builder.Services.AddHealthChecks();

        builder.Services.AddSingleton<ITransactionSourceAdapter, ASourceAdapter>();
        builder.Services.AddSingleton<ITransactionSourceAdapter, BSourceAdapter>();
        builder.Services.AddSingleton<ITransactionSourceAdapter, CSourceAdapter>();

        builder.Services.AddScoped<ITransactionAggregationService, TransactionAggregationService>();

        var app = builder.Build();

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        // Enable swagger in both dev + prod
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.RoutePrefix = "swagger";
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Transaction API");
        });

        // Disable HTTPS redirect inside containers
        if (!app.Environment.IsProduction())
        {
            app.UseHttpsRedirection();
        }

        app.MapHealthChecks("/health");
        app.MapControllers();

        app.Run();
    }
}
