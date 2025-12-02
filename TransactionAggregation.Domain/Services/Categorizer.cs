namespace TransactionAggregation.Domain.Services;

/// <summary>
/// Very simple rule-based categorizer.
/// In a real system this could be ML-based or data-driven.
/// </summary>
public static class Categorizer
{
    private static readonly (string Keyword, string Category)[] Rules =
    {
        ("Pick n Pay", "Groceries"),
        ("Checkers",   "Groceries"),
        ("Spar",       "Groceries"),
        ("Uber",       "Transport"),
        ("Bolt",       "Transport"),
        ("Fuel",       "Fuel"),
        ("Petroport",  "Fuel"),
        ("AirTime",    "Airtime"),
        ("MTN",        "Airtime"),
        ("Vodacom",    "Airtime"),
        ("Salary",     "Income"),
        ("PAYROLL",    "Income"),
    };

    public static string Categorize(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return "Other";

        foreach (var (keyword, category) in Rules)
        {
            if (description.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return category;
        }

        return "Other";
    }
}