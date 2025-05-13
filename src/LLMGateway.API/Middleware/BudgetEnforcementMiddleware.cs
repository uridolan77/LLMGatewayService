using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.Completion;
using LLMGateway.Core.Models.Embedding;
using LLMGateway.Core.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace LLMGateway.API.Middleware;

/// <summary>
/// Middleware for enforcing budget limits
/// </summary>
public class BudgetEnforcementMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<BudgetEnforcementMiddleware> _logger;
    private readonly GlobalOptions _globalOptions;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="next">Next middleware</param>
    /// <param name="logger">Logger</param>
    /// <param name="globalOptions">Global options</param>
    public BudgetEnforcementMiddleware(
        RequestDelegate next,
        ILogger<BudgetEnforcementMiddleware> logger,
        IOptions<GlobalOptions> globalOptions)
    {
        _next = next;
        _logger = logger;
        _globalOptions = globalOptions.Value;
    }

    /// <summary>
    /// Invoke the middleware
    /// </summary>
    /// <param name="context">HTTP context</param>
    /// <param name="costManagementService">Cost management service</param>
    /// <param name="modelService">Model service</param>
    /// <param name="tokenCounterService">Token counter service</param>
    /// <returns>Task</returns>
    public async Task InvokeAsync(
        HttpContext context,
        ICostManagementService costManagementService,
        IModelService modelService,
        ITokenCounterService tokenCounterService)
    {
        // Skip if budget enforcement is disabled
        if (!_globalOptions.EnableBudgetEnforcement)
        {
            await _next(context);
            return;
        }

        // Only check budget for POST requests to completion and embedding endpoints
        var path = context.Request.Path.Value?.ToLowerInvariant();
        if (context.Request.Method != "POST" ||
            (path?.Contains("/v1/completions") != true &&
             path?.Contains("/v1/chat/completions") != true &&
             path?.Contains("/v1/embeddings") != true))
        {
            await _next(context);
            return;
        }

        // Get the user ID from the context
        var userId = context.User?.Identity?.Name ?? "anonymous";

        try
        {
            // Enable buffering so we can read the request body multiple times
            context.Request.EnableBuffering();

            // Read the request body
            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            // Estimate the cost
            decimal estimatedCostUsd = 0;
            string? projectId = null;

            if (path?.Contains("/v1/completions") == true || path?.Contains("/v1/chat/completions") == true)
            {
                // Parse the completion request
                var request = JsonSerializer.Deserialize<CompletionRequest>(requestBody);
                if (request != null)
                {
                    projectId = request.ProjectId;

                    // Count tokens
                    var inputTokens = await tokenCounterService.CountTokensAsync(request);
                    var outputTokens = request.MaxTokens ?? 256; // Use default if not specified

                    // Estimate cost
                    estimatedCostUsd = await costManagementService.EstimateCompletionCostAsync(
                        "OpenAI", // Default provider, will be overridden by routing
                        request.ModelId,
                        inputTokens,
                        outputTokens);
                }
            }
            else if (path?.Contains("/v1/embeddings") == true)
            {
                // Parse the embedding request
                var request = JsonSerializer.Deserialize<EmbeddingRequest>(requestBody);
                if (request != null)
                {
                    projectId = request.ProjectId;

                    // Count tokens
                    var inputTokens = await tokenCounterService.CountTokensAsync(request.Input.ToString() ?? string.Empty);

                    // Estimate cost
                    estimatedCostUsd = await costManagementService.EstimateEmbeddingCostAsync(
                        "OpenAI", // Default provider, will be overridden by routing
                        request.ModelId,
                        inputTokens);
                }
            }

            // Check if the operation is within budget
            if (estimatedCostUsd > 0)
            {
                var isWithinBudget = await costManagementService.IsWithinBudgetAsync(userId, projectId, estimatedCostUsd);
                if (!isWithinBudget)
                {
                    _logger.LogWarning("Budget exceeded for user {UserId} and project {ProjectId}. Estimated cost: {EstimatedCostUsd}",
                        userId, projectId, estimatedCostUsd);

                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    context.Response.ContentType = "application/json";

                    var response = new
                    {
                        error = new
                        {
                            message = "Budget limit exceeded. Please contact your administrator.",
                            type = "budget_exceeded",
                            param = (string?)null,
                            code = "budget_exceeded"
                        }
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in budget enforcement middleware");
            // Continue with the request even if budget enforcement fails
        }
        finally
        {
            // Reset the request body position
            context.Request.Body.Position = 0;
        }

        await _next(context);
    }
}
