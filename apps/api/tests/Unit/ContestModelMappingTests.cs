using LotoAnalytics.Api.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;
using Shouldly;
using Xunit;

namespace LotoAnalytics.Api.UnitTests;

public sealed class ContestModelMappingTests
{
    [Fact]
    public void ContestEntitiesUsePortugueseDatabaseNames()
    {
        var options = new DbContextOptionsBuilder<LotoAnalyticsDbContext>()
            .UseNpgsql("Host=localhost;Database=lotoanalytics;Username=postgres;Password=postgres")
            .Options;
        using var dbContext = new LotoAnalyticsDbContext(options);

        var contest = dbContext.Model.FindEntityType(typeof(Contest));
        var contestNumber = dbContext.Model.FindEntityType(typeof(ContestNumber));
        var prizeTier = dbContext.Model.FindEntityType(typeof(ContestPrizeTier));
        var winnerCity = dbContext.Model.FindEntityType(typeof(ContestWinnerCity));

        contest.ShouldNotBeNull();
        contest.GetTableName().ShouldBe("concursos");
        contest.FindProperty(nameof(Contest.RawResultJson))?.GetColumnName().ShouldBe("result_json");
        contest.FindProperty(nameof(Contest.DrawDate))?.GetColumnName().ShouldBe("data_apuracao");

        contestNumber.ShouldNotBeNull();
        contestNumber.GetTableName().ShouldBe("concurso_dezenas");
        contestNumber.FindProperty(nameof(ContestNumber.NumberType))?.GetColumnName().ShouldBe("tipo");
        contestNumber.FindProperty(nameof(ContestNumber.Value))?.GetColumnName().ShouldBe("valor");

        prizeTier.ShouldNotBeNull();
        prizeTier.GetTableName().ShouldBe("concurso_rateios");
        prizeTier.FindProperty(nameof(ContestPrizeTier.Description))?.GetColumnName().ShouldBe("descricao_faixa");

        winnerCity.ShouldNotBeNull();
        winnerCity.GetTableName().ShouldBe("concurso_ganhadores_municipios");
        winnerCity.FindProperty(nameof(ContestWinnerCity.City))?.GetColumnName().ShouldBe("municipio");
        winnerCity.FindProperty(nameof(ContestWinnerCity.State))?.GetColumnName().ShouldBe("uf");
    }
}
