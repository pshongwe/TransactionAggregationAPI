using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace TransactionAggregation.Tests.TestUtils;

public class TokenAuthHandler
{
    private readonly HttpClient _client;
    private readonly IConfiguration _configuration;

    public TokenAuthHandler(HttpClient client, IConfiguration configuration)
    {
        _client = client;
        _configuration = configuration;
    }

    public async Task<HttpClient> CreateAuthorizedClientAsync()
    {
        var token = await GetJwtTokenAsync();
        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return _client;
    }

    private async Task<string> GetJwtTokenAsync()
    {
        var username = _configuration["Auth:Username"];
        var password = _configuration["Auth:Password"];

        var response = await _client.PostAsJsonAsync("/auth/token", new
        {
            username,
            password
        });

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return payload!.Token;
    }

    private class TokenResponse
    {
        public string Token { get; set; } = string.Empty;
    }
}