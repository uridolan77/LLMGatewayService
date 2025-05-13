using LLMGateway.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LLMGateway.API.Services;

/// <summary>
/// Background service to sync fine-tuning job statuses
/// </summary>
public class FineTuningJobSyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FineTuningJobSyncService> _logger;
    private readonly TimeSpan _syncInterval = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="serviceProvider">Service provider</param>
    /// <param name="logger">Logger</param>
    public FineTuningJobSyncService(
        IServiceProvider serviceProvider,
        ILogger<FineTuningJobSyncService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Fine-tuning job sync service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Syncing fine-tuning job statuses");

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var fineTuningService = scope.ServiceProvider.GetRequiredService<IFineTuningService>();
                
                await fineTuningService.SyncAllJobsStatusAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while syncing fine-tuning job statuses");
            }

            _logger.LogInformation("Fine-tuning job sync completed. Waiting for next sync interval");

            try
            {
                await Task.Delay(_syncInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Ignore task cancellation exceptions
                break;
            }
        }

        _logger.LogInformation("Fine-tuning job sync service is stopping");
    }
}
