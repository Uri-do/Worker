using System.ComponentModel.DataAnnotations;

namespace MonitoringWorker.Configuration;

/// <summary>
/// Configuration options for the monitoring service
/// </summary>
public class MonitoringOptions
{
    /// <summary>
    /// The configuration section name
    /// </summary>
    public const string SectionName = "Monitoring";
    
    /// <summary>
    /// List of endpoints to monitor
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one endpoint must be configured")]
    public List<EndpointConfig> Endpoints { get; set; } = new();
    
    /// <summary>
    /// Default timeout in seconds for HTTP requests
    /// </summary>
    [Range(1, 300, ErrorMessage = "Default timeout must be between 1 and 300 seconds")]
    public int DefaultTimeoutSeconds { get; set; } = 30;
}

/// <summary>
/// Configuration for a monitoring endpoint
/// </summary>
public class EndpointConfig
{
    /// <summary>
    /// Display name for the endpoint
    /// </summary>
    [Required(ErrorMessage = "Endpoint name is required")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Endpoint name must be between 1 and 100 characters")]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// URL to monitor
    /// </summary>
    [Required(ErrorMessage = "Endpoint URL is required")]
    [Url(ErrorMessage = "Endpoint URL must be a valid URL")]
    public string Url { get; set; } = string.Empty;
    
    /// <summary>
    /// Timeout in seconds for this specific endpoint (optional)
    /// </summary>
    [Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds")]
    public int? TimeoutSeconds { get; set; }
    
    /// <summary>
    /// HTTP method to use for the check (default: GET)
    /// </summary>
    public string Method { get; set; } = "GET";
    
    /// <summary>
    /// Expected HTTP status codes that indicate success
    /// </summary>
    public List<int> ExpectedStatusCodes { get; set; } = new() { 200 };
    
    /// <summary>
    /// Additional headers to send with the request
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }
}
