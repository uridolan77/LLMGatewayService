using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace LLMGateway.Tuning.Security.Authorization
{
    public class ModelAccessPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _defaultPolicyProvider;
        
        public ModelAccessPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            _defaultPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => 
            _defaultPolicyProvider.GetDefaultPolicyAsync();

        public Task<AuthorizationPolicy> GetFallbackPolicyAsync() => 
            _defaultPolicyProvider.GetFallbackPolicyAsync();

        public Task<AuthorizationPolicy> GetPolicyAsync(string policyName)
        {
            if (policyName.StartsWith("ModelAccess.", StringComparison.OrdinalIgnoreCase))
            {
                var permission = policyName.Substring("ModelAccess.".Length);
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .RequireClaim("Permission", permission)
                    .Build();
                
                return Task.FromResult(policy);
            }
            
            return _defaultPolicyProvider.GetPolicyAsync(policyName);
        }
    }
}
