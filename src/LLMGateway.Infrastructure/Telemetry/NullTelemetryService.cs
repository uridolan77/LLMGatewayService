namespace LLMGateway.Infrastructure.Telemetry;

/// <summary>
/// Null telemetry service
/// </summary>
public class NullTelemetryService : ITelemetryService
{
    /// <inheritdoc/>
    public void TrackEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
    {
        // Do nothing
    }

    /// <inheritdoc/>
    public void TrackException(Exception exception, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
    {
        // Do nothing
    }

    /// <inheritdoc/>
    public void TrackDependency(string dependencyTypeName, string dependencyName, string data, DateTimeOffset startTime, TimeSpan duration, bool success)
    {
        // Do nothing
    }

    /// <inheritdoc/>
    public IDisposable TrackOperation(string operationName)
    {
        return new NullDisposable();
    }

    private class NullDisposable : IDisposable
    {
        public void Dispose()
        {
            // Do nothing
        }
    }
}
