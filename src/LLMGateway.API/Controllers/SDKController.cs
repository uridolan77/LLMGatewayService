using LLMGateway.Core.Interfaces;
using LLMGateway.Core.Models.SDK;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LLMGateway.API.Controllers;

/// <summary>
/// SDK management controller for Phase 3 capabilities
/// </summary>
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/sdk")]
public class SDKController : ControllerBase
{
    private readonly ISDKManagementService _sdkManagementService;
    private readonly ILogger<SDKController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="sdkManagementService">SDK management service</param>
    /// <param name="logger">Logger</param>
    public SDKController(
        ISDKManagementService sdkManagementService,
        ILogger<SDKController> logger)
    {
        _sdkManagementService = sdkManagementService;
        _logger = logger;
    }

    /// <summary>
    /// Get available SDK languages and versions
    /// </summary>
    /// <returns>Available SDK options</returns>
    [HttpGet("languages")]
    [ProducesResponseType(typeof(IEnumerable<SDKLanguageOption>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAvailableLanguages()
    {
        try
        {
            var languages = await _sdkManagementService.GetAvailableSDKLanguagesAsync();
            return Ok(languages);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get available SDK languages");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Generate SDK for a specific language
    /// </summary>
    /// <param name="request">SDK generation request</param>
    /// <returns>Generated SDK information</returns>
    [HttpPost("generate")]
    [Authorize]
    [ProducesResponseType(typeof(GeneratedSDK), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateSDK([FromBody] GenerateSDKRequest request)
    {
        try
        {
            var sdk = await _sdkManagementService.GenerateSDKAsync(request);
            return Ok(sdk);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate SDK for language {Language}", request.Language);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get SDK documentation
    /// </summary>
    /// <param name="language">Programming language</param>
    /// <param name="version">SDK version</param>
    /// <returns>SDK documentation</returns>
    [HttpGet("documentation/{language}")]
    [ProducesResponseType(typeof(SDKDocumentation), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDocumentation(string language, [FromQuery] string version = "latest")
    {
        try
        {
            var documentation = await _sdkManagementService.GetSDKDocumentationAsync(language, version);
            return Ok(documentation);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SDK documentation for {Language} v{Version}", language, version);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get SDK code examples
    /// </summary>
    /// <param name="language">Programming language</param>
    /// <param name="useCase">Use case category</param>
    /// <returns>Code examples</returns>
    [HttpGet("examples/{language}")]
    [ProducesResponseType(typeof(IEnumerable<CodeExample>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCodeExamples(string language, [FromQuery] string useCase = "general")
    {
        try
        {
            var examples = await _sdkManagementService.GetCodeExamplesAsync(language, useCase);
            return Ok(examples);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get code examples for {Language} use case {UseCase}", language, useCase);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Generate custom API client
    /// </summary>
    /// <param name="request">Custom client request</param>
    /// <returns>Custom API client</returns>
    [HttpPost("custom-client")]
    [Authorize]
    [ProducesResponseType(typeof(CustomAPIClient), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateCustomClient([FromBody] GenerateCustomClientRequest request)
    {
        try
        {
            var client = await _sdkManagementService.GenerateCustomClientAsync(request);
            return Ok(client);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate custom client for language {Language}", request.Language);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get SDK usage analytics
    /// </summary>
    /// <param name="request">SDK analytics request</param>
    /// <returns>SDK usage analytics</returns>
    [HttpPost("analytics")]
    [Authorize(Policy = "AdminAccess")]
    [ProducesResponseType(typeof(SDKUsageAnalytics), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUsageAnalytics([FromBody] SDKAnalyticsRequest request)
    {
        try
        {
            var analytics = await _sdkManagementService.GetSDKUsageAnalyticsAsync(request);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SDK usage analytics");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Validate SDK configuration
    /// </summary>
    /// <param name="request">Validation request</param>
    /// <returns>Validation result</returns>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(SDKValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ValidateConfiguration([FromBody] ValidateSDKRequest request)
    {
        try
        {
            var result = await _sdkManagementService.ValidateSDKConfigurationAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate SDK configuration");
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get SDK changelog
    /// </summary>
    /// <param name="language">Programming language</param>
    /// <param name="fromVersion">From version (optional)</param>
    /// <param name="toVersion">To version (optional)</param>
    /// <returns>SDK changelog</returns>
    [HttpGet("changelog/{language}")]
    [ProducesResponseType(typeof(SDKChangelog), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetChangelog(string language, [FromQuery] string? fromVersion = null, [FromQuery] string? toVersion = null)
    {
        try
        {
            var changelog = await _sdkManagementService.GetSDKChangelogAsync(language, fromVersion, toVersion);
            return Ok(changelog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get SDK changelog for {Language}", language);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Generate migration guide
    /// </summary>
    /// <param name="request">Migration guide request</param>
    /// <returns>Migration guide</returns>
    [HttpPost("migration-guide")]
    [ProducesResponseType(typeof(SDKMigrationGuide), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GenerateMigrationGuide([FromBody] GenerateMigrationGuideRequest request)
    {
        try
        {
            var guide = await _sdkManagementService.GenerateMigrationGuideAsync(request);
            return Ok(guide);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate migration guide for {Language}", request.Language);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get performance benchmarks
    /// </summary>
    /// <param name="language">Programming language</param>
    /// <param name="version">SDK version</param>
    /// <returns>Performance benchmarks</returns>
    [HttpGet("benchmarks/{language}")]
    [ProducesResponseType(typeof(SDKPerformanceBenchmarks), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPerformanceBenchmarks(string language, [FromQuery] string version = "latest")
    {
        try
        {
            var benchmarks = await _sdkManagementService.GetPerformanceBenchmarksAsync(language, version);
            return Ok(benchmarks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get performance benchmarks for {Language} v{Version}", language, version);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Generate interactive playground
    /// </summary>
    /// <param name="request">Playground request</param>
    /// <returns>Playground configuration</returns>
    [HttpPost("playground")]
    [ProducesResponseType(typeof(SDKPlayground), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GeneratePlayground([FromBody] GeneratePlaygroundRequest request)
    {
        try
        {
            var playground = await _sdkManagementService.GeneratePlaygroundAsync(request);
            return Ok(playground);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate playground for {Language}", request.Language);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Get SDK support information
    /// </summary>
    /// <param name="language">Programming language</param>
    /// <returns>Support information</returns>
    [HttpGet("support/{language}")]
    [ProducesResponseType(typeof(SDKSupportInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSupportInfo(string language)
    {
        try
        {
            var supportInfo = await _sdkManagementService.GetSupportInfoAsync(language);
            return Ok(supportInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get support info for {Language}", language);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = ex.Message });
        }
    }
}
