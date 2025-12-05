using TransactionAggregation.Domain.Services;
using Xunit;

namespace TransactionAggregation.Tests.Domain;

public class CategorizerTests
{
    [Fact]
    public void Categorize_Returns_Other_For_Blank_Description()
    {
        var result = Categorizer.Categorize(string.Empty);
        Assert.Equal("Other", result);
    }

    [Fact]
    public void Categorize_Returns_Rule_Match()
    {
        var result = Categorizer.Categorize("Pick n Pay Mall");
        Assert.Equal("Groceries", result);
    }

    [Fact]
    public void Categorize_Defaults_To_Other_When_No_Rules_Match()
    {
        var result = Categorizer.Categorize("Unknown Merchant");
        Assert.Equal("Other", result);
    }
}