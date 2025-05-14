using System.Collections.Generic;

namespace LLMGateway.Tuning.Core.Interfaces
{
    public interface IEntityRecognitionService
    {
        List<Entity> RecognizeEntities(string text);
    }

    public class Entity
    {
        public string Text { get; set; }
        public string Type { get; set; }
        public int StartPosition { get; set; }
        public int EndPosition { get; set; }
    }
}
