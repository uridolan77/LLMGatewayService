using System;
using System.Text.RegularExpressions;
using LLMGateway.Tuning.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace LLMGateway.Tuning.Data.Anonymization
{
    public class DataAnonymizer
    {
        private readonly ILogger<DataAnonymizer> _logger;
        private readonly IEntityRecognitionService _entityRecognitionService;
        
        public DataAnonymizer(
            ILogger<DataAnonymizer> logger,
            IEntityRecognitionService entityRecognitionService)
        {
            _logger = logger;
            _entityRecognitionService = entityRecognitionService;
        }
        
        public AnonymizedData AnonymizeText(string text)
        {
            try
            {
                // Replace identified entities with placeholders
                var entities = _entityRecognitionService.RecognizeEntities(text);
                var anonymizedText = text;
                int replacementCount = 0;
                
                foreach (var entity in entities)
                {
                    string placeholder = $"[{entity.Type.ToUpper()}]";
                    anonymizedText = anonymizedText.Replace(entity.Text, placeholder);
                    replacementCount++;
                }
                
                // Replace email addresses
                var emailRegex = new Regex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
                anonymizedText = emailRegex.Replace(anonymizedText, match => {
                    replacementCount++;
                    return "[EMAIL]";
                });
                
                // Replace phone numbers
                var phoneRegex = new Regex(@"\b(?:\+\d{1,3}[-.\s]?)?\(?\d{3}\)?[-.\s]?\d{3}[-.\s]?\d{4}\b");
                anonymizedText = phoneRegex.Replace(anonymizedText, match => {
                    replacementCount++;
                    return "[PHONE]";
                });
                
                // Replace URLs
                var urlRegex = new Regex(@"https?://[^\s]+");
                anonymizedText = urlRegex.Replace(anonymizedText, match => {
                    replacementCount++;
                    return "[URL]";
                });
                
                // Replace IP addresses
                var ipRegex = new Regex(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b");
                anonymizedText = ipRegex.Replace(anonymizedText, match => {
                    replacementCount++;
                    return "[IP]";
                });
                
                _logger.LogInformation("Anonymized text with {Count} replacements", replacementCount);
                
                return new AnonymizedData(text, anonymizedText, replacementCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error anonymizing text");
                return new AnonymizedData(text, text, 0);
            }
        }
    }

    public record AnonymizedData(string OriginalText, string AnonymizedText, int EntityReplacements);
}
