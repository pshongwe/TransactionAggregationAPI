using System.Net;
using System.Text.Json;

namespace TransactionAggregation.Api.Middleware;

/// <summary>
/// Middleware for handling global exceptions and providing consistent error responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger instance.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>
    /// Invokes the middleware to process the HTTP request and handle exceptions.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred: {ExceptionMessage}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = new ErrorResponse
        {
            Message = "An error occurred while processing your request",
            StatusCode = HttpStatusCode.InternalServerError
        };

        switch (exception)
        {
            case ArgumentNullException:
            case ArgumentException:
                response.StatusCode = HttpStatusCode.BadRequest;
                response.Message = "Invalid request parameters";
                break;
            
            case TimeoutException:
                response.StatusCode = HttpStatusCode.GatewayTimeout;
                response.Message = "Request timeout";
                break;
            
            case OperationCanceledException:
                response.StatusCode = HttpStatusCode.ServiceUnavailable;
                response.Message = "Operation was cancelled";
                break;
            
            default:
                response.StatusCode = HttpStatusCode.InternalServerError;
                response.Message = "An unexpected error occurred";
                break;
        }

        context.Response.StatusCode = (int)response.StatusCode;
        return context.Response.WriteAsJsonAsync(response);
    }
}

/// <summary>
/// Represents an error response returned by the API.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public HttpStatusCode StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the error occurred.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
