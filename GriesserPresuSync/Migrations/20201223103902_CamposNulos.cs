using Microsoft.EntityFrameworkCore.Migrations;

namespace GriesserPresuSync.Migrations
{
    public partial class CamposNulos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "importe_transporte",
                table: "IG_GriesserSyncPresupuestos",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<float>(
                name: "importe_tejido",
                table: "IG_GriesserSyncPresupuestos",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<float>(
                name: "importe_tapas_y_testeros",
                table: "IG_GriesserSyncPresupuestos",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<float>(
                name: "importe_incrementos",
                table: "IG_GriesserSyncPresupuestos",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<float>(
                name: "importe_color",
                table: "IG_GriesserSyncPresupuestos",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AlterColumn<float>(
                name: "importe_automatismos",
                table: "IG_GriesserSyncPresupuestos",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "importe_transporte",
                table: "IG_GriesserSyncPresupuestos",
                type: "real",
                nullable: false,
                oldClrType: typeof(float),
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "importe_tejido",
                table: "IG_GriesserSyncPresupuestos",
                type: "real",
                nullable: false,
                oldClrType: typeof(float),
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "importe_tapas_y_testeros",
                table: "IG_GriesserSyncPresupuestos",
                type: "real",
                nullable: false,
                oldClrType: typeof(float),
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "importe_incrementos",
                table: "IG_GriesserSyncPresupuestos",
                type: "real",
                nullable: false,
                oldClrType: typeof(float),
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "importe_color",
                table: "IG_GriesserSyncPresupuestos",
                type: "real",
                nullable: false,
                oldClrType: typeof(float),
                oldNullable: true);

            migrationBuilder.AlterColumn<float>(
                name: "importe_automatismos",
                table: "IG_GriesserSyncPresupuestos",
                type: "real",
                nullable: false,
                oldClrType: typeof(float),
                oldNullable: true);
        }
    }
}
