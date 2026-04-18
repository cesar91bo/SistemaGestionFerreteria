using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGestionFerreteria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgregaUnidadMedida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UnidadMedida",
                columns: table => new
                {
                    IdUnidadMedida = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Abreviatura = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadMedida", x => x.IdUnidadMedida);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Productos_IdUnidadMedida",
                table: "Productos",
                column: "IdUnidadMedida");

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_UnidadMedida_IdUnidadMedida",
                table: "Productos",
                column: "IdUnidadMedida",
                principalTable: "UnidadMedida",
                principalColumn: "IdUnidadMedida",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Productos_UnidadMedida_IdUnidadMedida",
                table: "Productos");

            migrationBuilder.DropTable(
                name: "UnidadMedida");

            migrationBuilder.DropIndex(
                name: "IX_Productos_IdUnidadMedida",
                table: "Productos");
        }
    }
}
