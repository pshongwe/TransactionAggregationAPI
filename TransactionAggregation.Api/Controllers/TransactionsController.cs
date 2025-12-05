using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using TransactionAggregation.Domain.Abstractions;
using TransactionAggregation.Api.Dtos;
using TransactionAggregation.Api.Mapping;

namespace TransactionAggregation.Api.Controllers;

/// <summary>
/// Controller for managing and aggregating customer transactions.
/// </summary>
[ApiController]
[Route("customers")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionAggregationService _svc;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionsController"/> class.
    /// </summary>
    /// <param name="svc">The transaction aggregation service.</param>
    public TransactionsController(ITransactionAggregationService svc)
    {
        _svc = svc;
    }

    /// <summary>
    /// Health check endpoint for the Transaction Aggregation API
    /// </summary>
    /// <description>
    /// Returns a simple readiness message indicating that the Transaction Aggregation API is running and accessible.
    /// This endpoint does not require authentication and can be used to verify API availability.
    /// </description>
    /// <returns>A simple status message confirming the API is operational.</returns>
    /// <response code="200">API is running successfully.</response>
    [HttpGet("")]
    [ProducesResponseType(200)]
    public IActionResult HealthCheck()
        => Ok("Transaction Aggregation API is running");

    /// <summary>
    /// Retrieves all transactions for a customer
    /// </summary>
    /// <description>
    /// Returns a complete list of transactions for the specified customer. Results can be filtered by date range to retrieve transactions within a specific timeframe.
    /// Requires authentication and is subject to rate limiting.
    /// </description>
    /// <param name="customerId">The unique identifier of the customer whose transactions are being retrieved.</param>
    /// <param name="from">Optional. The start date for filtering transactions. Transactions on or after this date will be included.</param>
    /// <param name="to">Optional. The end date for filtering transactions. Transactions on or before this date will be included.</param>
    /// <param name="ct">Cancellation token for the async operation.</param>
    /// <returns>A collection of transaction details for the specified customer.</returns>
    /// <response code="200">Transactions retrieved successfully. Returns a list of TransactionDto objects.</response>
    /// <response code="400">Bad request. The customerId is empty or invalid.</response>
    /// <response code="401">Unauthorized. The request lacks valid authentication credentials.</response>
    /// <response code="429">Too many requests. Rate limit exceeded.</response>
    [HttpGet("{customerId}/transactions")]
    [Authorize]
    [EnableRateLimiting("default")]
    [ProducesResponseType(typeof(IEnumerable<TransactionDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(429)]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetCustomerTransactions(
        string customerId,
        DateTime? from,
        DateTime? to,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            return BadRequest("customerId cannot be empty.");

        var txns = await _svc.GetAllAsync(customerId, from, to, ct);
        return Ok(txns.ToDto());
    }

    /// <summary>
    /// Retrieves a category-level summary of customer transactions
    /// </summary>
    /// <description>
    /// Returns an aggregated summary of all transactions organized by category. The summary includes total amounts per category, transaction counts,
    /// and other relevant statistics. This endpoint provides a high-level overview of customer spending patterns across different categories.
    /// Results can be filtered by date range. Requires authentication and is subject to rate limiting.
    /// </description>
    /// <param name="customerId">The unique identifier of the customer whose transaction summary is being retrieved.</param>
    /// <param name="from">Optional. The start date for filtering transactions. Summary will only include transactions on or after this date.</param>
    /// <param name="to">Optional. The end date for filtering transactions. Summary will only include transactions on or before this date.</param>
    /// <param name="ct">Cancellation token for the async operation.</param>
    /// <returns>A collection of category summaries with aggregated transaction data.</returns>
    /// <response code="200">Summary retrieved successfully. Returns a list of CategorySummaryDto objects.</response>
    /// <response code="400">Bad request. The customerId is empty or invalid.</response>
    /// <response code="401">Unauthorized. The request lacks valid authentication credentials.</response>
    /// <response code="429">Too many requests. Rate limit exceeded.</response>
    [HttpGet("{customerId}/transactions/summary")]
    [Authorize]
    [EnableRateLimiting("default")]
    [ProducesResponseType(typeof(IEnumerable<CategorySummaryDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    [ProducesResponseType(429)]
    public async Task<ActionResult<IEnumerable<CategorySummaryDto>>> GetCustomerSummary(
        string customerId,
        DateTime? from,
        DateTime? to,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            return BadRequest("customerId cannot be empty.");

        var summary = await _svc.GetCategorySummaryAsync(customerId, from, to, ct);
        return Ok(summary);
    }
}