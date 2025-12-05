using System.Text.Json.Nodes;
using TransactionAggregation.Domain.Abstractions;
using TransactionAggregation.Domain.Models;
using TransactionAggregation.Domain.Services;

namespace TransactionAggregation.Api.Adapters;

/// <summary>
/// Transaction source adapter for C Source system.
/// </summary>
public sealed class CSourceAdapter : ITransactionSourceAdapter
{
    /// <summary>
    /// Gets the name of this transaction source.
    /// </summary>
    public string SourceName => "CSource";

    private readonly string _filePath;

    /// <summary>
    /// Initializes a new instance of the <see cref="CSourceAdapter"/> class.
    /// </summary>
    /// <param name="env">The web host environment.</param>
    public CSourceAdapter(IWebHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "MockData", "CSource.json");
    }

    /// <summary>
    /// Fetches and normalizes transactions from C Source for the specified customer.
    /// </summary>
    /// <param name="customerId">The customer identifier.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A collection of unified transactions.</returns>
    public async Task<IReadOnlyList<UnifiedTransaction>> FetchAndNormalizeAsync(
        string customerId,
        CancellationToken ct = default)
    {
        if (!File.Exists(_filePath))
            return Array.Empty<UnifiedTransaction>();

        var json = await File.ReadAllTextAsync(_filePath, ct);
        var root = JsonNode.Parse(json)?.AsObject();
        if (root is null)
            return Array.Empty<UnifiedTransaction>();

        var cust = root["account"]?.GetValue<string>();
        if (!string.Equals(cust, customerId, StringComparison.OrdinalIgnoreCase))
            return Array.Empty<UnifiedTransaction>();

        var entries = root["entries"]?.AsArray();
        if (entries is null)
            return Array.Empty<UnifiedTransaction>();

        var list = new List<UnifiedTransaction>();

        foreach (var entry in entries)
        {
            if (entry is null) continue;

            var txnId = Guid.NewGuid().ToString();
            var amount = entry["amt"]?.GetValue<decimal>() ?? 0m;
            var timestamp = DateTime.Parse(entry["date"]!.GetValue<string>());
            var description = entry["desc"]?.GetValue<string>() ?? string.Empty;

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

        return list;
    }
}