using System.Collections.Generic;
using System.Threading.Tasks;
using LLMGateway.Tuning.Core.Models;

namespace LLMGateway.Tuning.Core.Interfaces
{
    public interface IDataAugmenter
    {
        Task<List<TrainingExample>> AugmentExamplesAsync(List<TrainingExample> examples);
    }
}
