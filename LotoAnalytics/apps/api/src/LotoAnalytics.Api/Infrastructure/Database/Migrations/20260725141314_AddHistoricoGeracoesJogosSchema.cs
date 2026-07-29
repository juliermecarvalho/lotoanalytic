using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LotoAnalytics.Api.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoricoGeracoesJogosSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "geracoes_jogos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    usuario_id = table.Column<long>(type: "bigint", nullable: false),
                    codigo_modalidade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    quantidade_jogos = table.Column<int>(type: "integer", nullable: false),
                    dezenas_por_jogo = table.Column<int>(type: "integer", nullable: false),
                    filtros_json = table.Column<string>(type: "jsonb", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_geracoes_jogos", x => x.id);
                    table.ForeignKey(
                        name: "FK_geracoes_jogos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "jogos_gerados",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    geracao_jogo_id = table.Column<long>(type: "bigint", nullable: false),
                    numero_jogo = table.Column<int>(type: "integer", nullable: false),
                    dezenas = table.Column<string[]>(type: "text[]", nullable: false),
                    quantidade_pares = table.Column<int>(type: "integer", nullable: false),
                    quantidade_impares = table.Column<int>(type: "integer", nullable: false),
                    soma_dezenas = table.Column<int>(type: "integer", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_jogos_gerados", x => x.id);
                    table.ForeignKey(
                        name: "FK_jogos_gerados_geracoes_jogos_geracao_jogo_id",
                        column: x => x.geracao_jogo_id,
                        principalTable: "geracoes_jogos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_geracoes_jogos_usuario_id_criado_em",
                table: "geracoes_jogos",
                columns: new[] { "usuario_id", "criado_em" });

            migrationBuilder.CreateIndex(
                name: "IX_jogos_gerados_geracao_jogo_id_numero_jogo",
                table: "jogos_gerados",
                columns: new[] { "geracao_jogo_id", "numero_jogo" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "jogos_gerados");

            migrationBuilder.DropTable(
                name: "geracoes_jogos");
        }
    }
}
