using Microsoft.EntityFrameworkCore.Migrations;

namespace GriesserPresuSync.Migrations
{
    public partial class CamposPresupuesto : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<float>(
                name: "importe_automatismos",
                table: "IG_GriesserSyncPresupuestos",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "importe_color",
                table: "IG_GriesserSyncPresupuestos",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "importe_incrementos",
                table: "IG_GriesserSyncPresupuestos",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "importe_lineas",
                table: "IG_GriesserSyncPresupuestos",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "importe_tapas_y_testeros",
                table: "IG_GriesserSyncPresupuestos",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "importe_tejido",
                table: "IG_GriesserSyncPresupuestos",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "importe_total",
                table: "IG_GriesserSyncPresupuestos",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "importe_transporte",
                table: "IG_GriesserSyncPresupuestos",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "superficie",
                table: "IG_GriesserSyncPresupuestos",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "importe_automatismos",
                table: "IG_GriesserSyncPresupuestos");

            migrationBuilder.DropColumn(
                name: "importe_color",
                table: "IG_GriesserSyncPresupuestos");

            migrationBuilder.DropColumn(
                name: "importe_incrementos",
                table: "IG_GriesserSyncPresupuestos");

            migrationBuilder.DropColumn(
                name: "importe_lineas",
                table: "IG_GriesserSyncPresupuestos");

            migrationBuilder.DropColumn(
                name: "importe_tapas_y_testeros",
                table: "IG_GriesserSyncPresupuestos");

            migrationBuilder.DropColumn(
                name: "importe_tejido",
                table: "IG_GriesserSyncPresupuestos");

            migrationBuilder.DropColumn(
                name: "importe_total",
                table: "IG_GriesserSyncPresupuestos");

            migrationBuilder.DropColumn(
                name: "importe_transporte",
                table: "IG_GriesserSyncPresupuestos");

            migrationBuilder.DropColumn(
                name: "superficie",
                table: "IG_GriesserSyncPresupuestos");
        }
    }
}
