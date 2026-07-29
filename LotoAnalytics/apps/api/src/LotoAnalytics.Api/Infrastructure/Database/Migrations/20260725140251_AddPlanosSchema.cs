using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LotoAnalytics.Api.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddPlanosSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "planos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    nome = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    limite_jogos_por_geracao = table.Column<int>(type: "integer", nullable: false),
                    permite_exportar_csv = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    permite_exportar_pdf = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ativo = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_planos", x => x.id);
                });

            migrationBuilder.InsertData(
                table: "planos",
                columns: new[] { "id", "ativo", "codigo", "criado_em", "limite_jogos_por_geracao", "nome", "atualizado_em" },
                values: new object[] { 1L, true, "gratis", new DateTimeOffset(new DateTime(2026, 7, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 5, "Gratis", new DateTimeOffset(new DateTime(2026, 7, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.InsertData(
                table: "planos",
                columns: new[] { "id", "ativo", "permite_exportar_csv", "permite_exportar_pdf", "codigo", "criado_em", "limite_jogos_por_geracao", "nome", "atualizado_em" },
                values: new object[] { 2L, true, true, true, "premium", new DateTimeOffset(new DateTime(2026, 7, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), 100, "Premium", new DateTimeOffset(new DateTime(2026, 7, 13, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)) });

            migrationBuilder.CreateIndex(
                name: "IX_planos_codigo",
                table: "planos",
                column: "codigo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "planos");
        }
    }
}
