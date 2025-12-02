using TransactionAggregation.Domain.Models;

namespace TransactionAggregation.Domain.Abstractions;

public interface ITransactionAggregationService
{
    Task<IReadOnlyList<UnifiedTransaction>> GetAllAsync(
        string customerId,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default
    );
}