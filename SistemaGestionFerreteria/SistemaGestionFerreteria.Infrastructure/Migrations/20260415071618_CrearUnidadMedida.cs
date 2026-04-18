using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGestionFerreteria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CrearUnidadMedida : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Productos_UnidadMedida_IdUnidadMedida",
                table: "Productos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnidadMedida",
                table: "UnidadMedida");

            migrationBuilder.RenameTable(
                name: "UnidadMedida",
                newName: "UnidadesMedida");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnidadesMedida",
                table: "UnidadesMedida",
                column: "IdUnidadMedida");

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_UnidadesMedida_IdUnidadMedida",
                table: "Productos",
                column: "IdUnidadMedida",
                principalTable: "UnidadesMedida",
                principalColumn: "IdUnidadMedida",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Productos_UnidadesMedida_IdUnidadMedida",
                table: "Productos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UnidadesMedida",
                table: "UnidadesMedida");

            migrationBuilder.RenameTable(
                name: "UnidadesMedida",
                newName: "UnidadMedida");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UnidadMedida",
                table: "UnidadMedida",
                column: "IdUnidadMedida");

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_UnidadMedida_IdUnidadMedida",
                table: "Productos",
                column: "IdUnidadMedida",
                principalTable: "UnidadMedida",
                principalColumn: "IdUnidadMedida",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
