using System.Text.Json.Nodes;
using TransactionAggregation.Domain.Abstractions;
using TransactionAggregation.Domain.Models;
using TransactionAggregation.Domain.Services;

namespace TransactionAggregation.Api.Adapters;

public sealed class CSourceAdapter : ITransactionSourceAdapter
{
    public string SourceName => "CSource";

    private readonly string _filePath;

    public CSourceAdapter(IWebHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "MockData", "CSource.json");
    }

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