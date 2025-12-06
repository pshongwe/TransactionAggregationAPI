using Microsoft.Extensions.Logging;

namespace TransactionAggregation.Tests.TestUtils;

/// <summary>
/// Lightweight logger that captures log messages for assertion in tests.
/// </summary>
public sealed class TestLogger<T> : ILogger<T>
{
    private static readonly IDisposable NoopScope = new NoopDisposable();
    private readonly List<LogEntry> _entries = new();

    public IReadOnlyList<LogEntry> Entries => _entries;

    IDisposable ILogger.BeginScope<TState>(TState state)
        => NoopScope;

    bool ILogger.IsEnabled(LogLevel logLevel) => true;

    void ILogger.Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (formatter is null)
            throw new ArgumentNullException(nameof(formatter));

        var message = formatter(state, exception);
        _entries.Add(new LogEntry(logLevel, eventId, message, exception));
    }

    public readonly record struct LogEntry(LogLevel Level, EventId EventId, string Message, Exception? Exception);

    private sealed class NoopDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
