using MonitoringWorker.Models;

namespace MonitoringWorker.Services;

/// <summary>
/// Service for performing monitoring checks
/// </summary>
public interface IMonitoringService
{
    /// <summary>
    /// Performs all configured monitoring checks
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of monitoring results</returns>
    Task<IEnumerable<MonitoringResult>> PerformChecksAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Performs a monitoring check for a specific endpoint
    /// </summary>
    /// <param name="endpointName">Name of the endpoint to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Monitoring result for the endpoint</returns>
    Task<MonitoringResult?> PerformCheckAsync(string endpointName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs all configured monitoring checks (alias for PerformChecksAsync)
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Collection of monitoring results</returns>
    Task<List<MonitoringResult>> CheckAllEndpointsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a monitoring check for a specific endpoint by name (alias for PerformCheckAsync)
    /// </summary>
    /// <param name="endpointName">Name of the endpoint to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Monitoring result for the endpoint</returns>
    Task<MonitoringResult?> CheckEndpointByNameAsync(string endpointName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of configured endpoints
    /// </summary>
    /// <returns>List of configured endpoint information</returns>
    List<object> GetConfiguredEndpoints();
}
