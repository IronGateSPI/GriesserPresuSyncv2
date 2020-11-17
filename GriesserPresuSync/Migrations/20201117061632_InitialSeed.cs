using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace GriesserPresuSync.Migrations
{
    public partial class InitialSeed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IG_GriesserSyncPresupuestos",
                columns: table => new
                {
                    IdLinea = table.Column<string>(nullable: false),
                    IdBudget = table.Column<int>(nullable: false),
                    date = table.Column<DateTime>(nullable: false),
                    Articulo = table.Column<string>(nullable: true),
                    Cliente = table.Column<string>(nullable: true),
                    NPresupuesto = table.Column<string>(nullable: true),
                    NPersianas = table.Column<int>(nullable: false),
                    TotalSup = table.Column<float>(nullable: false),
                    TotalAncho = table.Column<float>(nullable: false),
                    TotalLargo = table.Column<float>(nullable: false),
                    LargoTapas = table.Column<float>(nullable: false),
                    TotalTapas = table.Column<int>(nullable: false),
                    Embalaje = table.Column<string>(nullable: true),
                    POS = table.Column<int>(nullable: false),
                    BK = table.Column<float>(nullable: false),
                    HL = table.Column<float>(nullable: false),
                    Accion = table.Column<string>(nullable: true),
                    TL = table.Column<float>(nullable: false),
                    Uni = table.Column<int>(nullable: false),
                    PUnidad = table.Column<float>(nullable: false),
                    PUnidad2 = table.Column<float>(nullable: false),
                    TEUR = table.Column<float>(nullable: false),
                    POS1 = table.Column<string>(nullable: true),
                    Color = table.Column<string>(nullable: true),
                    IsSincronized = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IG_GriesserSyncPresupuestos", x => x.IdLinea);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IG_GriesserSyncPresupuestos");
        }
    }
}
