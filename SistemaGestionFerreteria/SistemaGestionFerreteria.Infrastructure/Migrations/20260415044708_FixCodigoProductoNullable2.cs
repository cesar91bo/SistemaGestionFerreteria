using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGestionFerreteria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixCodigoProductoNullable2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Productos_CodigoProducto",
                table: "Productos");

            migrationBuilder.AlterColumn<string>(
                name: "CodigoProducto",
                table: "Productos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_CodigoProducto",
                table: "Productos",
                column: "CodigoProducto",
                unique: true,
                filter: "[CodigoProducto] IS NOT NULL AND [CodigoProducto] <> ''");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Productos_CodigoProducto",
                table: "Productos");

            migrationBuilder.AlterColumn<string>(
                name: "CodigoProducto",
                table: "Productos",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Productos_CodigoProducto",
                table: "Productos",
                column: "CodigoProducto",
                unique: true);
        }
    }
}
