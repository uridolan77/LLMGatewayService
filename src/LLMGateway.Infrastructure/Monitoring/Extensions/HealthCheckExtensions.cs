using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Text.Json;

namespace LLMGateway.Infrastructure.Monitoring.Extensions;

/// <summary>
/// Extensions for health checks
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Write health check response
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <param name="report">Health report</param>
    /// <returns>Task</returns>
    public static Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                exception = e.Value.Exception?.Message,
                data = e.Value.Data
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        
        return context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
