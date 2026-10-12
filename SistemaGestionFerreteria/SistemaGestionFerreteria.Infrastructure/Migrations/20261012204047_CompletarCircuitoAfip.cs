using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaGestionFerreteria.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompletarCircuitoAfip : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Facturas_Numero",
                table: "Facturas");

            migrationBuilder.AddColumn<string>(
                name: "AmbienteAfip",
                table: "Facturas",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CodigoComprobanteAfip",
                table: "Facturas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CuitEmisorAfip",
                table: "Facturas",
                type: "nvarchar(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdCajaVenta",
                table: "Facturas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdFacturaAsociada",
                table: "Facturas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OperacionId",
                table: "Facturas",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SolicitudAfip",
                table: "Facturas",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_Numero",
                table: "Facturas",
                columns: new[] { "TipoComprobante", "PuntoVenta", "NumeroComprobante" },
                unique: true,
                filter: "[AmbienteAfip] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_NumeroAfip",
                table: "Facturas",
                columns: new[] { "AmbienteAfip", "CuitEmisorAfip", "CodigoComprobanteAfip", "PuntoVenta", "NumeroComprobante" },
                unique: true,
                filter: "[EnviadaAfip] = 1 AND [AmbienteAfip] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_OperacionId",
                table: "Facturas",
                column: "OperacionId",
                unique: true,
                filter: "[OperacionId] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Facturas_Numero",
                table: "Facturas");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_NumeroAfip",
                table: "Facturas");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_OperacionId",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "AmbienteAfip",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "CodigoComprobanteAfip",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "CuitEmisorAfip",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "IdCajaVenta",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "IdFacturaAsociada",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "OperacionId",
                table: "Facturas");

            migrationBuilder.DropColumn(
                name: "SolicitudAfip",
                table: "Facturas");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_Numero",
                table: "Facturas",
                columns: new[] { "TipoComprobante", "PuntoVenta", "NumeroComprobante" },
                unique: true);
        }
    }
}
