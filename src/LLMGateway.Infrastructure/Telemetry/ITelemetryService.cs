namespace LLMGateway.Infrastructure.Telemetry;

/// <summary>
/// Interface for telemetry service
/// </summary>
public interface ITelemetryService
{
    /// <summary>
    /// Track an event
    /// </summary>
    /// <param name="eventName">Event name</param>
    /// <param name="properties">Properties</param>
    /// <param name="metrics">Metrics</param>
    void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null);
    
    /// <summary>
    /// Track an exception
    /// </summary>
    /// <param name="exception">Exception</param>
    /// <param name="properties">Properties</param>
    /// <param name="metrics">Metrics</param>
    void TrackException(Exception exception, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null);
    
    /// <summary>
    /// Track a dependency
    /// </summary>
    /// <param name="dependencyTypeName">Dependency type name</param>
    /// <param name="dependencyName">Dependency name</param>
    /// <param name="data">Data</param>
    /// <param name="startTime">Start time</param>
    /// <param name="duration">Duration</param>
    /// <param name="success">Success</param>
    void TrackDependency(string dependencyTypeName, string dependencyName, string data, DateTimeOffset startTime, TimeSpan duration, bool success);
    
    /// <summary>
    /// Track an operation
    /// </summary>
    /// <param name="operationName">Operation name</param>
    /// <returns>Disposable operation</returns>
    IDisposable TrackOperation(string operationName);
}
