using System.Net;
using FluentAssertions;
using TransactionAggregation.Tests.TestServer;
using Xunit;

namespace TransactionAggregation.Tests.Integration;

public class HealthCheckTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public HealthCheckTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RootEndpoint_ReturnsHealthyMessage()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/customers/C1");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Transaction Aggregation API is running", body);
    }
}