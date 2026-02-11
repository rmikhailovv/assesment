using Assesment.Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Assesment.Infrastructure.Postgres.Migrations;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting database migration...");

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");
        
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("ERROR: Connection string not found!");
            Environment.Exit(1);
        }

        Console.WriteLine($"Connecting to database...");

        var optionsBuilder = new DbContextOptionsBuilder<AssessmentDbContext>();
        optionsBuilder.UseNpgsql(connectionString);

        await using var context = new AssessmentDbContext(optionsBuilder.Options);

        try
        {
            Console.WriteLine("Applying migrations...");
            await context.Database.MigrateAsync();
            Console.WriteLine("Migrations completed successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR during migration: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            Environment.Exit(1);
        }
    }
}
