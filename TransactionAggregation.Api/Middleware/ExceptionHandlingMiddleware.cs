using System.Net;
using System.Text.Json;

namespace TransactionAggregation.Api.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

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

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public HttpStatusCode StatusCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
