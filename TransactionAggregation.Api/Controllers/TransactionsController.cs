using Microsoft.AspNetCore.Mvc;
using TransactionAggregation.Domain.Abstractions;
using TransactionAggregation.Api.Dtos;
using TransactionAggregation.Api.Mapping;

namespace TransactionAggregation.Api.Controllers;

[ApiController]
[Route("customers/{customerId}")]
public class TransactionsController : ControllerBase
{
    private readonly ITransactionAggregationService _svc;

    public TransactionsController(ITransactionAggregationService svc)
    {
        _svc = svc;
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<IEnumerable<TransactionDto>>> GetCustomerTransactions(
        string customerId,
        DateTime? from,
        DateTime? to,
        CancellationToken ct)
    {
        var txns = await _svc.GetAllAsync(customerId, from, to, ct);
        return Ok(txns.ToDto());
    }

    [HttpGet("transactions/summary")]
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


    [HttpGet("")]
    public IActionResult HealthCheck()
        => Ok("Transaction Aggregation API is running");
}