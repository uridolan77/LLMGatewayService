using LLMGateway.Core.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LLMGateway.Infrastructure.Persistence;

/// <summary>
/// Factory for creating DbContext for migrations
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LLMGatewayDbContext>
{
    /// <summary>
    /// Create the DbContext
    /// </summary>
    /// <param name="args">Args</param>
    /// <returns>DbContext</returns>
    public LLMGatewayDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        var persistenceOptions = new PersistenceOptions();
        configuration.GetSection("Persistence").Bind(persistenceOptions);

        var optionsBuilder = new DbContextOptionsBuilder<LLMGatewayDbContext>();

        switch (persistenceOptions.DatabaseProvider.ToLowerInvariant())
        {
            case "sqlserver":
                optionsBuilder.UseSqlServer(persistenceOptions.ConnectionString);
                break;
                
            case "postgresql":
                optionsBuilder.UseNpgsql(persistenceOptions.ConnectionString);
                break;
                
            case "sqlite":
                optionsBuilder.UseSqlite(persistenceOptions.ConnectionString);
                break;
                
            default:
                throw new ArgumentException($"Unsupported database provider: {persistenceOptions.DatabaseProvider}");
        }

        return new LLMGatewayDbContext(optionsBuilder.Options);
    }
}
