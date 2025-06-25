using MonitoringWorker.Models;
using System.Diagnostics;
using System.Net;
using System.Text.Json;

namespace MonitoringWorker.Middleware;

/// <summary>
/// Middleware for consistent API response formatting and error handling
/// </summary>
public class ApiResponseMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiResponseMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public ApiResponseMiddleware(
        RequestDelegate next,
        ILogger<ApiResponseMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
    }

    /// <summary>
    /// Processes the HTTP request
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        // Skip middleware for non-API requests
        if (!context.Request.Path.StartsWithSegments("/api"))
        {
            await _next(context);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var originalBodyStream = context.Response.Body;

        try
        {
            using var responseBody = new MemoryStream();
            context.Response.Body = responseBody;

            await _next(context);

            stopwatch.Stop();

            // Handle successful responses
            if (context.Response.StatusCode >= 200 && context.Response.StatusCode < 300)
            {
                await HandleSuccessResponse(context, responseBody, originalBodyStream, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                await HandleErrorResponse(context, responseBody, originalBodyStream, stopwatch.ElapsedMilliseconds);
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            await HandleExceptionAsync(context, ex, originalBodyStream, stopwatch.ElapsedMilliseconds);
        }
        finally
        {
            context.Response.Body = originalBodyStream;
        }
    }

    /// <summary>
    /// Handles successful API responses
    /// </summary>
    private async Task HandleSuccessResponse(
        HttpContext context, 
        MemoryStream responseBody, 
        Stream originalBodyStream, 
        long processingTimeMs)
    {
        responseBody.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(responseBody).ReadToEndAsync();

        // If response is already wrapped in ApiResponse, pass it through
        if (IsAlreadyWrapped(responseText))
        {
            context.Response.Body = originalBodyStream;
            await context.Response.WriteAsync(responseText);
            return;
        }

        // Wrap the response in ApiResponse format
        var apiResponse = new ApiResponse<object>
        {
            Success = true,
            Data = string.IsNullOrEmpty(responseText) ? null : JsonSerializer.Deserialize<object>(responseText),
            Message = GetSuccessMessage(context.Response.StatusCode),
            Metadata = new ResponseMetadata
            {
                ProcessingTimeMs = processingTimeMs,
                Version = "1.0"
            }
        };

        var wrappedResponse = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        });

        context.Response.ContentType = "application/json";
        context.Response.Body = originalBodyStream;
        await context.Response.WriteAsync(wrappedResponse);
    }

    /// <summary>
    /// Handles error responses
    /// </summary>
    private async Task HandleErrorResponse(
        HttpContext context, 
        MemoryStream responseBody, 
        Stream originalBodyStream, 
        long processingTimeMs)
    {
        responseBody.Seek(0, SeekOrigin.Begin);
        var responseText = await new StreamReader(responseBody).ReadToEndAsync();

        var apiResponse = ApiResponse.ErrorResult(
            GetErrorMessage(context.Response.StatusCode),
            string.IsNullOrEmpty(responseText) ? null : new List<string> { responseText }
        );

        apiResponse.Metadata = new ResponseMetadata
        {
            ProcessingTimeMs = processingTimeMs,
            Version = "1.0"
        };

        var wrappedResponse = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        });

        context.Response.ContentType = "application/json";
        context.Response.Body = originalBodyStream;
        await context.Response.WriteAsync(wrappedResponse);

        _logger.LogWarning("API error response: {StatusCode} - {Message}", 
            context.Response.StatusCode, responseText);
    }

    /// <summary>
    /// Handles unhandled exceptions
    /// </summary>
    private async Task HandleExceptionAsync(
        HttpContext context, 
        Exception exception, 
        Stream originalBodyStream, 
        long processingTimeMs)
    {
        _logger.LogError(exception, "Unhandled exception in API request: {Method} {Path}", 
            context.Request.Method, context.Request.Path);

        var apiResponse = ApiResponse.ErrorResult(
            exception, 
            _environment.IsDevelopment()
        );

        apiResponse.Metadata = new ResponseMetadata
        {
            ProcessingTimeMs = processingTimeMs,
            Version = "1.0"
        };

        var response = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = _environment.IsDevelopment()
        });

        context.Response.StatusCode = GetStatusCodeFromException(exception);
        context.Response.ContentType = "application/json";
        context.Response.Body = originalBodyStream;
        await context.Response.WriteAsync(response);
    }

    /// <summary>
    /// Checks if response is already wrapped in ApiResponse format
    /// </summary>
    private static bool IsAlreadyWrapped(string responseText)
    {
        if (string.IsNullOrEmpty(responseText))
            return false;

        try
        {
            using var document = JsonDocument.Parse(responseText);
            return document.RootElement.TryGetProperty("success", out _) ||
                   document.RootElement.TryGetProperty("Success", out _);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets success message based on status code
    /// </summary>
    private static string GetSuccessMessage(int statusCode)
    {
        return statusCode switch
        {
            200 => "Request completed successfully",
            201 => "Resource created successfully",
            202 => "Request accepted for processing",
            204 => "Request completed successfully",
            _ => "Request completed successfully"
        };
    }

    /// <summary>
    /// Gets error message based on status code
    /// </summary>
    private static string GetErrorMessage(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad request - invalid input provided",
            401 => "Unauthorized - authentication required",
            403 => "Forbidden - insufficient permissions",
            404 => "Resource not found",
            409 => "Conflict - resource already exists or is in use",
            422 => "Unprocessable entity - validation failed",
            429 => "Too many requests - rate limit exceeded",
            500 => "Internal server error",
            502 => "Bad gateway",
            503 => "Service unavailable",
            504 => "Gateway timeout",
            _ => "An error occurred while processing the request"
        };
    }

    /// <summary>
    /// Gets appropriate HTTP status code from exception type
    /// </summary>
    private static int GetStatusCodeFromException(Exception exception)
    {
        return exception switch
        {
            ArgumentNullException => 400,
            ArgumentException => 400,
            UnauthorizedAccessException => 401,
            KeyNotFoundException => 404,
            NotImplementedException => 501,
            TimeoutException => 504,
            _ => 500
        };
    }
}
