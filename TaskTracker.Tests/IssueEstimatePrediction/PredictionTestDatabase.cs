using Microsoft.EntityFrameworkCore;
using Npgsql;
using TaskTracker.Core.src.DataAccess;

namespace TaskTracker.Tests.IssueEstimatePrediction;

internal sealed class PredictionTestDatabase : IAsyncDisposable
{
    private const string DefaultConnectionString =
        "Host=localhost;Port=5434;Database=tasktracker_prediction_tests;Username=postgres;Password=admin";

    private PredictionTestDatabase(string connectionString)
    {
        ConnectionString = connectionString;
    }

    public string ConnectionString { get; }

    public static async Task<PredictionTestDatabase> CreateAsync()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        var baseConnectionString = Environment.GetEnvironmentVariable("TASKTRACKER_TEST_DB_CONNECTION")
            ?? DefaultConnectionString;
        var builder = new NpgsqlConnectionStringBuilder(baseConnectionString);
        var baseDatabaseName = string.IsNullOrWhiteSpace(builder.Database)
            ? "tasktracker_prediction_tests"
            : builder.Database;
        builder.Database = $"{baseDatabaseName}_{Guid.NewGuid():N}";

        var database = new PredictionTestDatabase(builder.ConnectionString);

        try
        {
            await using var dbContext = database.CreateContext();
            await dbContext.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Cannot create the prediction test database. Start PostgreSQL or set TASKTRACKER_TEST_DB_CONNECTION " +
                "to a connection string that points to a disposable test database user with create/drop permissions.",
                ex);
        }

        return database;
    }

    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            await using var dbContext = CreateContext();
            await dbContext.Database.EnsureDeletedAsync();
        }
        catch
        {
            // Cleanup must not hide the real test failure.
        }
    }
}
