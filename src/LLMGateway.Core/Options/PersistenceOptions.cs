namespace LLMGateway.Core.Options;

/// <summary>
/// Options for persistence
/// </summary>
public class PersistenceOptions
{
    /// <summary>
    /// Whether to use a database
    /// </summary>
    public bool UseDatabase { get; set; } = true;
    
    /// <summary>
    /// Database provider
    /// </summary>
    public string DatabaseProvider { get; set; } = "SQLServer";
    
    /// <summary>
    /// Database connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether to enable migrations
    /// </summary>
    public bool EnableMigrations { get; set; } = true;
    
    /// <summary>
    /// Whether to automatically migrate on startup
    /// </summary>
    public bool AutoMigrateOnStartup { get; set; } = true;
    
    /// <summary>
    /// Whether to enable initial data seeding
    /// </summary>
    public bool EnableSeeding { get; set; } = true;
    
    /// <summary>
    /// Data retention period
    /// </summary>
    public TimeSpan DataRetentionPeriod { get; set; } = TimeSpan.FromDays(90);
}
