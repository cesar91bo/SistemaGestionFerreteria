using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGestionFerreteria.Infrastructure.Migrations
{
    public partial class CambiarIvaAEnum : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TipoIva",
                table: "ProductosPrecios",
                type: "int",
                nullable: false,
                defaultValue: 210);

            migrationBuilder.Sql(@"
                UPDATE ProductosPrecios
                SET TipoIva =
                    CASE
                        WHEN PorcentajeIva = 10.5 THEN 105
                        WHEN PorcentajeIva = 21 THEN 210
                        WHEN PorcentajeIva = 27 THEN 270
                        ELSE 0
                    END
            ");

            migrationBuilder.DropColumn(
                name: "PorcentajeIva",
                table: "ProductosPrecios");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PorcentajeIva",
                table: "ProductosPrecios",
                type: "decimal(5,2)",
                nullable: true);

            migrationBuilder.Sql(@"
                UPDATE ProductosPrecios
                SET PorcentajeIva =
                    CASE
                        WHEN TipoIva = 105 THEN 10.5
                        WHEN TipoIva = 210 THEN 21
                        WHEN TipoIva = 270 THEN 27
                        ELSE 0
                    END
            ");

            migrationBuilder.DropColumn(
                name: "TipoIva",
                table: "ProductosPrecios");
        }
    }
}