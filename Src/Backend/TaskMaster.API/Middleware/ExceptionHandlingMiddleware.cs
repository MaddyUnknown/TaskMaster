using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TaskMaster.API.Exceptions;
using TaskMaster.API.Models.Common;

namespace TaskMaster.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IOptions<JsonOptions> jsonOptions)
    {
        _next = next;
        _logger = logger;
        _jsonOptions = jsonOptions.Value.JsonSerializerOptions;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning(ex, "Resource not found: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await WriteErrorResponseAsync(context, ex.Message);
        }
        catch (ValidationException ex)
        {
            _logger.LogWarning(ex, "Validation failed: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteErrorResponseAsync(context, ex.Errors.ToList());
        }
        catch (WorkerInactiveException ex)
        {
            _logger.LogWarning(ex, "Worker inactive: {Message}", ex.Message);
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await WriteErrorResponseAsync(context, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred.");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await WriteErrorResponseAsync(context, "An unexpected error occurred.");
        }
    }

    private async Task WriteErrorResponseAsync(HttpContext context, string error)
    {
        var response = ApiResponse<object?>.Fail(error);
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, response, _jsonOptions);
    }

    private async Task WriteErrorResponseAsync(HttpContext context, IList<string> errors)
    {
        var response = ApiResponse<object?>.Fail(errors);
        context.Response.ContentType = "application/json";
        await JsonSerializer.SerializeAsync(context.Response.Body, response, _jsonOptions);
    }
}
