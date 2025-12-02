namespace TransactionAggregation.Api.Dtos;

/// <summary>
/// Represents a normalized customer transaction returned by the API.
/// </summary>
public sealed record TransactionDto(
    /// <summary>Unique identifier of the transaction.</summary>
    string Id,

    /// <summary>Transaction amount as a numeric value.</summary>
    decimal Amount,

    /// <summary>ISO 8601 formatted date/time of the transaction.</summary>
    string Date,

    /// <summary>Currency of the transaction (ISO 4217 code).</summary>
    string Currency,

    /// <summary>Description or merchant string.</summary>
    string Description,

    /// <summary>Category assigned by the rule engine.</summary>
    string Category,

    /// <summary>Source system that produced the record.</summary>
    string Source
);