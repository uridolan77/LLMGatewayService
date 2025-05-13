// src/LLMGateway.Core/Services/ProviderHealthService.cs
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Requests;
using LLMGateway.Core.Options;
using LLMGateway.Core.Routing;
using LLMGateway.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace LLMGateway.Core.Services;

public class ProviderHealthService : BackgroundService
{
    private readonly ILLMProviderFactory _providerFactory;
    private readonly IProviderHealthRepository _healthRepository;
    private readonly IOptions<HealthCheckOptions> _healthCheckOptions;
    private readonly ILogger<ProviderHealthService> _logger;
    
    public ProviderHealthService(
        ILLMProviderFactory providerFactory,
        IProviderHealthRepository healthRepository,
        IOptions<HealthCheckOptions> healthCheckOptions,
        ILogger<ProviderHealthService> logger)
    {
        _providerFactory = providerFactory;
        _healthRepository = healthRepository;
        _healthCheckOptions = healthCheckOptions;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Provider Health Service is starting.");
        
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogDebug("Running provider health checks...");
                
                await RunProviderHealthChecksAsync();
                
                await Task.Delay(
                    TimeSpan.FromSeconds(_healthCheckOptions.Value.CheckIntervalSeconds), 
                    stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the service is stopping
            _logger.LogInformation("Provider Health Service is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Provider Health Service.");
        }
    }
    
    private async Task RunProviderHealthChecksAsync()
    {
        var providers = _providerFactory.GetAllProviders();
        
        foreach (var provider in providers)
        {
            try
            {
                _logger.LogDebug("Checking health of provider: {ProviderName}", provider.ProviderName);
                
                var stopwatch = Stopwatch.StartNew();
                var isHealthy = await CheckProviderHealthAsync(provider);
                stopwatch.Stop();
                
                var healthStatus = new ProviderHealthStatus
                {
                    ProviderName = provider.ProviderName,
                    Status = isHealthy ? "Healthy" : "Unhealthy",
                    LatencyMs = (int)stopwatch.ElapsedMilliseconds,
                    LastChecked = DateTime.UtcNow
                };
                
                await _healthRepository.SaveProviderHealthStatusAsync(healthStatus);
                
                _logger.LogInformation("Provider {ProviderName} is {Status} (Latency: {LatencyMs}ms)",
                    provider.ProviderName, 
                    healthStatus.Status,
                    healthStatus.LatencyMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking health of provider {ProviderName}: {ErrorMessage}",
                    provider.ProviderName, ex.Message);
                
                var healthStatus = new ProviderHealthStatus
                {
                    ProviderName = provider.ProviderName,
                    Status = "Error",
                    ErrorMessage = ex.Message,
                    LatencyMs = 0,
                    LastChecked = DateTime.UtcNow
                };
                
                await _healthRepository.SaveProviderHealthStatusAsync(healthStatus);
            }
        }
    }
    
    private async Task<bool> CheckProviderHealthAsync(ILLMProvider provider)
    {
        // Get a test model for this provider
        var testModel = await GetTestModelForProviderAsync(provider);
        
        if (testModel == null)
        {
            _logger.LogWarning("No suitable test model found for provider {ProviderName}", provider.ProviderName);
            return false;
        }
        
        try
        {
            // Create a simple completion request to test provider health
            var request = new CompletionRequest
            {
                Model = testModel.Id,
                Messages = new List<Message>
                {
                    new Message { Role = "user", Content = "Hello" }
                },
                MaxTokens = 5,
                Temperature = 0
            };
            
            var response = await provider.CreateCompletionAsync(request);
            
            return response != null && response.Choices.Count > 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Health check failed for provider {ProviderName}: {ErrorMessage}",
                provider.ProviderName, ex.Message);
            return false;
        }
    }
    
    private async Task<ModelInfo> GetTestModelForProviderAsync(ILLMProvider provider)
    {
        // Try to get a test model from configuration
        var configuredModel = _healthCheckOptions.Value.TestModels
            .FirstOrDefault(m => m.ProviderName.Equals(provider.ProviderName, StringComparison.OrdinalIgnoreCase));
        
        if (configuredModel != null)
        {
            var models = await provider.GetAvailableModelsAsync();
            return models.FirstOrDefault(m => m.Id == configuredModel.ModelId) ?? models.FirstOrDefault();
        }
        
        // Otherwise, get the first available model
        var allModels = await provider.GetAvailableModelsAsync();
        return allModels.FirstOrDefault(m => m.Capabilities.SupportsCompletion);
    }
}

