using TransactionAggregation.Domain.Models;

namespace TransactionAggregation.Tests.TestUtils;

public class UnifiedTransactionBuilder
{
    private string _transactionId = Guid.NewGuid().ToString();
    private string _customerId = "cust-default";
    private decimal _amount = 100m;
    private string _currency = "ZAR";
    private DateTime _timestamp = DateTime.UtcNow;
    private string _description = "Test Description";
    private string _category = "General";
    private string _sourceName = "ASource";

    public static UnifiedTransactionBuilder Default() => new();

    public UnifiedTransactionBuilder WithTransactionId(string id)
    {
        _transactionId = id;
        return this;
    }

    public UnifiedTransactionBuilder WithCustomerId(string id)
    {
        _customerId = id;
        return this;
    }

    public UnifiedTransactionBuilder WithAmount(decimal amt)
    {
        _amount = amt;
        return this;
    }

    public UnifiedTransactionBuilder WithCurrency(string cur)
    {
        _currency = cur;
        return this;
    }

    public UnifiedTransactionBuilder WithTimestamp(DateTime ts)
    {
        _timestamp = ts;
        return this;
    }

    public UnifiedTransactionBuilder WithDescription(string desc)
    {
        _description = desc;
        return this;
    }

    public UnifiedTransactionBuilder WithCategory(string cat)
    {
        _category = cat;
        return this;
    }

    public UnifiedTransactionBuilder WithSource(string source)
    {
        _sourceName = source;
        return this;
    }

    public UnifiedTransaction Build()
    {
        return new UnifiedTransaction(
            TransactionId: _transactionId,
            CustomerId: _customerId,
            Amount: _amount,
            Currency: _currency,
            Timestamp: _timestamp,
            Description: _description,
            Category: _category,
            SourceName: _sourceName
        );
    }
}