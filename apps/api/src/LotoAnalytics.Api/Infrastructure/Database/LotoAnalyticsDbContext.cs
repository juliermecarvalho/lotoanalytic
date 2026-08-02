using Microsoft.EntityFrameworkCore;

namespace LotoAnalytics.Api.Infrastructure.Database;

public sealed class LotoAnalyticsDbContext(DbContextOptions<LotoAnalyticsDbContext> options) : DbContext(options)
{
    private static readonly Guid LotofacilModeId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static readonly Guid MegaSenaModeId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private static readonly Guid QuinaModeId = Guid.Parse("00000000-0000-0000-0000-000000000003");

    private static readonly Guid MaisMilionariaModeId = Guid.Parse("00000000-0000-0000-0000-000000000004");

    private static readonly Guid LotomaniaModeId = Guid.Parse("00000000-0000-0000-0000-000000000005");

    private static readonly Guid TimemaniaModeId = Guid.Parse("00000000-0000-0000-0000-000000000006");

    private static readonly Guid DuplaSenaModeId = Guid.Parse("00000000-0000-0000-0000-000000000007");

    private static readonly Guid DiaDeSorteModeId = Guid.Parse("00000000-0000-0000-0000-000000000008");

    private static readonly Guid SuperSeteModeId = Guid.Parse("00000000-0000-0000-0000-000000000009");

    private static readonly Guid FreePlanId = Guid.Parse("10000000-0000-0000-0000-000000000001");

    private static readonly Guid PremiumPlanId = Guid.Parse("10000000-0000-0000-0000-000000000002");

    public DbSet<LotteryMode> LotteryModes => Set<LotteryMode>();

    public DbSet<Contest> Contests => Set<Contest>();

    public DbSet<ContestNumber> ContestNumbers => Set<ContestNumber>();

    public DbSet<ContestPrizeTier> ContestPrizeTiers => Set<ContestPrizeTier>();

    public DbSet<ContestWinnerCity> ContestWinnerCities => Set<ContestWinnerCity>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Plan> Plans => Set<Plan>();

    public DbSet<GameGeneration> GameGenerations => Set<GameGeneration>();

    public DbSet<GeneratedGame> GeneratedGames => Set<GeneratedGame>();

    public DbSet<GameChecking> GameCheckings => Set<GameChecking>();

    public DbSet<CheckedUserGame> CheckedUserGames => Set<CheckedUserGame>();

    public DbSet<FilterStatistic> FilterStatistics => Set<FilterStatistic>();

