namespace TransactionAggregation.Domain.Models;

public sealed record UnifiedTransaction(
    string TransactionId,
    string CustomerId,
    decimal Amount,
    string Currency,
    DateTime Timestamp,
    string Description,
    string Category,
    string SourceName
);
