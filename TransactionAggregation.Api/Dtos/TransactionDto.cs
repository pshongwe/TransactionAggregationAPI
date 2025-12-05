namespace TransactionAggregation.Api.Dtos;

/// <summary>
/// Represents a normalized customer transaction returned by the API.
/// </summary>
/// <param name="Id">Unique identifier of the transaction.</param>
/// <param name="Amount">Transaction amount as a numeric value.</param>
/// <param name="Date">ISO 8601 formatted date/time of the transaction.</param>
/// <param name="Currency">Currency of the transaction (ISO 4217 code).</param>
/// <param name="Description">Description or merchant string.</param>
/// <param name="Category">Category assigned by the rule engine.</param>
/// <param name="Source">Source system that produced the record.</param>
public sealed record TransactionDto(
    string Id,
    decimal Amount,
    string Date,
    string Currency,
    string Description,
    string Category,
    string Source
);