using System.Collections.Generic;
using System.Threading.Tasks;

namespace LLMGateway.Tuning.Core.Interfaces
{
    public interface IUserContextProvider
    {
        Task<UserContext> GetUserContextAsync(string userId);
    }

    public class UserContext
    {
        public string Segment { get; set; }
        public List<string> Preferences { get; set; } = new List<string>();
    }
}
