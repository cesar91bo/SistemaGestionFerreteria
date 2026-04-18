using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGestionFerreteria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregarPorcentajeGananciaYCostoOpcional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioCosto",
                table: "ProductosPrecios",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)");

            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeGanancia",
                table: "ProductosPrecios",
                type: "decimal(5,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PorcentajeGanancia",
                table: "ProductosPrecios");

            migrationBuilder.AlterColumn<decimal>(
                name: "PrecioCosto",
                table: "ProductosPrecios",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);
        }
    }
}