    // Configura o modelo relacional em PT-BR e registra os seeds iniciais.
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var createdAt = new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero);

        ConfigureLotteryMode(modelBuilder, createdAt);
        ConfigureContest(modelBuilder);
        ConfigureContestNumber(modelBuilder);
        ConfigureContestPrizeTier(modelBuilder);
        ConfigureContestWinnerCity(modelBuilder);
        ConfigureUser(modelBuilder);
        ConfigurePlan(modelBuilder, createdAt);
        ConfigureGameGeneration(modelBuilder);
        ConfigureGeneratedGame(modelBuilder);
        ConfigureGameChecking(modelBuilder);
        ConfigureCheckedUserGame(modelBuilder);
        ConfigureFilterStatistic(modelBuilder);
    }

    // Configura as distribuicoes pre-calculadas das estatisticas de filtro por modalidade.
    private static void ConfigureFilterStatistic(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FilterStatistic>(entity =>
        {
            entity.ToTable("estatisticas_filtros");
            entity.HasKey(statistic => statistic.Id);
            entity.HasIndex(statistic => new { statistic.LotteryModeCode, statistic.Category, statistic.Value }).IsUnique();

            entity.Property(statistic => statistic.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
            entity.Property(statistic => statistic.LotteryModeCode).HasColumnName("codigo_modalidade").HasMaxLength(40).IsRequired();
            entity.Property(statistic => statistic.Category).HasColumnName("categoria").HasMaxLength(30).IsRequired();
            entity.Property(statistic => statistic.Value).HasColumnName("valor");
            entity.Property(statistic => statistic.Count).HasColumnName("quantidade");
            entity.Property(statistic => statistic.UpdatedAt).HasColumnName("atualizado_em").HasDefaultValueSql("now()");
        });
    }

    // Configura o cadastro de modalidades suportadas pelo sistema.
    private static void ConfigureLotteryMode(ModelBuilder modelBuilder, DateTimeOffset createdAt)
    {
        modelBuilder.Entity<LotteryMode>(entity =>
        {
            entity.ToTable("modalidades");
            entity.HasKey(mode => mode.Id);
            entity.HasIndex(mode => mode.Code).IsUnique();
            entity.HasIndex(mode => mode.CaixaGameType).IsUnique();

            entity.Property(mode => mode.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
            entity.Property(mode => mode.Code).HasColumnName("codigo").HasMaxLength(40).IsRequired();
            entity.Property(mode => mode.Name).HasColumnName("nome").HasMaxLength(80).IsRequired();
            entity.Property(mode => mode.CaixaGameType).HasColumnName("tipo_jogo_caixa").HasMaxLength(80).IsRequired();
            entity.Property(mode => mode.CaixaGameNumber).HasColumnName("numero_jogo_caixa");
            entity.Property(mode => mode.MainNumbersCount).HasColumnName("quantidade_dezenas_principal");
            entity.Property(mode => mode.SimpleBetPrice).HasColumnName("valor_aposta_simples").HasPrecision(10, 2);
            entity.Property(mode => mode.SecondDrawNumbersCount).HasColumnName("quantidade_dezenas_segundo_sorteio");
            entity.Property(mode => mode.HasTrevos).HasColumnName("possui_trevos").HasDefaultValue(false);
            entity.Property(mode => mode.HasHeartTeam).HasColumnName("possui_time_coracao").HasDefaultValue(false);
            entity.Property(mode => mode.HasLuckyMonth).HasColumnName("possui_mes_sorte").HasDefaultValue(false);
            entity.Property(mode => mode.Active).HasColumnName("ativa").HasDefaultValue(true);
            entity.Property(mode => mode.CreatedAt).HasColumnName("criado_em").HasDefaultValueSql("now()");
            entity.Property(mode => mode.UpdatedAt).HasColumnName("atualizado_em").HasDefaultValueSql("now()");

            entity.HasData(
                new LotteryMode { Id = LotofacilModeId, Code = "lotofacil", Name = "Lotofacil", CaixaGameType = "LOTOFACIL", CaixaGameNumber = 25, MainNumbersCount = 15, SimpleBetPrice = 3.50m, Active = true, CreatedAt = createdAt, UpdatedAt = createdAt },
                new LotteryMode { Id = MegaSenaModeId, Code = "mega_sena", Name = "Mega-Sena", CaixaGameType = "MEGA_SENA", CaixaGameNumber = 2, MainNumbersCount = 6, SimpleBetPrice = 6.00m, Active = true, CreatedAt = createdAt, UpdatedAt = createdAt },
                new LotteryMode { Id = QuinaModeId, Code = "quina", Name = "Quina", CaixaGameType = "QUINA", CaixaGameNumber = 5, MainNumbersCount = 5, SimpleBetPrice = 3.00m, Active = true, CreatedAt = createdAt, UpdatedAt = createdAt },
                new LotteryMode { Id = MaisMilionariaModeId, Code = "maismilionaria", Name = "+Milionaria", CaixaGameType = "MAIS_MILIONARIA", CaixaGameNumber = 33, MainNumbersCount = 6, HasTrevos = true, Active = true, CreatedAt = createdAt, UpdatedAt = createdAt },
                new LotteryMode { Id = LotomaniaModeId, Code = "lotomania", Name = "Lotomania", CaixaGameType = "LOTOMANIA", CaixaGameNumber = 7, MainNumbersCount = 20, Active = true, CreatedAt = createdAt, UpdatedAt = createdAt },
                new LotteryMode { Id = TimemaniaModeId, Code = "timemania", Name = "Timemania", CaixaGameType = "TIMEMANIA", CaixaGameNumber = 10, MainNumbersCount = 7, HasHeartTeam = true, Active = true, CreatedAt = createdAt, UpdatedAt = createdAt },
                new LotteryMode { Id = DuplaSenaModeId, Code = "dupla_sena", Name = "Dupla Sena", CaixaGameType = "DUPLA_SENA", CaixaGameNumber = 12, MainNumbersCount = 6, SecondDrawNumbersCount = 6, Active = true, CreatedAt = createdAt, UpdatedAt = createdAt },
                new LotteryMode { Id = DiaDeSorteModeId, Code = "dia_de_sorte", Name = "Dia de Sorte", CaixaGameType = "DIA_DE_SORTE", CaixaGameNumber = 31, MainNumbersCount = 7, HasLuckyMonth = true, Active = true, CreatedAt = createdAt, UpdatedAt = createdAt },
                new LotteryMode { Id = SuperSeteModeId, Code = "super_sete", Name = "Super Sete", CaixaGameType = "SUPER_SETE", CaixaGameNumber = 32, MainNumbersCount = 7, Active = true, CreatedAt = createdAt, UpdatedAt = createdAt });
        });
    }

    // Configura o resultado oficial de um concurso de loteria.
    private static void ConfigureContest(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Contest>(entity =>
        {
            entity.ToTable("concursos");
            entity.HasKey(contest => contest.Id);
            entity.HasIndex(contest => new { contest.LotteryModeId, contest.Number }).IsUnique();

            entity.Property(contest => contest.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
            entity.Property(contest => contest.LotteryModeId).HasColumnName("modalidade_id").HasColumnType("uuid");
            entity.Property(contest => contest.Number).HasColumnName("numero");
            entity.Property(contest => contest.PreviousContestNumber).HasColumnName("numero_concurso_anterior");
            entity.Property(contest => contest.NextContestNumber).HasColumnName("numero_concurso_proximo");
            entity.Property(contest => contest.DrawDate).HasColumnName("data_apuracao");
            entity.Property(contest => contest.NextContestDate).HasColumnName("data_proximo_concurso");
            entity.Property(contest => contest.DrawLocation).HasColumnName("local_sorteio").HasMaxLength(160);
            entity.Property(contest => contest.DrawCityState).HasColumnName("municipio_uf_sorteio").HasMaxLength(160);
            entity.Property(contest => contest.Accumulated).HasColumnName("acumulado").HasDefaultValue(false);
            entity.Property(contest => contest.LatestContest).HasColumnName("ultimo_concurso").HasDefaultValue(false);
            entity.Property(contest => contest.CollectedAmount).HasColumnName("valor_arrecadado").HasPrecision(14, 2);
            entity.Property(contest => contest.NextContestEstimatedValue).HasColumnName("valor_estimado_proximo_concurso").HasPrecision(14, 2);
            entity.Property(contest => contest.NextContestAccumulatedValue).HasColumnName("valor_acumulado_proximo_concurso").HasPrecision(14, 2);
            entity.Property(contest => contest.SpecialResult).HasColumnName("resultado_especial").HasMaxLength(160);
            entity.Property(contest => contest.RawResultJson).HasColumnName("result_json").HasColumnType("jsonb").IsRequired();
            entity.Property(contest => contest.CreatedAt).HasColumnName("criado_em").HasDefaultValueSql("now()");
            entity.Property(contest => contest.UpdatedAt).HasColumnName("atualizado_em").HasDefaultValueSql("now()");

            entity.HasOne(contest => contest.LotteryMode)
                .WithMany()
                .HasForeignKey(contest => contest.LotteryModeId);
        });
    }

    // Configura as dezenas normalizadas de cada concurso.
    private static void ConfigureContestNumber(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContestNumber>(entity =>
        {
            entity.ToTable("concurso_dezenas");
            entity.HasKey(number => number.Id);
            entity.HasIndex(number => new { number.ContestId, number.NumberType, number.Position }).IsUnique();

            entity.Property(number => number.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
            entity.Property(number => number.ContestId).HasColumnName("concurso_id").HasColumnType("uuid");
            entity.Property(number => number.NumberType).HasColumnName("tipo").HasMaxLength(30).IsRequired();
            entity.Property(number => number.Position).HasColumnName("posicao");
            entity.Property(number => number.Value).HasColumnName("valor").HasMaxLength(4).IsRequired();
            entity.Property(number => number.NumericValue).HasColumnName("valor_numero");
            entity.Property(number => number.CreatedAt).HasColumnName("criado_em").HasDefaultValueSql("now()");

            entity.HasOne(number => number.Contest)
                .WithMany(contest => contest.Numbers)
                .HasForeignKey(number => number.ContestId);
        });
    }

    // Configura as faixas de rateio de premio de cada concurso.
    private static void ConfigureContestPrizeTier(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContestPrizeTier>(entity =>
        {
            entity.ToTable("concurso_rateios");
            entity.HasKey(prizeTier => prizeTier.Id);
            entity.HasIndex(prizeTier => new { prizeTier.ContestId, prizeTier.Tier }).IsUnique();

            entity.Property(prizeTier => prizeTier.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
            entity.Property(prizeTier => prizeTier.ContestId).HasColumnName("concurso_id").HasColumnType("uuid");
            entity.Property(prizeTier => prizeTier.Tier).HasColumnName("faixa");
            entity.Property(prizeTier => prizeTier.Description).HasColumnName("descricao_faixa").HasMaxLength(120).IsRequired();
            entity.Property(prizeTier => prizeTier.WinnersCount).HasColumnName("numero_ganhadores");
            entity.Property(prizeTier => prizeTier.PrizeValue).HasColumnName("valor_premio").HasPrecision(14, 2);
            entity.Property(prizeTier => prizeTier.CreatedAt).HasColumnName("criado_em").HasDefaultValueSql("now()");

            entity.HasOne(prizeTier => prizeTier.Contest)
                .WithMany(contest => contest.PrizeTiers)
                .HasForeignKey(prizeTier => prizeTier.ContestId);
        });
    }

    // Configura os municipios ganhadores informados pela API da Caixa.
    private static void ConfigureContestWinnerCity(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContestWinnerCity>(entity =>
        {
            entity.ToTable("concurso_ganhadores_municipios");
            entity.HasKey(winnerCity => winnerCity.Id);

            entity.Property(winnerCity => winnerCity.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
            entity.Property(winnerCity => winnerCity.ContestId).HasColumnName("concurso_id").HasColumnType("uuid");
            entity.Property(winnerCity => winnerCity.City).HasColumnName("municipio").HasMaxLength(120).IsRequired();
            entity.Property(winnerCity => winnerCity.State).HasColumnName("uf").HasMaxLength(2).IsRequired();
            entity.Property(winnerCity => winnerCity.WinnersCount).HasColumnName("ganhadores");
            entity.Property(winnerCity => winnerCity.CreatedAt).HasColumnName("criado_em").HasDefaultValueSql("now()");

            entity.HasOne(winnerCity => winnerCity.Contest)
                .WithMany(contest => contest.WinnerCities)
                .HasForeignKey(winnerCity => winnerCity.ContestId);
        });
    }

    // Configura os usuarios sincronizados a partir do Keycloak.
    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("usuarios");
            entity.HasKey(user => user.Id);
            entity.HasIndex(user => user.KeycloakSubject).IsUnique();
            entity.HasIndex(user => user.Email);

            entity.Property(user => user.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
            entity.Property(user => user.KeycloakSubject).HasColumnName("keycloak_subject");
            entity.Property(user => user.Username).HasColumnName("nome_usuario").HasMaxLength(120);
            entity.Property(user => user.Email).HasColumnName("email").HasMaxLength(180);
            entity.Property(user => user.PlanCode).HasColumnName("codigo_plano").HasMaxLength(40).HasDefaultValue("gratis").IsRequired();
            entity.Property(user => user.Active).HasColumnName("ativo").HasDefaultValue(true);
            entity.Property(user => user.LastLoginAt).HasColumnName("ultimo_login_em");
            entity.Property(user => user.CreatedAt).HasColumnName("criado_em").HasDefaultValueSql("now()");
            entity.Property(user => user.UpdatedAt).HasColumnName("atualizado_em").HasDefaultValueSql("now()");
        });
    }

    // Configura os planos comerciais disponiveis para os usuarios.
    private static void ConfigurePlan(ModelBuilder modelBuilder, DateTimeOffset createdAt)
    {
        modelBuilder.Entity<Plan>(entity =>
        {
            entity.ToTable("planos");
            entity.HasKey(plan => plan.Id);
            entity.HasIndex(plan => plan.Code).IsUnique();

            entity.Property(plan => plan.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
            entity.Property(plan => plan.Code).HasColumnName("codigo").HasMaxLength(40).IsRequired();
            entity.Property(plan => plan.Name).HasColumnName("nome").HasMaxLength(80).IsRequired();
            entity.Property(plan => plan.GameGenerationLimit).HasColumnName("limite_jogos_por_geracao");
            entity.Property(plan => plan.CanExportCsv).HasColumnName("permite_exportar_csv").HasDefaultValue(false);
            entity.Property(plan => plan.CanExportPdf).HasColumnName("permite_exportar_pdf").HasDefaultValue(false);
            entity.Property(plan => plan.Active).HasColumnName("ativo").HasDefaultValue(true);
            entity.Property(plan => plan.CreatedAt).HasColumnName("criado_em").HasDefaultValueSql("now()");
            entity.Property(plan => plan.UpdatedAt).HasColumnName("atualizado_em").HasDefaultValueSql("now()");

            entity.HasData(
                new Plan
                {
                    Id = FreePlanId,
                    Code = "gratis",
                    Name = "Gratis",
                    GameGenerationLimit = 5,
                    CanExportCsv = false,
                    CanExportPdf = false,
                    Active = true,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                },
                new Plan
                {
                    Id = PremiumPlanId,
                    Code = "premium",
                    Name = "Premium",
                    GameGenerationLimit = 100,
                    CanExportCsv = true,
                    CanExportPdf = true,
                    Active = true,
                    CreatedAt = createdAt,
                    UpdatedAt = createdAt
                });
        });
    }

    // Configura o historico de geracoes de jogos feitas por usuario.
    private static void ConfigureGameGeneration(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameGeneration>(entity =>
        {
            entity.ToTable("geracoes_jogos");
            entity.HasKey(generation => generation.Id);
            entity.HasIndex(generation => new { generation.UserId, generation.CreatedAt });

            entity.Property(generation => generation.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
            entity.Property(generation => generation.UserId).HasColumnName("usuario_id").HasColumnType("uuid");
            entity.Property(generation => generation.LotteryModeCode).HasColumnName("codigo_modalidade").HasMaxLength(40).IsRequired();
            entity.Property(generation => generation.GameCount).HasColumnName("quantidade_jogos");
            entity.Property(generation => generation.NumbersPerGame).HasColumnName("dezenas_por_jogo");
            entity.Property(generation => generation.FiltersJson).HasColumnName("filtros_json").HasColumnType("jsonb").IsRequired();
            entity.Property(generation => generation.CreatedAt).HasColumnName("criado_em").HasDefaultValueSql("now()");

            entity.HasOne(generation => generation.User)
                .WithMany()
                .HasForeignKey(generation => generation.UserId);
        });
    }

    // Configura os jogos individuais gerados dentro de uma geracao.
    private static void ConfigureGeneratedGame(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GeneratedGame>(entity =>
        {
            entity.ToTable("jogos_gerados");
            entity.HasKey(game => game.Id);
            entity.HasIndex(game => new { game.GameGenerationId, game.GameNumber }).IsUnique();

            entity.Property(game => game.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
            entity.Property(game => game.GameGenerationId).HasColumnName("geracao_jogo_id").HasColumnType("uuid");
            entity.Property(game => game.GameNumber).HasColumnName("numero_jogo");
            entity.Property(game => game.Numbers).HasColumnName("dezenas").HasColumnType("text[]").IsRequired();
            entity.Property(game => game.EvenCount).HasColumnName("quantidade_pares");
            entity.Property(game => game.OddCount).HasColumnName("quantidade_impares");
            entity.Property(game => game.NumbersSum).HasColumnName("soma_dezenas");
            entity.Property(game => game.CreatedAt).HasColumnName("criado_em").HasDefaultValueSql("now()");

            entity.HasOne(game => game.GameGeneration)
                .WithMany(generation => generation.Games)
                .HasForeignKey(game => game.GameGenerationId);
        });
    }

    // Configura o historico de conferencias feitas por usuario.
    private static void ConfigureGameChecking(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GameChecking>(entity =>
        {
            entity.ToTable("conferencias");
            entity.HasKey(checking => checking.Id);
            entity.HasIndex(checking => new { checking.UserId, checking.CreatedAt });

            entity.Property(checking => checking.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
            entity.Property(checking => checking.UserId).HasColumnName("usuario_id").HasColumnType("uuid");
            entity.Property(checking => checking.LotteryModeCode).HasColumnName("codigo_modalidade").HasMaxLength(40).IsRequired();
            entity.Property(checking => checking.DrawnNumbers).HasColumnName("dezenas_sorteadas").HasColumnType("text[]").IsRequired();
            entity.Property(checking => checking.GameCount).HasColumnName("quantidade_jogos");
            entity.Property(checking => checking.AwardSummaryJson).HasColumnName("resumo_premiacao_json").HasColumnType("jsonb").IsRequired();
            entity.Property(checking => checking.CreatedAt).HasColumnName("criado_em").HasDefaultValueSql("now()");

            entity.HasOne(checking => checking.User)
                .WithMany()
                .HasForeignKey(checking => checking.UserId);
        });
    }

    // Configura os jogos conferidos dentro de uma conferencia.
    private static void ConfigureCheckedUserGame(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CheckedUserGame>(entity =>
        {
            entity.ToTable("jogos_conferidos");
            entity.HasKey(game => game.Id);
            entity.HasIndex(game => new { game.GameCheckingId, game.GameNumber }).IsUnique();

            entity.Property(game => game.Id).HasColumnName("id").HasColumnType("uuid").ValueGeneratedNever();
            entity.Property(game => game.GameCheckingId).HasColumnName("conferencia_id").HasColumnType("uuid");
            entity.Property(game => game.GameNumber).HasColumnName("numero_jogo");
            entity.Property(game => game.Numbers).HasColumnName("dezenas").HasColumnType("text[]").IsRequired();
            entity.Property(game => game.HitCount).HasColumnName("quantidade_acertos");
            entity.Property(game => game.MatchedNumbers).HasColumnName("dezenas_acertadas").HasColumnType("text[]").IsRequired();
            entity.Property(game => game.Awarded).HasColumnName("premiado");
            entity.Property(game => game.CreatedAt).HasColumnName("criado_em").HasDefaultValueSql("now()");

            entity.HasOne(game => game.GameChecking)
                .WithMany(checking => checking.Games)
                .HasForeignKey(game => game.GameCheckingId);
        });
    }
}
