namespace TransactionAggregation.Api.Dtos;

/// <summary>
/// Represents a summary of transactions aggregated by category.
/// </summary>
public sealed record CategorySummaryDto(
    string Category,
    decimal TotalAmount,
    int TransactionCount
);