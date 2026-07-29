using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LotoAnalytics.Api.Infrastructure.Database;

public sealed class LotoAnalyticsDbContextFactory : IDesignTimeDbContextFactory<LotoAnalyticsDbContext>
{
    // Cria o DbContext usado pelas ferramentas do EF em tempo de design.
    public LotoAnalyticsDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=lotoanalytics;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<LotoAnalyticsDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new LotoAnalyticsDbContext(options);
    }
}
