using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LotoAnalytics.Api.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AtualizaValorApostaLotofacil : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "modalidades",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "valor_aposta_simples",
                value: 3.50m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "modalidades",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "valor_aposta_simples",
                value: 3.00m);
        }
    }
}
