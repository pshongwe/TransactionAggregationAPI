using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using TransactionAggregation.Api;

namespace TransactionAggregation.Tests.TestServer;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Ensure the application uses the test environment
        builder.UseEnvironment("Development");

        return base.CreateHost(builder);
    }
}