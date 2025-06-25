using System.Text.Json.Serialization;

namespace MonitoringWorker.Models;

/// <summary>
/// Standard API response wrapper
/// </summary>
/// <typeparam name="T">Type of the response data</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates if the request was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Response data
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Error message if the request failed
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Additional error details
    /// </summary>
    public List<string>? Errors { get; set; }

    /// <summary>
    /// Response metadata
    /// </summary>
    public ResponseMetadata? Metadata { get; set; }

    /// <summary>
    /// Timestamp of the response
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Creates a successful response
    /// </summary>
    public static ApiResponse<T> SuccessResult(T data, string? message = null, ResponseMetadata? metadata = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Message = message,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Creates an error response
    /// </summary>
    public static ApiResponse<T> ErrorResult(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }

    /// <summary>
    /// Creates an error response from exception
    /// </summary>
    public static ApiResponse<T> ErrorResult(Exception exception, bool includeStackTrace = false)
    {
        var errors = new List<string> { exception.Message };
        
        if (includeStackTrace && !string.IsNullOrEmpty(exception.StackTrace))
        {
            errors.Add($"StackTrace: {exception.StackTrace}");
        }

        var innerException = exception.InnerException;
        while (innerException != null)
        {
            errors.Add($"Inner: {innerException.Message}");
            innerException = innerException.InnerException;
        }

        return new ApiResponse<T>
        {
            Success = false,
            Message = "An error occurred while processing the request",
            Errors = errors
        };
    }
}

/// <summary>
/// API response without data
/// </summary>
public class ApiResponse : ApiResponse<object>
{
    /// <summary>
    /// Creates a successful response without data
    /// </summary>
    public static ApiResponse SuccessResult(string? message = null)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message ?? "Operation completed successfully"
        };
    }

    /// <summary>
    /// Creates an error response without data
    /// </summary>
    public new static ApiResponse ErrorResult(string message, List<string>? errors = null)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            Errors = errors
        };
    }

    /// <summary>
    /// Creates an error response from exception without data
    /// </summary>
    public new static ApiResponse ErrorResult(Exception exception, bool includeStackTrace = false)
    {
        var errors = new List<string> { exception.Message };
        
        if (includeStackTrace && !string.IsNullOrEmpty(exception.StackTrace))
        {
            errors.Add($"StackTrace: {exception.StackTrace}");
        }

        return new ApiResponse
        {
            Success = false,
            Message = "An error occurred while processing the request",
            Errors = errors
        };
    }
}

/// <summary>
/// Response metadata for additional information
/// </summary>
public class ResponseMetadata
{
    /// <summary>
    /// Total count for paginated responses
    /// </summary>
    public int? TotalCount { get; set; }

    /// <summary>
    /// Current page number
    /// </summary>
    public int? PageNumber { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int? PageSize { get; set; }

    /// <summary>
    /// Total pages
    /// </summary>
    public int? TotalPages { get; set; }

    /// <summary>
    /// Whether there is a next page
    /// </summary>
    public bool? HasNextPage { get; set; }

    /// <summary>
    /// Whether there is a previous page
    /// </summary>
    public bool? HasPreviousPage { get; set; }

    /// <summary>
    /// Request processing time in milliseconds
    /// </summary>
    public long? ProcessingTimeMs { get; set; }

    /// <summary>
    /// API version
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    /// Additional custom metadata
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object>? AdditionalData { get; set; }

    /// <summary>
    /// Creates metadata for paginated response
    /// </summary>
    public static ResponseMetadata ForPagination(int totalCount, int pageNumber, int pageSize)
    {
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        
        return new ResponseMetadata
        {
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = pageNumber < totalPages,
            HasPreviousPage = pageNumber > 1
        };
    }

    /// <summary>
    /// Creates metadata with processing time
    /// </summary>
    public static ResponseMetadata WithProcessingTime(long processingTimeMs)
    {
        return new ResponseMetadata
        {
            ProcessingTimeMs = processingTimeMs
        };
    }
}

/// <summary>
/// Paginated API response
/// </summary>
/// <typeparam name="T">Type of the items</typeparam>
public class PaginatedApiResponse<T> : ApiResponse<List<T>>
{
    /// <summary>
    /// Creates a paginated response
    /// </summary>
    public static PaginatedApiResponse<T> Create(
        List<T> items, 
        int totalCount, 
        int pageNumber, 
        int pageSize,
        string? message = null)
    {
        return new PaginatedApiResponse<T>
        {
            Success = true,
            Data = items,
            Message = message,
            Metadata = ResponseMetadata.ForPagination(totalCount, pageNumber, pageSize)
        };
    }
}
