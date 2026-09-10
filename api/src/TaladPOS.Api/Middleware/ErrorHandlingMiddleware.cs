using System.Text.Json;
using TaladPOS.Domain.Members;
using TaladPOS.Domain.Products;

namespace TaladPOS.Api.Middleware;

/// <summary>
/// Maps domain/application exceptions to HTTP status codes so controllers
/// don't need repetitive try/catch blocks. Extend the switch below as new
/// domain exceptions are introduced by later user stories.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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
        catch (InsufficientStockException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, "insufficient_stock", new { ex.ProductId });
        }
        catch (DuplicateBarcodeException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, "duplicate_barcode", new { ex.Barcode });
        }
        catch (DuplicatePhoneNumberException)
        {
            await WriteErrorAsync(context, StatusCodes.Status409Conflict, "phone_number_already_registered", null);
        }
        catch (ArgumentException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status400BadRequest, "invalid_request", new { ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            await WriteErrorAsync(context, StatusCodes.Status404NotFound, "not_found", new { ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception while processing {Path}", context.Request.Path);
            await WriteErrorAsync(context, StatusCodes.Status500InternalServerError, "internal_error", null);
        }
    }

    private static Task WriteErrorAsync(HttpContext context, int statusCode, string error, object? details)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";
        var payload = details is null
            ? new { error }
            : (object)new { error, details };
        return context.Response.WriteAsync(JsonSerializer.Serialize(payload));
    }
}
