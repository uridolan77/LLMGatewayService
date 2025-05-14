using System.Collections.Generic;
using LLMGateway.Tuning.Core.Enums;
using LLMGateway.Tuning.Core.Models;

namespace LLMGateway.Tuning.Data.Validation
{
    public class FeedbackValidator
    {
        public ValidationResult ValidateFeedback(FeedbackData feedback)
        {
            var errors = new List<string>();

            if (string.IsNullOrWhiteSpace(feedback.OriginalPrompt))
                errors.Add("Original prompt cannot be empty");
            
            if (string.IsNullOrWhiteSpace(feedback.ModelResponse))
                errors.Add("Model response cannot be empty");
            
            if (feedback.FeedbackType == FeedbackType.ManualCorrection && 
                string.IsNullOrWhiteSpace(feedback.CorrectedResponse))
                errors.Add("Manual correction feedback requires a corrected response");

            if (feedback.SatisfactionScore < -1 || feedback.SatisfactionScore > 1)
                errors.Add("Satisfaction score must be between -1 and 1");

            return new ValidationResult(
                errors.Count == 0,
                errors);
        }
    }

    public record ValidationResult(bool IsValid, List<string> Errors);
}
