using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LotoAnalytics.Api.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddEstatisticasFiltrosSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "estatisticas_filtros",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_modalidade = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    categoria = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    valor = table.Column<int>(type: "integer", nullable: false),
                    quantidade = table.Column<int>(type: "integer", nullable: false),
                    atualizado_em = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_estatisticas_filtros", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_estatisticas_filtros_codigo_modalidade_categoria_valor",
                table: "estatisticas_filtros",
                columns: new[] { "codigo_modalidade", "categoria", "valor" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "estatisticas_filtros");
        }
    }
}
