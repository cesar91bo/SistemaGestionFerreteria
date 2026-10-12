using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGestionFerreteria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class GuardarMontoRecibido : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "MontoRecibido",
                table: "Facturas",
                type: "decimal(18,2)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MontoRecibido",
                table: "Facturas");
        }
    }
}
