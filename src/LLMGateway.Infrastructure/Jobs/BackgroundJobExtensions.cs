using LLMGateway.Core.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace LLMGateway.Infrastructure.Jobs;

/// <summary>
/// Extensions for background jobs
/// </summary>
public static class BackgroundJobExtensions
{
    /// <summary>
    /// Add background jobs
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddBackgroundJobs(this IServiceCollection services, IConfiguration configuration)
    {
        var jobOptions = new BackgroundJobOptions();
        configuration.GetSection("BackgroundJobs").Bind(jobOptions);
        
        // Add Quartz
        services.AddQuartz(q =>
        {
            // Register job factory is no longer needed - it's the default
            // q.UseMicrosoftDependencyInjectionJobFactory() is obsolete
            
            // Add token usage report job
            if (jobOptions.EnableTokenUsageReports)
            {
                var tokenUsageReportJobKey = new JobKey("TokenUsageReportJob");
                try
                {
                    q.AddJob<TokenUsageReportJob>(opts => opts.WithIdentity(tokenUsageReportJobKey));
                    q.AddTrigger(opts => opts
                        .ForJob(tokenUsageReportJobKey)
                        .WithIdentity("TokenUsageReportJob-Trigger")
                        .WithCronSchedule(jobOptions.TokenUsageReportSchedule));
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail startup
                    Console.WriteLine($"Invalid cron expression for TokenUsageReportSchedule: {jobOptions.TokenUsageReportSchedule}. Error: {ex.Message}");
                }
            }
            
            // Add provider health check job
            if (jobOptions.EnableProviderHealthChecks)
            {
                var providerHealthCheckJobKey = new JobKey("ProviderHealthCheckJob");
                q.AddJob<ProviderHealthCheckJob>(opts => opts.WithIdentity(providerHealthCheckJobKey));
                q.AddTrigger(opts => opts
                    .ForJob(providerHealthCheckJobKey)
                    .WithIdentity("ProviderHealthCheckJob-Trigger")
                    .WithSimpleSchedule(s => s
                        .WithIntervalInMinutes(jobOptions.ProviderHealthCheckIntervalMinutes)
                        .RepeatForever()));
            }
            
            // Add model metrics aggregation job
            if (jobOptions.EnableModelMetricsAggregation)
            {
                var modelMetricsAggregationJobKey = new JobKey("ModelMetricsAggregationJob");
                try
                {
                    q.AddJob<ModelMetricsAggregationJob>(opts => opts.WithIdentity(modelMetricsAggregationJobKey));
                    q.AddTrigger(opts => opts
                        .ForJob(modelMetricsAggregationJobKey)
                        .WithIdentity("ModelMetricsAggregationJob-Trigger")
                        .WithCronSchedule(jobOptions.ModelMetricsAggregationSchedule));
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail startup
                    Console.WriteLine($"Invalid cron expression for ModelMetricsAggregationSchedule: {jobOptions.ModelMetricsAggregationSchedule}. Error: {ex.Message}");
                }
            }
            
            // Add database maintenance job
            if (jobOptions.EnableDatabaseMaintenance)
            {
                var databaseMaintenanceJobKey = new JobKey("DatabaseMaintenanceJob");
                try
                {
                    q.AddJob<DatabaseMaintenanceJob>(opts => opts.WithIdentity(databaseMaintenanceJobKey));
                    q.AddTrigger(opts => opts
                        .ForJob(databaseMaintenanceJobKey)
                        .WithIdentity("DatabaseMaintenanceJob-Trigger")
                        .WithCronSchedule(jobOptions.DatabaseMaintenanceSchedule));
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail startup
                    Console.WriteLine($"Invalid cron expression for DatabaseMaintenanceSchedule: {jobOptions.DatabaseMaintenanceSchedule}. Error: {ex.Message}");
                }
            }
            
            // Add cost report job
            if (jobOptions.EnableCostReports)
            {
                var costReportJobKey = new JobKey("CostReportJob");
                try
                {
                    q.AddJob<CostReportJob>(opts => opts.WithIdentity(costReportJobKey));
                    q.AddTrigger(opts => opts
                        .ForJob(costReportJobKey)
                        .WithIdentity("CostReportJob-Trigger")
                        .WithCronSchedule(jobOptions.CostReportSchedule));
                }
                catch (Exception ex)
                {
                    // Log the error but don't fail startup
                    Console.WriteLine($"Invalid cron expression for CostReportSchedule: {jobOptions.CostReportSchedule}. Error: {ex.Message}");
                }
            }
        });
        
        // Add Quartz hosted service
        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);
        
        // Register job classes
        services.AddTransient<TokenUsageReportJob>();
        services.AddTransient<ProviderHealthCheckJob>();
        services.AddTransient<ModelMetricsAggregationJob>();
        services.AddTransient<DatabaseMaintenanceJob>();
        services.AddTransient<CostReportJob>();
        
        return services;
    }
}
