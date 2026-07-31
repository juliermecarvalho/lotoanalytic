using LotoAnalytics.Api.Infrastructure.Database;
using LotoAnalytics.Api.Features.Contests;
using LotoAnalytics.Api.Common.Auth;
using LotoAnalytics.Api.Features.Users;
using LotoAnalytics.Api.Features.FilterStatistics;
using LotoAnalytics.Api.Features.GameGeneration;
using LotoAnalytics.Api.Features.GameChecking;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    // Permite que o frontend local acesse a API durante o desenvolvimento.
    options.AddPolicy("LotoAnalyticsWeb", policy =>
    {
        var allowedOrigins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>()
            ?? ["http://127.0.0.1:5174", "http://localhost:5174"];

        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});
builder.Services.AddOpenApi();
builder.Services.Configure<CaixaLotteryOptions>(
    builder.Configuration.GetSection(CaixaLotteryOptions.SectionName));
builder.Services
    .AddHttpClient<ICaixaLotteryClient, CaixaLotteryClient>()
    .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
    {
        // Sai por proxy quando configurado, permitindo usar um IP brasileiro para driblar o bloqueio da Caixa.
        var caixaOptions = serviceProvider
            .GetRequiredService<IOptions<CaixaLotteryOptions>>()
            .Value;

        return CaixaHttpHandlerFactory.Create(caixaOptions.Proxy);
    });
builder.Services.AddKeycloakJwtAuthentication(builder.Configuration);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.Configure<ContestUpdateScheduleOptions>(
    builder.Configuration.GetSection(ContestUpdateScheduleOptions.SectionName));

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<LotoAnalyticsDbContext>(options => options.UseNpgsql(connectionString));
    builder.Services.AddScoped<IContestImportStore, EfContestImportStore>();
    builder.Services.AddScoped<IContestImportService, ContestImportService>();
    builder.Services.AddScoped<IContestBulkUpdateService, ContestBulkUpdateService>();
    builder.Services.AddScoped<IFilterStatisticsRefreshService, FilterStatisticsRefreshService>();
    builder.Services.AddScoped<ICurrentUserSynchronizer, CurrentUserSynchronizer>();
    builder.Services.AddScoped<IGameGenerationHistoryService, GameGenerationHistoryService>();
    builder.Services.AddScoped<IGameCheckingHistoryService, GameCheckingHistoryService>();
    builder.Services.AddHostedService<ContestUpdateHostedService>();
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs");
}

await app.ApplyDatabaseMigrationsAsync();

app.UseCors("LotoAnalyticsWeb");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
