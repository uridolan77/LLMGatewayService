using LLMGateway.Core.Exceptions;
using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Routing;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Core.Services;

/// <summary>
/// Service for A/B testing
/// </summary>
public class ABTestingService : IABTestingService
{
    private readonly IABTestingRepository _repository;
    private readonly ILogger<ABTestingService> _logger;
    private readonly Random _random = new();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="repository">A/B testing repository</param>
    /// <param name="logger">Logger</param>
    public ABTestingService(
        IABTestingRepository repository,
        ILogger<ABTestingService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ABTestingExperiment>> GetAllExperimentsAsync(bool includeInactive = false)
    {
        try
        {
            return await _repository.GetAllExperimentsAsync(includeInactive);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get all experiments");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ABTestingExperiment> GetExperimentAsync(string experimentId)
    {
        try
        {
            var experiment = await _repository.GetExperimentByIdAsync(experimentId);
            if (experiment == null)
            {
                throw new NotFoundException($"Experiment with ID {experimentId} not found");
            }

            return experiment;
        }
        catch (Exception ex) when (ex is not NotFoundException)
        {
            _logger.LogError(ex, "Failed to get experiment {ExperimentId}", experimentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ABTestingExperiment> CreateExperimentAsync(ABTestingExperimentCreateRequest request, string userId)
    {
        try
        {
            // Validate the request
            ValidateExperimentRequest(request);

            // Create the experiment
            var experiment = new ABTestingExperiment
            {
                Id = Guid.NewGuid().ToString(),
                Name = request.Name,
                Description = request.Description,
                IsActive = true,
                StartDate = DateTime.UtcNow,
                EndDate = request.EndDate,
                TrafficAllocationPercentage = request.TrafficAllocationPercentage,
                ControlModelId = request.ControlModelId,
                TreatmentModelId = request.TreatmentModelId,
                UserSegments = request.UserSegments ?? new List<string>(),
                Metrics = request.Metrics ?? new List<string> { "latency", "tokenCount", "errorRate" },
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            return await _repository.CreateExperimentAsync(experiment);
        }
        catch (Exception ex) when (ex is not ValidationException)
        {
            _logger.LogError(ex, "Failed to create experiment");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ABTestingExperiment> UpdateExperimentAsync(string experimentId, ABTestingExperimentUpdateRequest request)
    {
        try
        {
            // Get the existing experiment
            var experiment = await GetExperimentAsync(experimentId);

            // Update the experiment properties
            if (request.Name != null)
            {
                experiment.Name = request.Name;
            }

            if (request.Description != null)
            {
                experiment.Description = request.Description;
            }

            if (request.IsActive.HasValue)
            {
                experiment.IsActive = request.IsActive.Value;
            }

            if (request.TrafficAllocationPercentage.HasValue)
            {
                if (request.TrafficAllocationPercentage < 0 || request.TrafficAllocationPercentage > 100)
                {
                    throw new ValidationException("Traffic allocation percentage must be between 0 and 100");
                }

                experiment.TrafficAllocationPercentage = request.TrafficAllocationPercentage.Value;
            }

            if (request.ControlModelId != null)
            {
                experiment.ControlModelId = request.ControlModelId;
            }

            if (request.TreatmentModelId != null)
            {
                experiment.TreatmentModelId = request.TreatmentModelId;
            }

            if (request.UserSegments != null)
            {
                experiment.UserSegments = request.UserSegments;
            }

            if (request.Metrics != null)
            {
                experiment.Metrics = request.Metrics;
            }

            if (request.EndDate.HasValue)
            {
                experiment.EndDate = request.EndDate;
            }

            experiment.UpdatedAt = DateTime.UtcNow;

            return await _repository.UpdateExperimentAsync(experiment);
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ValidationException)
        {
            _logger.LogError(ex, "Failed to update experiment {ExperimentId}", experimentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteExperimentAsync(string experimentId)
    {
        try
        {
            // Check if the experiment exists
            await GetExperimentAsync(experimentId);

            await _repository.DeleteExperimentAsync(experimentId);
        }
        catch (Exception ex) when (ex is not NotFoundException)
        {
            _logger.LogError(ex, "Failed to delete experiment {ExperimentId}", experimentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ABTestingResult>> GetExperimentResultsAsync(string experimentId)
    {
        try
        {
            // Check if the experiment exists
            await GetExperimentAsync(experimentId);

            return await _repository.GetExperimentResultsAsync(experimentId);
        }
        catch (Exception ex) when (ex is not NotFoundException)
        {
            _logger.LogError(ex, "Failed to get results for experiment {ExperimentId}", experimentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ABTestingResult> CreateResultAsync(ABTestingResultCreateRequest request, string userId)
    {
        try
        {
            // Check if the experiment exists
            var experiment = await GetExperimentAsync(request.ExperimentId);

            // Get the user's group assignment
            var group = await GetUserGroupAssignmentAsync(request.ExperimentId, userId);

            // Create the result
            var result = new ABTestingResult
            {
                Id = Guid.NewGuid().ToString(),
                ExperimentId = request.ExperimentId,
                UserId = userId,
                RequestId = request.RequestId,
                Group = group,
                ModelId = group == "control" ? experiment.ControlModelId : experiment.TreatmentModelId,
                Timestamp = DateTime.UtcNow,
                Metrics = request.Metrics
            };

            return await _repository.CreateResultAsync(result);
        }
        catch (Exception ex) when (ex is not NotFoundException)
        {
            _logger.LogError(ex, "Failed to create result for experiment {ExperimentId}", request.ExperimentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<ABTestingExperimentStatistics> GetExperimentStatisticsAsync(string experimentId)
    {
        try
        {
            // Check if the experiment exists
            var experiment = await GetExperimentAsync(experimentId);

            // Get the experiment results
            var results = await _repository.GetExperimentResultsAsync(experimentId);

            // Group results by control and treatment
            var controlResults = results.Where(r => r.Group == "control").ToList();
            var treatmentResults = results.Where(r => r.Group == "treatment").ToList();

            // Calculate statistics for each metric
            var metricStats = new Dictionary<string, MetricStatistics>();

            foreach (var metric in experiment.Metrics)
            {
                var controlValues = controlResults
                    .Where(r => r.Metrics.ContainsKey(metric))
                    .Select(r => r.Metrics[metric])
                    .ToList();

                var treatmentValues = treatmentResults
                    .Where(r => r.Metrics.ContainsKey(metric))
                    .Select(r => r.Metrics[metric])
                    .ToList();

                if (controlValues.Any() && treatmentValues.Any())
                {
                    var controlAvg = controlValues.Average();
                    var treatmentAvg = treatmentValues.Average();
                    var percentageDiff = CalculatePercentageDifference(controlAvg, treatmentAvg);
                    var pValue = CalculatePValue(controlValues, treatmentValues);
                    var isSignificant = pValue < 0.05;

                    metricStats[metric] = new MetricStatistics
                    {
                        MetricName = metric,
                        ControlAverage = controlAvg,
                        TreatmentAverage = treatmentAvg,
                        PercentageDifference = percentageDiff,
                        PValue = pValue,
                        IsStatisticallySignificant = isSignificant
                    };
                }
            }

            return new ABTestingExperimentStatistics
            {
                ExperimentId = experimentId,
                ExperimentName = experiment.Name,
                ControlSampleSize = controlResults.Count,
                TreatmentSampleSize = treatmentResults.Count,
                Metrics = metricStats
            };
        }
        catch (Exception ex) when (ex is not NotFoundException)
        {
            _logger.LogError(ex, "Failed to get statistics for experiment {ExperimentId}", experimentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> AssignUserToGroupAsync(string experimentId, string userId)
    {
        try
        {
            // Check if the experiment exists
            var experiment = await GetExperimentAsync(experimentId);

            // Check if the experiment is active
            if (!experiment.IsActive)
            {
                throw new ValidationException($"Experiment {experimentId} is not active");
            }

            // Check if the experiment has ended
            if (experiment.EndDate.HasValue && experiment.EndDate.Value < DateTime.UtcNow)
            {
                throw new ValidationException($"Experiment {experimentId} has ended");
            }

            // Check if the user is already assigned to a group
            var existingGroup = await _repository.GetUserGroupAssignmentAsync(experimentId, userId);
            if (!string.IsNullOrEmpty(existingGroup))
            {
                return existingGroup;
            }

            // Check if the user is in the target segments
            if (experiment.UserSegments.Any() && !experiment.UserSegments.Contains(userId))
            {
                // User is not in the target segments, assign to control group
                await _repository.SetUserGroupAssignmentAsync(experimentId, userId, "control");
                return "control";
            }

            // Randomly assign the user to a group based on traffic allocation
            var randomValue = _random.Next(1, 101);
            var group = randomValue <= experiment.TrafficAllocationPercentage ? "treatment" : "control";

            // Save the assignment
            await _repository.SetUserGroupAssignmentAsync(experimentId, userId, group);

            return group;
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ValidationException)
        {
            _logger.LogError(ex, "Failed to assign user {UserId} to group for experiment {ExperimentId}", userId, experimentId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<string> GetModelForUserAsync(string requestedModelId, string userId)
    {
        try
        {
            // Get active experiments for the requested model
            var experiments = await _repository.GetActiveExperimentsForModelAsync(requestedModelId);
            if (!experiments.Any())
            {
                // No active experiments for this model, use the requested model
                return requestedModelId;
            }

            // For simplicity, we'll use the first active experiment
            var experiment = experiments.First();

            // Assign the user to a group
            var group = await AssignUserToGroupAsync(experiment.Id, userId);

            // Return the model based on the group
            return group == "control" ? experiment.ControlModelId : experiment.TreatmentModelId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get model for user {UserId} with requested model {RequestedModelId}", userId, requestedModelId);
            // In case of error, fall back to the requested model
            return requestedModelId;
        }
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<ABTestingExperiment>> GetActiveExperimentsForModelAsync(string modelId)
    {
        try
        {
            return await _repository.GetActiveExperimentsForModelAsync(modelId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get active experiments for model {ModelId}", modelId);
            throw;
        }
    }

    #region Helper methods

    private async Task<string> GetUserGroupAssignmentAsync(string experimentId, string userId)
    {
        // Check if the user is already assigned to a group
        var existingGroup = await _repository.GetUserGroupAssignmentAsync(experimentId, userId);
        if (!string.IsNullOrEmpty(existingGroup))
        {
            return existingGroup;
        }

        // If not assigned, assign the user to a group
        return await AssignUserToGroupAsync(experimentId, userId);
    }

    private static void ValidateExperimentRequest(ABTestingExperimentCreateRequest request)
    {
        var errors = new Dictionary<string, string>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors.Add("Name", "Experiment name is required");
        }

        if (string.IsNullOrWhiteSpace(request.ControlModelId))
        {
            errors.Add("ControlModelId", "Control model ID is required");
        }

        if (string.IsNullOrWhiteSpace(request.TreatmentModelId))
        {
            errors.Add("TreatmentModelId", "Treatment model ID is required");
        }

        if (request.TrafficAllocationPercentage < 0 || request.TrafficAllocationPercentage > 100)
        {
            errors.Add("TrafficAllocationPercentage", "Traffic allocation percentage must be between 0 and 100");
        }

        if (request.EndDate.HasValue && request.EndDate.Value <= DateTime.UtcNow)
        {
            errors.Add("EndDate", "End date must be in the future");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException("Invalid experiment request", errors);
        }
    }

    private static double CalculatePercentageDifference(double controlValue, double treatmentValue)
    {
        if (controlValue == 0)
        {
            return treatmentValue == 0 ? 0 : 100;
        }

        return ((treatmentValue - controlValue) / controlValue) * 100;
    }

    private static double CalculatePValue(List<double> controlValues, List<double> treatmentValues)
    {
        // This is a simplified implementation of a t-test
        // In a real implementation, you would use a proper statistical library

        // Calculate means
        var controlMean = controlValues.Average();
        var treatmentMean = treatmentValues.Average();

        // Calculate variances
        var controlVariance = controlValues.Sum(x => Math.Pow(x - controlMean, 2)) / (controlValues.Count - 1);
        var treatmentVariance = treatmentValues.Sum(x => Math.Pow(x - treatmentMean, 2)) / (treatmentValues.Count - 1);

        // Calculate standard error
        var standardError = Math.Sqrt((controlVariance / controlValues.Count) + (treatmentVariance / treatmentValues.Count));

        // Calculate t-statistic
        var tStatistic = Math.Abs(controlMean - treatmentMean) / standardError;

        // Calculate degrees of freedom (simplified)
        var degreesOfFreedom = controlValues.Count + treatmentValues.Count - 2;

        // Convert t-statistic to p-value (simplified)
        // This is a very rough approximation
        var pValue = 1.0 / (1.0 + Math.Exp(0.7 * tStatistic));

        return pValue;
    }

    #endregion
}
