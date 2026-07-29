using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LotoAnalytics.Api.Infrastructure.Database.Migrations;

[DbContext(typeof(LotoAnalyticsDbContext))]
[Migration("20260713000000_InitialModalidadesSchema")]
public partial class InitialModalidadesSchema : Migration
{
    // Cria a tabela inicial de modalidades e insere as loterias suportadas.
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "modalidades",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                tipo_jogo_caixa = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                numero_jogo_caixa = table.Column<int>(type: "integer", nullable: true),
                quantidade_dezenas_principal = table.Column<int>(type: "integer", nullable: false),
                quantidade_dezenas_segundo_sorteio = table.Column<int>(type: "integer", nullable: true),
                possui_trevos = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                possui_time_coracao = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                possui_mes_sorte = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                ativa = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_modalidades", x => x.id);
            });

        migrationBuilder.InsertData(
            table: "modalidades",
            columns: ["id", "codigo", "nome", "tipo_jogo_caixa", "numero_jogo_caixa", "quantidade_dezenas_principal", "quantidade_dezenas_segundo_sorteio", "possui_trevos", "possui_time_coracao", "possui_mes_sorte", "ativa", "criado_em", "atualizado_em"],
            columnTypes: ["bigint", "character varying(40)", "character varying(80)", "character varying(80)", "integer", "integer", "integer", "boolean", "boolean", "boolean", "boolean", "timestamp with time zone", "timestamp with time zone"],
            values: new object[,]
            {
                { 1L, "lotofacil", "Lotofacil", "LOTOFACIL", 25, 15, null, false, false, false, true, new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero) },
                { 2L, "mega_sena", "Mega-Sena", "MEGA_SENA", 2, 6, null, false, false, false, true, new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero) },
                { 3L, "quina", "Quina", "QUINA", 5, 5, null, false, false, false, true, new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero) },
                { 4L, "maismilionaria", "+Milionaria", "MAIS_MILIONARIA", 33, 6, null, true, false, false, true, new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero) },
                { 5L, "lotomania", "Lotomania", "LOTOMANIA", 7, 20, null, false, false, false, true, new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero) },
                { 6L, "timemania", "Timemania", "TIMEMANIA", 10, 7, null, false, true, false, true, new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero) },
                { 7L, "dupla_sena", "Dupla Sena", "DUPLA_SENA", 12, 6, 6, false, false, false, true, new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero) },
                { 8L, "dia_de_sorte", "Dia de Sorte", "DIA_DE_SORTE", 31, 7, null, false, false, true, true, new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero) },
                { 9L, "super_sete", "Super Sete", "SUPER_SETE", 32, 7, null, false, false, false, true, new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 7, 13, 0, 0, 0, TimeSpan.Zero) }
            });

        migrationBuilder.CreateIndex(
            name: "ix_modalidades_codigo",
            table: "modalidades",
            column: "codigo",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "ix_modalidades_tipo_jogo_caixa",
            table: "modalidades",
            column: "tipo_jogo_caixa",
            unique: true);
    }

    // Remove a tabela inicial de modalidades.
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "modalidades");
    }
}
