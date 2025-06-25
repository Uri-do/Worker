using Microsoft.Extensions.Options;
using MonitoringWorker.Configuration;
using MonitoringWorker.Models;
using System.Diagnostics;
using System.Net;

namespace MonitoringWorker.Services;

/// <summary>
/// Implementation of the monitoring service
/// </summary>
public class MonitoringService : IMonitoringService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MonitoringService> _logger;
    private readonly MonitoringOptions _options;

    public MonitoringService(
        HttpClient httpClient,
        ILogger<MonitoringService> logger,
        IOptions<MonitoringOptions> options)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<IEnumerable<MonitoringResult>> PerformChecksAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting monitoring checks for {EndpointCount} endpoints", _options.Endpoints.Count);
        
        var tasks = _options.Endpoints.Select(endpoint => 
            CheckEndpointAsync(endpoint, cancellationToken));
        
        var results = await Task.WhenAll(tasks);
        
        _logger.LogInformation("Completed monitoring checks. Results: {HealthyCount} healthy, {UnhealthyCount} unhealthy, {ErrorCount} errors",
            results.Count(r => r.Status == MonitoringStatus.Healthy),
            results.Count(r => r.Status == MonitoringStatus.Unhealthy),
            results.Count(r => r.Status == MonitoringStatus.Error));
        
        return results;
    }

    public async Task<MonitoringResult?> PerformCheckAsync(string endpointName, CancellationToken cancellationToken = default)
    {
        var endpoint = _options.Endpoints.FirstOrDefault(e => 
            string.Equals(e.Name, endpointName, StringComparison.OrdinalIgnoreCase));
        
        if (endpoint == null)
        {
            _logger.LogWarning("Endpoint {EndpointName} not found in configuration", endpointName);
            return null;
        }
        
        return await CheckEndpointAsync(endpoint, cancellationToken);
    }

    private async Task<MonitoringResult> CheckEndpointAsync(EndpointConfig endpoint, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTimeOffset.UtcNow;
        
        try
        {
            _logger.LogDebug("Checking endpoint {EndpointName} at {Url}", endpoint.Name, endpoint.Url);
            
            using var request = new HttpRequestMessage(new HttpMethod(endpoint.Method), endpoint.Url);
            
            // Add custom headers if configured
            if (endpoint.Headers != null)
            {
                foreach (var header in endpoint.Headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
            
            // Set timeout for this specific request
            var timeout = TimeSpan.FromSeconds(endpoint.TimeoutSeconds ?? _options.DefaultTimeoutSeconds);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(timeout);
            
            var response = await _httpClient.SendAsync(request, cts.Token);
            stopwatch.Stop();
            
            var isSuccess = endpoint.ExpectedStatusCodes.Contains((int)response.StatusCode);
            var status = isSuccess ? MonitoringStatus.Healthy : MonitoringStatus.Unhealthy;
            
            var result = new MonitoringResult
            {
                CheckName = endpoint.Name,
                Status = status,
                Message = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}",
                Timestamp = startTime,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Details = new
                {
                    StatusCode = (int)response.StatusCode,
                    ReasonPhrase = response.ReasonPhrase,
                    ResponseTime = stopwatch.ElapsedMilliseconds,
                    Url = endpoint.Url,
                    Method = endpoint.Method,
                    ExpectedStatusCodes = endpoint.ExpectedStatusCodes,
                    IsSuccess = isSuccess
                }
            };
            
            _logger.LogDebug("Endpoint {EndpointName} check completed: {Status} in {Duration}ms", 
                endpoint.Name, status, stopwatch.ElapsedMilliseconds);
            
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("Endpoint {EndpointName} check was cancelled", endpoint.Name);
            
            return new MonitoringResult
            {
                CheckName = endpoint.Name,
                Status = MonitoringStatus.Error,
                Message = "Check was cancelled",
                Timestamp = startTime,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Details = new { Error = "Operation was cancelled", Url = endpoint.Url }
            };
        }
        catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException || !cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("Endpoint {EndpointName} check timed out after {Timeout}ms", 
                endpoint.Name, stopwatch.ElapsedMilliseconds);
            
            return new MonitoringResult
            {
                CheckName = endpoint.Name,
                Status = MonitoringStatus.Error,
                Message = "Request timed out",
                Timestamp = startTime,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Details = new { Error = "Request timed out", Url = endpoint.Url, TimeoutMs = (endpoint.TimeoutSeconds ?? _options.DefaultTimeoutSeconds) * 1000 }
            };
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "HTTP error checking endpoint {EndpointName}", endpoint.Name);
            
            return new MonitoringResult
            {
                CheckName = endpoint.Name,
                Status = MonitoringStatus.Error,
                Message = "HTTP request failed",
                Timestamp = startTime,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Details = new { Error = ex.Message, Url = endpoint.Url }
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Unexpected error checking endpoint {EndpointName}", endpoint.Name);
            
            return new MonitoringResult
            {
                CheckName = endpoint.Name,
                Status = MonitoringStatus.Error,
                Message = "Unexpected error occurred",
                Timestamp = startTime,
                DurationMs = stopwatch.ElapsedMilliseconds,
                Details = new { Error = ex.Message, Type = ex.GetType().Name, Url = endpoint.Url }
            };
        }
    }

    /// <summary>
    /// Performs all configured monitoring checks (alias for PerformChecksAsync)
    /// </summary>
    public async Task<List<MonitoringResult>> CheckAllEndpointsAsync(CancellationToken cancellationToken = default)
    {
        var results = await PerformChecksAsync(cancellationToken);
        return results.ToList();
    }

    /// <summary>
    /// Performs a monitoring check for a specific endpoint by name (alias for PerformCheckAsync)
    /// </summary>
    public async Task<MonitoringResult?> CheckEndpointByNameAsync(string endpointName, CancellationToken cancellationToken = default)
    {
        return await PerformCheckAsync(endpointName, cancellationToken);
    }

    /// <summary>
    /// Gets the list of configured endpoints
    /// </summary>
    public List<object> GetConfiguredEndpoints()
    {
        return _options.Endpoints.Select(e => new
        {
            Name = e.Name,
            Url = e.Url,
            Method = e.Method,
            TimeoutSeconds = e.TimeoutSeconds ?? _options.DefaultTimeoutSeconds,
            ExpectedStatusCodes = e.ExpectedStatusCodes,
            Headers = e.Headers?.Count > 0 ? e.Headers.Keys.ToList() : new List<string>(),
            Enabled = true // All configured endpoints are considered enabled
        }).ToList<object>();
    }
}
