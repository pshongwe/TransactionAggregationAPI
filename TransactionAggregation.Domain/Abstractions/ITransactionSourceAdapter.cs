using TransactionAggregation.Domain.Models;

namespace TransactionAggregation.Domain.Abstractions;

/// <summary>
/// Represents a single upstream source system.
/// This adapter knows how to fetch and normalize any raw data into UnifiedTransaction.
/// </summary>
public interface ITransactionSourceAdapter
{
    string SourceName { get; }

    Task<IReadOnlyList<UnifiedTransaction>> FetchAndNormalizeAsync(
        string customerId,
        CancellationToken ct = default
    );
}