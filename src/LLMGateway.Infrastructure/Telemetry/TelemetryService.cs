using LLMGateway.Core.Options;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace LLMGateway.Infrastructure.Telemetry;

/// <summary>
/// Telemetry service
/// </summary>
public class TelemetryService : ITelemetryService
{
    private readonly TelemetryClient _telemetryClient;
    private readonly ILogger<TelemetryService> _logger;
    private readonly TelemetryOptions _options;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="telemetryClient">Telemetry client</param>
    /// <param name="logger">Logger</param>
    /// <param name="options">Telemetry options</param>
    public TelemetryService(
        TelemetryClient telemetryClient,
        ILogger<TelemetryService> logger,
        IOptions<TelemetryOptions> options)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;
        _options = options.Value;
    }

    /// <inheritdoc/>
    public void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
    {
        if (!_options.EnableTelemetry)
        {
            return;
        }
        
        _logger.LogDebug("Tracking event {EventName}", eventName);
        _telemetryClient.TrackEvent(eventName, properties, metrics);
    }

    /// <inheritdoc/>
    public void TrackException(Exception exception, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
    {
        if (!_options.EnableTelemetry || !_options.TrackExceptions)
        {
            return;
        }
        
        _logger.LogDebug("Tracking exception {ExceptionType}: {ExceptionMessage}", exception.GetType().Name, exception.Message);
        _telemetryClient.TrackException(exception, properties, metrics);
    }

    /// <inheritdoc/>
    public void TrackDependency(string dependencyTypeName, string dependencyName, string data, DateTimeOffset startTime, TimeSpan duration, bool success)
    {
        if (!_options.EnableTelemetry || !_options.TrackDependencies)
        {
            return;
        }
        
        _logger.LogDebug("Tracking dependency {DependencyType} {DependencyName}", dependencyTypeName, dependencyName);
        _telemetryClient.TrackDependency(dependencyTypeName, dependencyName, data, startTime, duration, success);
    }

    /// <inheritdoc/>
    public IDisposable TrackOperation(string operationName)
    {
        if (!_options.EnableTelemetry || !_options.TrackPerformance)
        {
            return new NullDisposable();
        }
        
        _logger.LogDebug("Starting operation {OperationName}", operationName);
        return new OperationTelemetry(_telemetryClient, operationName);
    }

    private class OperationTelemetry : IDisposable
    {
        private readonly TelemetryClient _telemetryClient;
        private readonly string _operationName;
        private readonly Stopwatch _stopwatch;
        private readonly IOperationHolder<DependencyTelemetry> _operation;

        public OperationTelemetry(TelemetryClient telemetryClient, string operationName)
        {
            _telemetryClient = telemetryClient;
            _operationName = operationName;
            _stopwatch = Stopwatch.StartNew();
            _operation = _telemetryClient.StartOperation<DependencyTelemetry>(_operationName);
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _operation.Telemetry.Duration = _stopwatch.Elapsed;
            _operation.Telemetry.Success = true;
            _telemetryClient.StopOperation(_operation);
        }
    }

    private class NullDisposable : IDisposable
    {
        public void Dispose()
        {
        }
    }
}
