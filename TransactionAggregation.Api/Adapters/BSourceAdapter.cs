using System.Text.Json.Nodes;
using TransactionAggregation.Domain.Abstractions;
using TransactionAggregation.Domain.Models;
using TransactionAggregation.Domain.Services;

namespace TransactionAggregation.Api.Adapters;

public sealed class BSourceAdapter : ITransactionSourceAdapter
{
    public string SourceName => "BSource";

    private readonly string _filePath;

    public BSourceAdapter(IWebHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "MockData", "BSource.json");
    }

    public async Task<IReadOnlyList<UnifiedTransaction>> FetchAndNormalizeAsync(
        string customerId,
        CancellationToken ct = default)
    {
        if (!File.Exists(_filePath))
            return Array.Empty<UnifiedTransaction>();

        var json = await File.ReadAllTextAsync(_filePath, ct);
        var root = JsonNode.Parse(json)?.AsArray();
        if (root is null)
            return Array.Empty<UnifiedTransaction>();

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

        return list;
    }
}