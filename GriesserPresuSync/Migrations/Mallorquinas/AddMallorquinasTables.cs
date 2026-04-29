using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GriesserPresuSync.Migrations
{
    public partial class AddMallorquinasTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Crear tabla principal IG_GriesserSyncMallor2
            migrationBuilder.CreateTable(
                name: "IG_GriesserSyncMallor2",
                columns: table => new
                {
                    IdLinea = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    id_budget = table.Column<int>(type: "int", nullable: true),
                    cod_client = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    date_created = table.Column<DateTime>(type: "datetime2", nullable: true),
                    presupuesto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    expiration_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    id_mallorquina_color = table.Column<int>(type: "int", nullable: true),
                    cod_mallorquina_color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    id_mallorquina_acabado = table.Column<int>(type: "int", nullable: true),
                    cod_mallorquina_acabado = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    num_lines = table.Column<int>(type: "int", nullable: true),
                    price_lines = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    num_incrementos = table.Column<int>(type: "int", nullable: true),
                    price_incrementos = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    price_color = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    importe_transporte = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    importe_instalacion = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    total = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IG_GriesserSyncMallor2", x => x.IdLinea);
                });

            // Crear tabla IG_GriesserSyncMallor2_Lineas
            migrationBuilder.CreateTable(
                name: "IG_GriesserSyncMallor2_Lineas",
                columns: table => new
                {
                    IdLineaPadre = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    IdLinea = table.Column<int>(type: "int", nullable: false),
                    pos = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    id_mallorquina_tipo = table.Column<int>(type: "int", nullable: true),
                    cod_mallorquina_tipo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    id_mallorquina_modelo = table.Column<int>(type: "int", nullable: true),
                    cod_mallorquina_modelo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ancho_hueco = table.Column<int>(type: "int", nullable: true),
                    alto_hueco = table.Column<int>(type: "int", nullable: true),
                    id_mallorquina_esquema = table.Column<int>(type: "int", nullable: true),
                    cod_mallorquina_esquema = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    id_mallorquina_tipologia_instalacion = table.Column<int>(type: "int", nullable: true),
                    cod_mallorquina_tipologia_instalacion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    motorizado = table.Column<bool>(type: "bit", nullable: true),
                    ancho_hoja = table.Column<int>(type: "int", nullable: true),
                    alto_hoja = table.Column<int>(type: "int", nullable: true),
                    precio_por_hoja = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    num_hojas = table.Column<int>(type: "int", nullable: true),
                    precio_herraje = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    precio_incrementos = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    precio_automatismos = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    units = table.Column<int>(type: "int", nullable: true),
                    total = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    id_automatismo_marca = table.Column<int>(type: "int", nullable: true),
                    cod_automatismo_marca = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    id_automatismo_receptor = table.Column<int>(type: "int", nullable: true),
                    cod_automatismo_receptor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    precio_receptor = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    precio_mandos = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IG_GriesserSyncMallor2_Lineas", x => new { x.IdLineaPadre, x.IdLinea });
                    table.ForeignKey(
                        name: "FK_IG_GriesserSyncMallor2_Lineas_Padre",
                        column: x => x.IdLineaPadre,
                        principalTable: "IG_GriesserSyncMallor2",
                        principalColumn: "IdLinea",
                        onDelete: ReferentialAction.Cascade);
                });

            // Crear tabla IG_GriesserSyncMallor2_Mandos
            migrationBuilder.CreateTable(
                name: "IG_GriesserSyncMallor2_Mandos",
                columns: table => new
                {
                    IdLineaPadre = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    IdLinea = table.Column<int>(type: "int", nullable: false),
                    id_automatismo_mando = table.Column<int>(type: "int", nullable: false),
                    cod_automatismo_mando = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    units = table.Column<int>(type: "int", nullable: true),
                    total = table.Column<decimal>(type: "decimal(18,2)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IG_GriesserSyncMallor2_Mandos", x => new { x.IdLineaPadre, x.IdLinea, x.id_automatismo_mando });
                    table.ForeignKey(
                        name: "FK_IG_GriesserSyncMallor2_Mandos_Linea",
                        columns: x => new { x.IdLineaPadre, x.IdLinea },
                        principalTable: "IG_GriesserSyncMallor2_Lineas",
                        principalColumns: new[] { "IdLineaPadre", "IdLinea" },
                        onDelete: ReferentialAction.Cascade);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "IG_GriesserSyncMallor2_Mandos");
            migrationBuilder.DropTable(name: "IG_GriesserSyncMallor2_Lineas");
            migrationBuilder.DropTable(name: "IG_GriesserSyncMallor2");
        }
    }
}