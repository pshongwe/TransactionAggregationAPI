using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TransactionAggregation.Api.Dtos;
using TransactionAggregation.Tests.TestServer;
using TransactionAggregation.Tests.TestUtils;
using Xunit;

namespace TransactionAggregation.Tests.Integration;

public class GetSummaryTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly IConfiguration _configuration;

    public GetSummaryTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _configuration = factory.Services.GetRequiredService<IConfiguration>();
    }

    // Setup Auth
    private async Task<HttpClient> CreateAuthorizedClientAsync()
    {
        var client = _factory.CreateClient();
        var tokenHandler = new TokenAuthHandler(client, _configuration);
        return await tokenHandler.CreateAuthorizedClientAsync();
    }

    [Fact]
    public async Task Summary_Returns_Grouped_Categories()
    {
        var client = await CreateAuthorizedClientAsync();

        var response = await client.GetAsync("/customers/C1/transactions/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var summary = await response.Content.ReadFromJsonAsync<List<CategorySummaryDto>>();

        Assert.NotNull(summary);
        Assert.NotEmpty(summary);

        Assert.All(summary, s => Assert.False(string.IsNullOrWhiteSpace(s.Category)));
        Assert.All(summary, s => Assert.True(s.TransactionCount >= 1));
    }
}