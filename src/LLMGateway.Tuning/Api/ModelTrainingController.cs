using System.Threading.Tasks;
using LLMGateway.Tuning.Core.Models;
using LLMGateway.Tuning.Data.Collection;
using LLMGateway.Tuning.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Tuning.Api
{
    [ApiController]
    [Route("api/model-training")]
    public class ModelTrainingController : ControllerBase
    {
        private readonly ILogger<ModelTrainingController> _logger;
        private readonly FeedbackCollector _feedbackCollector;

        public ModelTrainingController(
            ILogger<ModelTrainingController> logger,
            FeedbackCollector feedbackCollector)
        {
            _logger = logger;
            _feedbackCollector = feedbackCollector;
        }

        [HttpPost("feedback")]
        public async Task<IActionResult> SubmitFeedback([FromBody] FeedbackData feedback)
        {
            _logger.LogInformation("Received feedback from user: {UserId}", feedback.UserId);
            
            var id = await _feedbackCollector.RecordFeedbackAsync(feedback);
            
            return Ok(new { id });
        }
    }
}
