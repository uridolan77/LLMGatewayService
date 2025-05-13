using LLMGateway.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace LLMGateway.Infrastructure.Persistence;

/// <summary>
/// Database context for LLM Gateway
/// </summary>
public class LLMGatewayDbContext : DbContext
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="options">Database context options</param>
    public LLMGatewayDbContext(DbContextOptions<LLMGatewayDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Token usage records
    /// </summary>
    public DbSet<TokenUsageRecord> TokenUsageRecords { get; set; } = null!;

    /// <summary>
    /// Provider health records
    /// </summary>
    public DbSet<ProviderHealthRecord> ProviderHealthRecords { get; set; } = null!;

    /// <summary>
    /// Model metrics records
    /// </summary>
    public DbSet<ModelMetricsRecord> ModelMetricsRecords { get; set; } = null!;

    /// <summary>
    /// API keys
    /// </summary>
    public DbSet<ApiKey> ApiKeys { get; set; } = null!;

    /// <summary>
    /// Users
    /// </summary>
    public DbSet<User> Users { get; set; } = null!;

    /// <summary>
    /// Refresh tokens
    /// </summary>
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    /// <summary>
    /// Permissions
    /// </summary>
    public DbSet<Permission> Permissions { get; set; } = null!;

    /// <summary>
    /// Routing decisions
    /// </summary>
    public DbSet<RoutingDecision> RoutingDecisions { get; set; } = null!;

    /// <summary>
    /// Request logs
    /// </summary>
    public DbSet<RequestLog> RequestLogs { get; set; } = null!;

    /// <summary>
    /// Settings
    /// </summary>
    public DbSet<Setting> Settings { get; set; } = null!;

    /// <summary>
    /// Models
    /// </summary>
    public DbSet<Model> Models { get; set; } = null!;

    /// <summary>
    /// Provider configurations
    /// </summary>
    public DbSet<ProviderConfiguration> ProviderConfigurations { get; set; } = null!;

    /// <summary>
    /// Prompt templates
    /// </summary>
    public DbSet<PromptTemplate> PromptTemplates { get; set; } = null!;

    /// <summary>
    /// Prompt template versions
    /// </summary>
    public DbSet<PromptTemplateVersion> PromptTemplateVersions { get; set; } = null!;

    /// <summary>
    /// Conversations
    /// </summary>
    public DbSet<Conversation> Conversations { get; set; } = null!;

    /// <summary>
    /// Conversation messages
    /// </summary>
    public DbSet<ConversationMessage> ConversationMessages { get; set; } = null!;

    /// <summary>
    /// A/B testing experiments
    /// </summary>
    public DbSet<ABTestingExperiment> ABTestingExperiments { get; set; } = null!;

    /// <summary>
    /// A/B testing results
    /// </summary>
    public DbSet<ABTestingResult> ABTestingResults { get; set; } = null!;

    /// <summary>
    /// A/B testing user assignments
    /// </summary>
    public DbSet<ABTestingUserAssignment> ABTestingUserAssignments { get; set; } = null!;

    /// <summary>
    /// Fine-tuning jobs
    /// </summary>
    public DbSet<FineTuningJob> FineTuningJobs { get; set; } = null!;

    /// <summary>
    /// Fine-tuning step metrics
    /// </summary>
    public DbSet<FineTuningStepMetric> FineTuningStepMetrics { get; set; } = null!;

    /// <summary>
    /// Fine-tuning files
    /// </summary>
    public DbSet<FineTuningFile> FineTuningFiles { get; set; } = null!;

    /// <summary>
    /// Fine-tuning file contents
    /// </summary>
    public DbSet<FineTuningFileContent> FineTuningFileContents { get; set; } = null!;

    /// <summary>
    /// Cost records
    /// </summary>
    public DbSet<CostRecord> CostRecords { get; set; } = null!;

    /// <summary>
    /// Budgets
    /// </summary>
    public DbSet<Budget> Budgets { get; set; } = null!;

    /// <summary>
    /// User permissions
    /// </summary>
    public DbSet<UserPermission> UserPermissions { get; set; } = null!;

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure token usage records
        modelBuilder.Entity<TokenUsageRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.ApiKeyId).IsRequired();
            entity.Property(e => e.RequestId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ModelId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RequestType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.PromptTokens).IsRequired();
            entity.Property(e => e.CompletionTokens).IsRequired();
            entity.Property(e => e.TotalTokens).IsRequired();
            entity.Property(e => e.EstimatedCostUsd).HasPrecision(18, 6).IsRequired();

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ApiKeyId);
            entity.HasIndex(e => e.ModelId);
            entity.HasIndex(e => e.Provider);
            entity.HasIndex(e => e.Timestamp);

            entity.HasOne(e => e.User)
                .WithMany(u => u.TokenUsageRecords)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ApiKey)
                .WithMany(a => a.TokenUsageRecords)
                .HasForeignKey(e => e.ApiKeyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure provider health records
        modelBuilder.Entity<ProviderHealthRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Provider).IsRequired();
            entity.Property(e => e.IsAvailable).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.ResponseTimeMs).IsRequired();

            entity.HasIndex(e => e.Provider);
            entity.HasIndex(e => e.Timestamp);
        });

        // Configure model metrics records
        modelBuilder.Entity<ModelMetricsRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.ModelId).IsRequired();
            entity.Property(e => e.Provider).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.RequestCount).IsRequired();
            entity.Property(e => e.SuccessCount).IsRequired();
            entity.Property(e => e.FailureCount).IsRequired();
            entity.Property(e => e.TotalTokens).IsRequired();
            entity.Property(e => e.AverageResponseTimeMs).IsRequired();
            entity.Property(e => e.TotalCostUsd).HasPrecision(18, 6).IsRequired();

            entity.HasIndex(e => e.ModelId);
            entity.HasIndex(e => e.Provider);
            entity.HasIndex(e => e.Timestamp);
        });

        // Configure API keys
        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ExpiresAt);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.Permissions).IsRequired();
            entity.Property(e => e.DailyTokenLimit).IsRequired();
            entity.Property(e => e.MonthlyTokenLimit).IsRequired();

            entity.HasIndex(e => e.Key).IsUnique();
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsActive);

            entity.HasOne(e => e.User)
                .WithMany(u => u.ApiKeys)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure users
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.Property(e => e.PasswordHash).HasMaxLength(250);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);

            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Role);
        });

        // Configure routing decisions
        modelBuilder.Entity<RoutingDecision>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.RequestedModelId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SelectedModelId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Strategy).IsRequired().HasMaxLength(50);
            entity.Property(e => e.WasSuccessful).IsRequired();
            entity.Property(e => e.ResponseTimeMs).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Details).HasMaxLength(4000);
            entity.Property(e => e.RequestContent).HasMaxLength(4000);
            entity.Property(e => e.FallbackReason).HasMaxLength(255);
            entity.Property(e => e.RoutingReason).HasMaxLength(255);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.RequestedModelId);
            entity.HasIndex(e => e.SelectedModelId);
            entity.HasIndex(e => e.Strategy);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.IsFallback);

            entity.HasOne(e => e.User)
                .WithMany(u => u.RoutingDecisions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure request logs
        modelBuilder.Entity<RequestLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.ApiKeyId).IsRequired();
            entity.Property(e => e.Path).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Method).IsRequired().HasMaxLength(10);
            entity.Property(e => e.StatusCode).IsRequired();
            entity.Property(e => e.RequestType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ModelId).HasMaxLength(100);
            entity.Property(e => e.RequestSizeBytes).IsRequired();
            entity.Property(e => e.ResponseSizeBytes).IsRequired();
            entity.Property(e => e.DurationMs).IsRequired();
            entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45);
            entity.Property(e => e.UserAgent).HasMaxLength(500);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            entity.Property(e => e.RequestHeaders).HasMaxLength(2000);
            entity.Property(e => e.RequestBody).HasMaxLength(4000);
            entity.Property(e => e.ResponseHeaders).HasMaxLength(2000);
            entity.Property(e => e.ResponseBody).HasMaxLength(4000);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ApiKeyId);
            entity.HasIndex(e => e.ModelId);
            entity.HasIndex(e => e.StatusCode);
            entity.HasIndex(e => e.Timestamp);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(e => e.ApiKey)
                .WithMany()
                .HasForeignKey(e => e.ApiKeyId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure settings
        modelBuilder.Entity<Setting>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Key).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).HasMaxLength(4000);
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Category).IsRequired().HasMaxLength(50);
            entity.Property(e => e.IsEncrypted).IsRequired();
            entity.Property(e => e.LastModified).IsRequired();
            entity.Property(e => e.ModifiedBy).HasMaxLength(100);

            entity.HasIndex(e => e.Key).IsUnique();
            entity.HasIndex(e => e.Category);
        });

        // Configure models
        modelBuilder.Entity<Model>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(100).IsRequired();
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProviderModelId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ContextWindow).IsRequired();
            entity.Property(e => e.SupportsCompletions).IsRequired();
            entity.Property(e => e.SupportsEmbeddings).IsRequired();
            entity.Property(e => e.SupportsStreaming).IsRequired();
            entity.Property(e => e.SupportsFunctionCalling).IsRequired();
            entity.Property(e => e.SupportsVision).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.CostPer1kPromptTokensUsd).HasPrecision(18, 6).IsRequired();
            entity.Property(e => e.CostPer1kCompletionTokensUsd).HasPrecision(18, 6).IsRequired();

            entity.HasIndex(e => e.Provider);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.Provider, e.ProviderModelId }).IsUnique();
        });

        // Configure prompt templates
        modelBuilder.Entity<PromptTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(10000);
            entity.Property(e => e.Version).IsRequired();
            entity.Property(e => e.TagsJson).HasMaxLength(1000);
            entity.Property(e => e.VariablesJson).HasMaxLength(5000);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.IsPublic).IsRequired();

            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.CreatedBy);
            entity.HasIndex(e => e.IsPublic);
        });

        // Configure prompt template versions
        modelBuilder.Entity<PromptTemplateVersion>(entity =>
        {
            entity.HasKey(e => e.VersionId);
            entity.Property(e => e.VersionId).ValueGeneratedOnAdd();
            entity.Property(e => e.TemplateId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(10000);
            entity.Property(e => e.Version).IsRequired();
            entity.Property(e => e.TagsJson).HasMaxLength(1000);
            entity.Property(e => e.VariablesJson).HasMaxLength(5000);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.IsPublic).IsRequired();

            entity.HasIndex(e => e.TemplateId);
            entity.HasIndex(e => e.Version);
        });

        // Configure conversations
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.MetadataJson).HasMaxLength(4000);
            entity.Property(e => e.ModelId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.SystemPrompt).HasMaxLength(4000);
            entity.Property(e => e.IsArchived).IsRequired();
            entity.Property(e => e.TagsJson).HasMaxLength(1000);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.IsArchived);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.UpdatedAt);
        });

        // Configure conversation messages
        modelBuilder.Entity<ConversationMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.ConversationId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(10000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ModelId).HasMaxLength(100);
            entity.Property(e => e.Provider).HasMaxLength(50);
            entity.Property(e => e.FunctionCallJson).HasMaxLength(4000);
            entity.Property(e => e.MetadataJson).HasMaxLength(4000);

            entity.HasIndex(e => e.ConversationId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Conversation)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure A/B testing experiments
        modelBuilder.Entity<ABTestingExperiment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.TrafficAllocationPercentage).IsRequired();
            entity.Property(e => e.ControlModelId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.TreatmentModelId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UserSegmentsJson).HasMaxLength(4000);
            entity.Property(e => e.MetricsJson).HasMaxLength(4000);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();

            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.CreatedBy);
            entity.HasIndex(e => e.ControlModelId);
            entity.HasIndex(e => e.TreatmentModelId);
        });

        // Configure A/B testing results
        modelBuilder.Entity<ABTestingResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.ExperimentId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RequestId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Group).IsRequired().HasMaxLength(20);
            entity.Property(e => e.ModelId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.MetricsJson).HasMaxLength(4000);

            entity.HasIndex(e => e.ExperimentId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Timestamp);

            entity.HasOne(e => e.Experiment)
                .WithMany(e => e.Results)
                .HasForeignKey(e => e.ExperimentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure A/B testing user assignments
        modelBuilder.Entity<ABTestingUserAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.ExperimentId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Group).IsRequired().HasMaxLength(20);
            entity.Property(e => e.AssignedAt).IsRequired();

            entity.HasIndex(e => new { e.ExperimentId, e.UserId }).IsUnique();

            entity.HasOne(e => e.Experiment)
                .WithMany(e => e.UserAssignments)
                .HasForeignKey(e => e.ExperimentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure fine-tuning jobs
        modelBuilder.Entity<FineTuningJob>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.BaseModelId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.FineTunedModelId).HasMaxLength(100);
            entity.Property(e => e.TrainingFileId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ValidationFileId).HasMaxLength(50);
            entity.Property(e => e.HyperparametersJson).HasMaxLength(4000);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(4000);
            entity.Property(e => e.MetricsJson).HasMaxLength(10000);
            entity.Property(e => e.ProviderJobId).HasMaxLength(100);
            entity.Property(e => e.TagsJson).HasMaxLength(1000);

            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedBy);
            entity.HasIndex(e => e.Provider);
            entity.HasIndex(e => e.BaseModelId);
            entity.HasIndex(e => e.ProviderJobId);
        });

        // Configure fine-tuning step metrics
        modelBuilder.Entity<FineTuningStepMetric>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.JobId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Step).IsRequired();
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Loss).IsRequired();

            entity.HasIndex(e => e.JobId);
            entity.HasIndex(e => e.Step);

            entity.HasOne(e => e.Job)
                .WithMany(j => j.Events)
                .HasForeignKey(e => e.JobId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure fine-tuning files
        modelBuilder.Entity<FineTuningFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Size).IsRequired();
            entity.Property(e => e.Purpose).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProviderFileId).HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20);

            entity.HasIndex(e => e.CreatedBy);
            entity.HasIndex(e => e.Provider);
            entity.HasIndex(e => e.ProviderFileId);
        });

        // Configure fine-tuning file contents
        modelBuilder.Entity<FineTuningFileContent>(entity =>
        {
            entity.HasKey(e => e.FileId);
            entity.Property(e => e.FileId).HasMaxLength(50);
            entity.Property(e => e.Content).IsRequired();

            entity.HasOne(e => e.File)
                .WithOne()
                .HasForeignKey<FineTuningFileContent>(e => e.FileId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure cost records
        modelBuilder.Entity<CostRecord>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.RequestId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ModelId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.OperationType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.InputTokens).IsRequired();
            entity.Property(e => e.OutputTokens).IsRequired();
            entity.Property(e => e.TotalTokens).IsRequired();
            entity.Property(e => e.CostUsd).IsRequired().HasPrecision(18, 6);
            entity.Property(e => e.ProjectId).HasMaxLength(50);
            entity.Property(e => e.TagsJson).HasMaxLength(1000);
            entity.Property(e => e.MetadataJson).HasMaxLength(4000);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Provider);
            entity.HasIndex(e => e.ModelId);
            entity.HasIndex(e => e.OperationType);
            entity.HasIndex(e => e.Timestamp);
            entity.HasIndex(e => e.ProjectId);
        });

        // Configure budgets
        modelBuilder.Entity<Budget>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProjectId).HasMaxLength(50);
            entity.Property(e => e.AmountUsd).IsRequired().HasPrecision(18, 6);
            entity.Property(e => e.StartDate).IsRequired();
            entity.Property(e => e.ResetPeriod).IsRequired();
            entity.Property(e => e.AlertThresholdPercentage).IsRequired();
            entity.Property(e => e.EnforceBudget).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.TagsJson).HasMaxLength(1000);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.ProjectId);
        });

        // Configure provider configurations
        modelBuilder.Entity<ProviderConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.Provider).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ApiKey).HasMaxLength(1000);
            entity.Property(e => e.ApiUrl).IsRequired().HasMaxLength(255);
            entity.Property(e => e.TimeoutSeconds).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.AdditionalConfiguration).HasMaxLength(4000);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.LastModified).IsRequired();

            entity.HasIndex(e => e.Provider).IsUnique();
            entity.HasIndex(e => e.IsActive);
        });

        // Configure user permissions
        modelBuilder.Entity<UserPermission>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.Permission });
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Permission).IsRequired().HasMaxLength(100);
            entity.Property(e => e.IsGranted).IsRequired();
            entity.Property(e => e.GrantedBy).HasMaxLength(50);
            entity.Property(e => e.GrantedAt).IsRequired();

            entity.HasOne(e => e.User)
                .WithMany(u => u.Permissions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.GrantedByUser)
                .WithMany()
                .HasForeignKey(e => e.GrantedBy)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure refresh tokens
        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.Token).IsRequired().HasMaxLength(255);
            entity.Property(e => e.ExpiresAt).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.RevokedAt);
            entity.Property(e => e.CreatedByIp).IsRequired().HasMaxLength(50);
            entity.Property(e => e.RevokedByIp).HasMaxLength(50);
            entity.Property(e => e.ReasonRevoked).HasMaxLength(255);

            entity.HasIndex(e => e.Token).IsUnique();
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
