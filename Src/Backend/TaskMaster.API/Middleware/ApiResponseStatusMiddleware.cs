using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using TaskMaster.API.Models.Common;

namespace TaskMaster.API.Middleware;

public class ApiResponseStatusMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiResponseStatusMiddleware> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiResponseStatusMiddleware(
        RequestDelegate next,
        ILogger<ApiResponseStatusMiddleware> logger,
        IOptions<JsonOptions> jsonOptions)
    {
        _next = next;
        _logger = logger;
        _jsonOptions = jsonOptions.Value.JsonSerializerOptions;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        if (context.Response.HasStarted) return;

        if (context.Response.StatusCode == StatusCodes.Status401Unauthorized)
        {
            _logger.LogWarning("Request to {Path} was unauthorized", context.Request.Path);
            var unauthorized = ApiResponse<object?>.Fail("Unauthorized. A valid access token is required.");
            context.Response.ContentType = "application/json";
            await JsonSerializer.SerializeAsync(context.Response.Body, unauthorized, _jsonOptions);
        }
        else if (context.Response.StatusCode == StatusCodes.Status403Forbidden)
        {
            _logger.LogWarning("Request to {Path} was forbidden", context.Request.Path);
            var forbidden = ApiResponse<object?>.Fail("Forbidden. The token is missing a required scope for this endpoint.");
            context.Response.ContentType = "application/json";
            await JsonSerializer.SerializeAsync(context.Response.Body, forbidden, _jsonOptions);
        }
    }
}