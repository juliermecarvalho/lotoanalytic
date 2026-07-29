using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LotoAnalytics.Api.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddValorApostaSimplesModalidades : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "valor_aposta_simples",
                table: "modalidades",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "modalidades",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000001"),
                column: "valor_aposta_simples",
                value: 3.00m);

            migrationBuilder.UpdateData(
                table: "modalidades",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000002"),
                column: "valor_aposta_simples",
                value: null);

            migrationBuilder.UpdateData(
                table: "modalidades",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000003"),
                column: "valor_aposta_simples",
                value: null);

            migrationBuilder.UpdateData(
                table: "modalidades",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000004"),
                column: "valor_aposta_simples",
                value: null);

            migrationBuilder.UpdateData(
                table: "modalidades",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000005"),
                column: "valor_aposta_simples",
                value: null);

            migrationBuilder.UpdateData(
                table: "modalidades",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000006"),
                column: "valor_aposta_simples",
                value: null);

            migrationBuilder.UpdateData(
                table: "modalidades",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000007"),
                column: "valor_aposta_simples",
                value: null);

            migrationBuilder.UpdateData(
                table: "modalidades",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000008"),
                column: "valor_aposta_simples",
                value: null);

            migrationBuilder.UpdateData(
                table: "modalidades",
                keyColumn: "id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000009"),
                column: "valor_aposta_simples",
                value: null);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "valor_aposta_simples",
                table: "modalidades");
        }
    }
}
