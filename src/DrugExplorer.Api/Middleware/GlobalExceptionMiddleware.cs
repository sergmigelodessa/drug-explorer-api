using System.Text.Json;
using DrugExplorer.Api.Models;
using DrugExplorer.Domain.Exceptions;

namespace DrugExplorer.Api.Middleware;

public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
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
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var response = context.Response;
        response.ContentType = "application/json";

        var errorResponse = new ErrorResponse
        {
            Timestamp = DateTime.UtcNow,
            TraceId = context.TraceIdentifier
        };

        switch (exception)
        {
            case ExternalApiException externalApiEx:
                response.StatusCode = externalApiEx.HttpStatusCode ?? 502;
                errorResponse.ErrorCode = "EXTERNAL_API_ERROR";
                errorResponse.Message = externalApiEx.Message;
                errorResponse.Details = $"API: {externalApiEx.ApiName}";
                break;

            case SearchProcessingException searchEx:
                response.StatusCode = 400;
                errorResponse.ErrorCode = "SEARCH_PROCESSING_ERROR";
                errorResponse.Message = searchEx.Message;
                errorResponse.Details = $"Step: {searchEx.ProcessingStep}, Query: {searchEx.Query}";
                break;

            case ArgumentException argEx:
                response.StatusCode = 400;
                errorResponse.ErrorCode = "INVALID_ARGUMENT";
                errorResponse.Message = argEx.Message;
                break;

            default:
                response.StatusCode = 500;
                errorResponse.ErrorCode = "INTERNAL_SERVER_ERROR";
                errorResponse.Message = "An unexpected error occurred";
                errorResponse.Details = exception.Message;
                break;
        }

        return response.WriteAsJsonAsync(errorResponse);
    }
}

public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}
