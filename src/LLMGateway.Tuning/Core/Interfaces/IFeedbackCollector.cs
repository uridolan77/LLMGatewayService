using System.Threading.Tasks;
using LLMGateway.Tuning.Core.Models;

namespace LLMGateway.Tuning.Core.Interfaces
{
    public interface IFeedbackCollector
    {
        Task<string> RecordFeedbackAsync(FeedbackData feedback);
    }
}
