using Microsoft.AspNetCore.Authorization;

namespace LLMGateway.Tuning.Security.Authorization
{
    public class ModelAccessAttribute : AuthorizeAttribute
    {
        public ModelAccessAttribute(ModelPermission permission)
        {
            Policy = $"ModelAccess.{permission}";
        }
    }

    public enum ModelPermission
    {
        View,
        Train,
        Evaluate,
        Deploy,
        Manage
    }
}