// src/LLMGateway.Core/Options/HealthCheckOptions.cs
namespace LLMGateway.Core.Options;

public class HealthCheckOptions
{
    public bool EnableHealthChecks { get; set; } = true;
    public int CheckIntervalSeconds { get; set; } = 60;
    public List<ProviderTestModel> TestModels { get; set; } = new();
}

public class ProviderTestModel
{
    public string ProviderName { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
}

// src/LLMGateway.Infrastructure/Persistence/Repositories/IProviderHealthRepository.cs
using LLMGateway.Core.Routing;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

public interface IProviderHealthRepository
{
    Task SaveProviderHealthStatusAsync(ProviderHealthStatus healthStatus);
    Task<List<ProviderHealthStatus>> GetAllProviderHealthStatusAsync();
    Task<ProviderHealthStatus> GetProviderHealthStatusAsync(string providerName);
}

// src/LLMGateway.Infrastructure/Persistence/Repositories/ProviderHealthRepository.cs
using LLMGateway.Core.Routing;
using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Infrastructure.Persistence.Repositories;

public class ProviderHealthRepository : IProviderHealthRepository
{
    private readonly LLMGatewayDbContext _dbContext;
    private readonly ILogger<ProviderHealthRepository> _logger;
    
    public ProviderHealthRepository(
        LLMGatewayDbContext dbContext,
        ILogger<ProviderHealthRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    
    public async Task SaveProviderHealthStatusAsync(ProviderHealthStatus healthStatus)
    {
        try
        {
            var entity = new ProviderHealthCheckEntity
            {
                ProviderName = healthStatus.ProviderName,
                Status = healthStatus.Status,
                LatencyMs = healthStatus.LatencyMs,
                ErrorMessage = healthStatus.ErrorMessage,
                Timestamp = DateTime.UtcNow
            };
            
            _dbContext.ProviderHealthChecks.Add(entity);
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving provider health status: {ErrorMessage}", ex.Message);
            // Do not throw, as this is a background task
        }
    }
    
    public async Task<List<ProviderHealthStatus>> GetAllProviderHealthStatusAsync()
    {
        try
        {
            // Group by provider name and get the latest health check for each provider
            var healthChecks = await _dbContext.ProviderHealthChecks
                .GroupBy(h => h.ProviderName)
                .Select(g => g.OrderByDescending(h => h.Timestamp).First())
                .ToListAsync();
            
            return healthChecks.Select(h => new ProviderHealthStatus
            {
                ProviderName = h.ProviderName,
                Status = h.Status,
                LatencyMs = h.LatencyMs,
                ErrorMessage = h.ErrorMessage,
                LastChecked = h.Timestamp
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving provider health statuses: {ErrorMessage}", ex.Message);
            return new List<ProviderHealthStatus>();
        }
    }
    
    public async Task<ProviderHealthStatus> GetProviderHealthStatusAsync(string providerName)
    {
        try
        {
            var healthCheck = await _dbContext.ProviderHealthChecks
                .Where(h => h.ProviderName == providerName)
                .OrderByDescending(h => h.Timestamp)
                .FirstOrDefaultAsync();
            
            if (healthCheck == null)
            {
                return new ProviderHealthStatus
                {
                    ProviderName = providerName,
                    Status = "Unknown",
                    LastChecked = DateTime.MinValue
                };
            }
            
            return new ProviderHealthStatus
            {
                ProviderName = healthCheck.ProviderName,
                Status = healthCheck.Status,
                LatencyMs = healthCheck.LatencyMs,
                ErrorMessage = healthCheck.ErrorMessage,
                LastChecked = healthCheck.Timestamp
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving health status for provider {ProviderName}: {ErrorMessage}", 
                providerName, ex.Message);
            
            return new ProviderHealthStatus
            {
                ProviderName = providerName,
                Status = "Error",
                ErrorMessage = ex.Message,
                LastChecked = DateTime.UtcNow
            };
        }
    }
}
