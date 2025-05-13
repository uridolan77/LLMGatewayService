// src/LLMGateway.Infrastructure/Persistence/Migrations/InitialMigration.cs
using Microsoft.EntityFrameworkCore.Migrations;

namespace LLMGateway.Infrastructure.Persistence.Migrations;

public partial class InitialMigration : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create TokenUsage table
        migrationBuilder.CreateTable(
            name: "TokenUsage",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                ModelId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                PromptTokens = table.Column<int>(type: "int", nullable: false),
                CompletionTokens = table.Column<int>(type: "int", nullable: false),
                RequestType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_TokenUsage", x => x.Id);
            });

        // Create RoutingHistory table
        migrationBuilder.CreateTable(
            name: "RoutingHistory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OriginalModelId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                SelectedModelId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                RoutingStrategy = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                RequestContent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                RequestTokenCount = table.Column<int>(type: "int", nullable: true),
                IsFallback = table.Column<bool>(type: "bit", nullable: false),
                FallbackReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                LatencyMs = table.Column<int>(type: "int", nullable: false),
                Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RoutingHistory", x => x.Id);
            });

        // Create ProviderHealthChecks table
        migrationBuilder.CreateTable(
            name: "ProviderHealthChecks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProviderName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                LatencyMs = table.Column<int>(type: "int", nullable: false),
                ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ProviderHealthChecks", x => x.Id);
            });

        // Create ModelMetrics table
        migrationBuilder.CreateTable(
            name: "ModelMetrics",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ModelId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Provider = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                AverageLatencyMs = table.Column<double>(type: "float", nullable: false),
                SuccessCount = table.Column<int>(type: "int", nullable: false),
                ErrorCount = table.Column<int>(type: "int", nullable: false),
                ThroughputPerMinute = table.Column<double>(type: "float", nullable: false),
                CostPerRequest = table.Column<double>(type: "float", nullable: false),
                LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ModelMetrics", x => x.Id);
            });

        // Create RequestLogs table
        migrationBuilder.CreateTable(
            name: "RequestLogs",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                RequestType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                ModelId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                UserId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                PromptTokens = table.Column<int>(type: "int", nullable: false),
                CompletionTokens = table.Column<int>(type: "int", nullable: false),
                LatencyMs = table.Column<int>(type: "int", nullable: false),
                IsSuccess = table.Column<bool>(type: "bit", nullable: false),
                ErrorMessage = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                RequestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RequestLogs", x => x.Id);
            });

        // Create indices
        migrationBuilder.CreateIndex(
            name: "IX_TokenUsage_UserId",
            table: "TokenUsage",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_TokenUsage_ModelId",
            table: "TokenUsage",
            column: "ModelId");

        migrationBuilder.CreateIndex(
            name: "IX_TokenUsage_Provider",
            table: "TokenUsage",
            column: "Provider");

        migrationBuilder.CreateIndex(
            name: "IX_TokenUsage_Timestamp",
            table: "TokenUsage",
            column: "Timestamp");

        migrationBuilder.CreateIndex(
            name: "IX_RoutingHistory_OriginalModelId",
            table: "RoutingHistory",
            column: "OriginalModelId");

        migrationBuilder.CreateIndex(
            name: "IX_RoutingHistory_SelectedModelId",
            table: "RoutingHistory",
            column: "SelectedModelId");

        migrationBuilder.CreateIndex(
            name: "IX_RoutingHistory_RoutingStrategy",
            table: "RoutingHistory",
            column: "RoutingStrategy");

        migrationBuilder.CreateIndex(
            name: "IX_RoutingHistory_Timestamp",
            table: "RoutingHistory",
            column: "Timestamp");

        migrationBuilder.CreateIndex(
            name: "IX_ProviderHealthChecks_ProviderName",
            table: "ProviderHealthChecks",
            column: "ProviderName");

        migrationBuilder.CreateIndex(
            name: "IX_ProviderHealthChecks_Status",
            table: "ProviderHealthChecks",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_ProviderHealthChecks_Timestamp",
            table: "ProviderHealthChecks",
            column: "Timestamp");

        migrationBuilder.CreateIndex(
            name: "IX_ModelMetrics_ModelId",
            table: "ModelMetrics",
            column: "ModelId");

        migrationBuilder.CreateIndex(
            name: "IX_ModelMetrics_Provider",
            table: "ModelMetrics",
            column: "Provider");

        migrationBuilder.CreateIndex(
            name: "IX_ModelMetrics_LastUpdated",
            table: "ModelMetrics",
            column: "LastUpdated");

        migrationBuilder.CreateIndex(
            name: "IX_RequestLogs_RequestType",
            table: "RequestLogs",
            column: "RequestType");

        migrationBuilder.CreateIndex(
            name: "IX_RequestLogs_ModelId",
            table: "RequestLogs",
            column: "ModelId");

        migrationBuilder.CreateIndex(
            name: "IX_RequestLogs_UserId",
            table: "RequestLogs",
            column: "UserId");

        migrationBuilder.CreateIndex(
            name: "IX_RequestLogs_Timestamp",
            table: "RequestLogs",
            column: "Timestamp");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "TokenUsage");
        migrationBuilder.DropTable(name: "RoutingHistory");
        migrationBuilder.DropTable(name: "ProviderHealthChecks");
        migrationBuilder.DropTable(name: "ModelMetrics");
        migrationBuilder.DropTable(name: "RequestLogs");
    }
}

// src/LLMGateway.Infrastructure/Persistence/DesignTimeDbContextFactory.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace LLMGateway.Infrastructure.Persistence;

/// <summary>
/// This class is used by EF Core tools to create a DbContext for migrations.
/// It's not used at runtime.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LLMGatewayDbContext>
{
    public LLMGatewayDbContext CreateDbContext(string[] args)
    {
        // Build configuration from the appsettings.json file
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var builder = new DbContextOptionsBuilder<LLMGatewayDbContext>();
        var connectionString = configuration.GetSection("Persistence:ConnectionString").Value;

        // Use the correct database provider based on configuration
        var databaseProvider = configuration.GetSection("Persistence:DatabaseProvider").Value?.ToLowerInvariant() ?? "sqlserver";

        switch (databaseProvider)
        {
            case "sqlserver":
                builder.UseSqlServer(connectionString,
                    x => x.MigrationsAssembly("LLMGateway.Infrastructure"));
                break;
            case "postgresql":
                builder.UseNpgsql(connectionString,
                    x => x.MigrationsAssembly("LLMGateway.Infrastructure"));
                break;
            case "sqlite":
                builder.UseSqlite(connectionString,
                    x => x.MigrationsAssembly("LLMGateway.Infrastructure"));
                break;
            default:
                throw new Exception($"Unsupported database provider: {databaseProvider}");
        }

        return new LLMGatewayDbContext(builder.Options);
    }
}
