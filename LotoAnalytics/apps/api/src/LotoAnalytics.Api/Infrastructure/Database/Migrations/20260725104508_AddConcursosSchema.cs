using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace LotoAnalytics.Api.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddConcursosSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "concursos",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    modalidade_id = table.Column<long>(type: "bigint", nullable: false),
                    numero = table.Column<int>(type: "integer", nullable: false),
                    numero_concurso_anterior = table.Column<int>(type: "integer", nullable: true),
                    numero_concurso_proximo = table.Column<int>(type: "integer", nullable: true),
                    data_apuracao = table.Column<DateOnly>(type: "date", nullable: true),
                    data_proximo_concurso = table.Column<DateOnly>(type: "date", nullable: true),
                    local_sorteio = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    municipio_uf_sorteio = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    acumulado = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    ultimo_concurso = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    valor_arrecadado = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    valor_estimado_proximo_concurso = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    valor_acumulado_proximo_concurso = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
                    resultado_especial = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true),
                    result_json = table.Column<string>(type: "jsonb", nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_concursos", x => x.id);
                    table.ForeignKey(
                        name: "FK_concursos_modalidades_modalidade_id",
                        column: x => x.modalidade_id,
                        principalTable: "modalidades",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "concurso_dezenas",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    concurso_id = table.Column<long>(type: "bigint", nullable: false),
                    tipo = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    posicao = table.Column<int>(type: "integer", nullable: false),
                    valor = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    valor_numero = table.Column<int>(type: "integer", nullable: true),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_concurso_dezenas", x => x.id);
                    table.ForeignKey(
                        name: "FK_concurso_dezenas_concursos_concurso_id",
                        column: x => x.concurso_id,
                        principalTable: "concursos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "concurso_rateios",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    concurso_id = table.Column<long>(type: "bigint", nullable: false),
                    faixa = table.Column<int>(type: "integer", nullable: false),
                    descricao_faixa = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    numero_ganhadores = table.Column<int>(type: "integer", nullable: false),
                    valor_premio = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
                    criado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_concurso_rateios", x => x.id);
                    table.ForeignKey(
                        name: "FK_concurso_rateios_concursos_concurso_id",
                        column: x => x.concurso_id,
                        principalTable: "concursos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_concurso_dezenas_concurso_id_tipo_posicao",
                table: "concurso_dezenas",
                columns: new[] { "concurso_id", "tipo", "posicao" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_concurso_rateios_concurso_id_faixa",
                table: "concurso_rateios",
                columns: new[] { "concurso_id", "faixa" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_concursos_modalidade_id_numero",
                table: "concursos",
                columns: new[] { "modalidade_id", "numero" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "concurso_dezenas");

            migrationBuilder.DropTable(
                name: "concurso_rateios");

            migrationBuilder.DropTable(
                name: "concursos");
        }
    }
}
