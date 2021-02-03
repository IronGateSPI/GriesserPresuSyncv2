using Microsoft.EntityFrameworkCore.Migrations;

namespace GriesserPresuSync.Migrations
{
    public partial class AccionamientoGH : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<float>(
                name: "HL",
                table: "IG_GriesserSyncPresupuestos",
                nullable: true,
                oldClrType: typeof(float),
                oldType: "real");

            migrationBuilder.AddColumn<float>(
                name: "GH",
                table: "IG_GriesserSyncPresupuestos",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "accionamiento",
                table: "IG_GriesserSyncPresupuestos",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GH",
                table: "IG_GriesserSyncPresupuestos");

            migrationBuilder.DropColumn(
                name: "accionamiento",
                table: "IG_GriesserSyncPresupuestos");

            migrationBuilder.AlterColumn<float>(
                name: "HL",
                table: "IG_GriesserSyncPresupuestos",
                type: "real",
                nullable: false,
                oldClrType: typeof(float),
                oldNullable: true);
        }
    }
}
