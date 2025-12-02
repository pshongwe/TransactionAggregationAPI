using TransactionAggregation.Api.Dtos;
using TransactionAggregation.Domain.Models;

namespace TransactionAggregation.Api.Mapping;

public static class DtoMappers
{
    public static TransactionDto ToDto(this UnifiedTransaction t)
        => new(
            Id: t.TransactionId,
            Amount: t.Amount,
            Date: t.Timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
            Currency: t.Currency,
            Description: t.Description,
            Category: t.Category,
            Source: t.SourceName
        );

    public static IEnumerable<TransactionDto> ToDto(this IEnumerable<UnifiedTransaction> txns)
        => txns.Select(t => t.ToDto());
}