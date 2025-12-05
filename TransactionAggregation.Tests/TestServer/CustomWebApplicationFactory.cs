using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TransactionAggregation.Api;

namespace TransactionAggregation.Tests.TestServer;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((context, configBuilder) =>
        {
            configBuilder.AddJsonFile("appsettings.Testing.json", optional: false, reloadOnChange: true);
        });

        builder.ConfigureServices(services =>
        {
            Environment.SetEnvironmentVariable("INTERNAL_API_KEY", "TEST_INTERNAL_KEY");
        });
    }
}