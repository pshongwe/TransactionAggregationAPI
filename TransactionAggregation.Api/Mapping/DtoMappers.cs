using TransactionAggregation.Api.Dtos;
using TransactionAggregation.Domain.Models;

namespace TransactionAggregation.Api.Mapping;

/// <summary>
/// Provides mapping functionality for converting domain models to DTOs.
/// </summary>
public static class DtoMappers
{
    /// <summary>
    /// Converts a UnifiedTransaction to a TransactionDto.
    /// </summary>
    /// <param name="t">The unified transaction to convert.</param>
    /// <returns>A TransactionDto representation of the transaction.</returns>
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

    /// <summary>
    /// Converts a collection of UnifiedTransactions to a collection of TransactionDtos.
    /// </summary>
    /// <param name="txns">The collection of unified transactions to convert.</param>
    /// <returns>An enumerable of TransactionDto representations.</returns>
    public static IEnumerable<TransactionDto> ToDto(this IEnumerable<UnifiedTransaction> txns)
        => txns.Select(t => t.ToDto());
}