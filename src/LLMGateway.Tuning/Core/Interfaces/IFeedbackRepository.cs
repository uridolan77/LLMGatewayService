using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LLMGateway.Tuning.Core.Enums;
using LLMGateway.Tuning.Core.Models;

namespace LLMGateway.Tuning.Core.Interfaces
{
    public interface IFeedbackRepository
    {
        Task<string> SaveFeedbackAsync(FeedbackData feedback);
        Task<FeedbackData> GetFeedbackAsync(string id);
        Task<List<FeedbackData>> GetFeedbackForDatasetGenerationAsync(
            DateTime since, 
            int maxRecords = 1000,
            FeedbackType? feedbackType = null);
        Task<int> GetFeedbackCountSinceAsync(DateTime since);
        Task<List<FeedbackData>> GetFeedbackByUserIdAsync(string userId, int limit = 100);
    }
}
