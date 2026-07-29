using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LotoAnalytics.Api.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoricoConferenciasSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "conferencias",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo_modalidade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    dezenas_sorteadas = table.Column<string[]>(type: "text[]", nullable: false),
                    quantidade_jogos = table.Column<int>(type: "integer", nullable: false),
                    resumo_premiacao_json = table.Column<string>(type: "jsonb", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conferencias", x => x.id);
                    table.ForeignKey(
                        name: "FK_conferencias_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "jogos_conferidos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    conferencia_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_jogo = table.Column<int>(type: "integer", nullable: false),
                    dezenas = table.Column<string[]>(type: "text[]", nullable: false),
                    quantidade_acertos = table.Column<int>(type: "integer", nullable: false),
                    dezenas_acertadas = table.Column<string[]>(type: "text[]", nullable: false),
                    premiado = table.Column<bool>(type: "boolean", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jogos_conferidos", x => x.id);
                    table.ForeignKey(
                        name: "FK_jogos_conferidos_conferencias_conferencia_id",
                        column: x => x.conferencia_id,
                        principalTable: "conferencias",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_conferencias_usuario_id_criado_em",
                table: "conferencias",
                columns: new[] { "usuario_id", "criado_em" });

            migrationBuilder.CreateIndex(
                name: "IX_jogos_conferidos_conferencia_id_numero_jogo",
                table: "jogos_conferidos",
                columns: new[] { "conferencia_id", "numero_jogo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "jogos_conferidos");

            migrationBuilder.DropTable(
                name: "conferencias");
        }
    }
}
