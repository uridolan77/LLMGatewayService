using LLMGateway.Tuning.Core.Interfaces;
using LLMGateway.Tuning.Data.Collection;
using LLMGateway.Tuning.Data.Validation;
using LLMGateway.Tuning.Data.Anonymization;
using LLMGateway.Tuning.Security.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace LLMGateway.Tuning
{
    public static class ServiceRegistration
    {
        public static IServiceCollection AddLLMTuningServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add validators and utilities
            services.AddSingleton<FeedbackValidator>();
            services.AddSingleton<DataAnonymizer>();

            // Add repositories and data access
            // In a real implementation, you would add your actual repository implementations here
            
            // Add collection and preparation services
            services.AddScoped<FeedbackCollector>();
            
            // Add security services
            services.AddSingleton<IAuthorizationPolicyProvider, ModelAccessPolicyProvider>();
            
            // Configure HTTP clients for model adapters
            services.AddHttpClient<Training.Adapters.OpenAiAdapter>();
            
            // Configure options
            services.Configure<Training.Adapters.OpenAiOptions>(configuration.GetSection("OpenAI"));
            
            return services;
        }
    }
}
