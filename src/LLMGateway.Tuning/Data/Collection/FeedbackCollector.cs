using System;
using System.Threading.Tasks;
using LLMGateway.Tuning.Core.Interfaces;
using LLMGateway.Tuning.Core.Models;
using LLMGateway.Tuning.Data.Validation;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Tuning.Data.Collection
{
    public class FeedbackCollector : IFeedbackCollector
    {
        private readonly ILogger<FeedbackCollector> _logger;
        private readonly IFeedbackRepository _repository;
        private readonly IUserContextProvider _userContextProvider;
        private readonly FeedbackValidator _validator;

        public FeedbackCollector(
            ILogger<FeedbackCollector> logger,
            IFeedbackRepository repository,
            IUserContextProvider userContextProvider,
            FeedbackValidator validator)
        {
            _logger = logger;
            _repository = repository;
            _userContextProvider = userContextProvider;
            _validator = validator;
        }

        public async Task<string> RecordFeedbackAsync(FeedbackData feedback)
        {
            // Validate feedback
            var validation = _validator.ValidateFeedback(feedback);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Invalid feedback received: {Errors}", string.Join(", ", validation.Errors));
                throw new ArgumentException($"Invalid feedback: {string.Join(", ", validation.Errors)}");
            }

            // Enrich feedback with context
            var userContext = await _userContextProvider.GetUserContextAsync(feedback.UserId);
            var enrichedFeedback = EnrichFeedback(feedback, userContext);
            
            // Store feedback
            var id = await _repository.SaveFeedbackAsync(enrichedFeedback);
            
            _logger.LogInformation("Feedback recorded with ID: {FeedbackId}", id);
            
            // Trigger processing pipeline if threshold reached
            await TriggerProcessingIfNeededAsync();
            
            return id;
        }
        
        private FeedbackData EnrichFeedback(FeedbackData feedback, UserContext context)
        {
            return feedback with
            {
                UserSegment = context.Segment,
                UserContext = context.Preferences,
                Timestamp = DateTime.UtcNow
            };
        }
        
        private async Task TriggerProcessingIfNeededAsync()
        {
            try
            {
                // Check if we've met the threshold for processing
                var recentCount = await _repository.GetFeedbackCountSinceAsync(DateTime.UtcNow.AddDays(-1));
                
                if (recentCount >= 500) // Example threshold
                {
                    _logger.LogInformation("Feedback threshold reached ({Count}). Triggering processing pipeline.", recentCount);
                    // Could use a message queue, event, or direct call
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking feedback threshold");
            }
        }
    }
}
