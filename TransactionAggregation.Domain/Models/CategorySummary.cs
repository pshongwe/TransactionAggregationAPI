namespace TransactionAggregation.Domain.Models;

public record CategorySummary(
    string Category,
    decimal TotalAmount,
    int TransactionCount
);