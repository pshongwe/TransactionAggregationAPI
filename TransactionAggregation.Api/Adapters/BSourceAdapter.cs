using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;
using TransactionAggregation.Domain.Abstractions;
using TransactionAggregation.Domain.Models;
using TransactionAggregation.Domain.Services;

namespace TransactionAggregation.Api.Adapters;

/// <summary>
/// Transaction source adapter for B Source system.
/// </summary>
public sealed class BSourceAdapter : ITransactionSourceAdapter
{
    /// <summary>
    /// Gets the name of this transaction source.
    /// </summary>
    public string SourceName => "BSource";

    private readonly string _filePath;
    private readonly ILogger<BSourceAdapter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="BSourceAdapter"/> class.
    /// </summary>
    /// <param name="env">The web host environment.</param>
    /// <param name="logger">The logger.</param>
    public BSourceAdapter(IWebHostEnvironment env, ILogger<BSourceAdapter> logger)
    {
        _filePath = Path.Combine(env.ContentRootPath, "MockData", "BSource.json");
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Fetches and normalizes transactions from B Source for the specified customer.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A collection of unified transactions.</returns>
    public async Task<IReadOnlyList<UnifiedTransaction>> FetchAndNormalizeAsync(
        string customerId,
        CancellationToken ct = default)
    {
        if (!File.Exists(_filePath))
        {
            _logger.LogWarning("BSource data file missing at {FilePath}", _filePath);
            return Array.Empty<UnifiedTransaction>();
        }

        var json = await File.ReadAllTextAsync(_filePath, ct);
        var root = JsonNode.Parse(json)?.AsArray();
        if (root is null)
        {
            _logger.LogWarning("BSource payload at {FilePath} was empty or invalid JSON", _filePath);
            return Array.Empty<UnifiedTransaction>();
        }

        var list = new List<UnifiedTransaction>();

        foreach (var node in root)
        {
            if (node is null) continue;

            var cust = node["customer"]?.GetValue<string>();
            if (!string.Equals(cust, customerId, StringComparison.OrdinalIgnoreCase))
                continue;

            var txnId = node["id"]?.GetValue<string>() ?? Guid.NewGuid().ToString();
            var amount = node["value"]?.GetValue<decimal>() ?? 0m;
            var timestamp = DateTime.Parse(node["timestamp"]!.GetValue<string>());
            var description = node["merchant"]?.GetValue<string>() ?? string.Empty;

            list.Add(new UnifiedTransaction(
                TransactionId: txnId,
                CustomerId: cust!,
                Amount: amount,
                Currency: "ZAR",
                Timestamp: timestamp,
                Description: description,
                Category: Categorizer.Categorize(description),
                SourceName: SourceName
            ));
        }

        _logger.LogInformation("BSource returned {Count} transactions for {CustomerId}", list.Count, customerId);
        return list;
    }
}