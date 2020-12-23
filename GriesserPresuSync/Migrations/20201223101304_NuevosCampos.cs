using Microsoft.EntityFrameworkCore.Migrations;

namespace GriesserPresuSync.Migrations
{
    public partial class NuevosCampos : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Cliente",
                table: "IG_GriesserSyncPresupuestos");

            migrationBuilder.DropColumn(
                name: "Embalaje",
                table: "IG_GriesserSyncPresupuestos");

            migrationBuilder.DropColumn(
                name: "POS1",
                table: "IG_GriesserSyncPresupuestos");

            migrationBuilder.AddColumn<string>(
                name: "con_testero",
                table: "IG_GriesserSyncPresupuestos",
                nullable: true);

            migrationBuilder.AddColumn<float>(
                name: "price",
                table: "IG_GriesserSyncPresupuestos",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "price_tapa",
                table: "IG_GriesserSyncPresupuestos",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<float>(
                name: "price_testero",
                table: "IG_GriesserSyncPresupuestos",
                nullable: false,
                defaultValue: 0f);

            migrationBuilder.AddColumn<string>(
                name: "tipo",
                table: "IG_GriesserSyncPresupuestos",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "title",
                table: "IG_GriesserSyncPresupuestos",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "con_testero",
                table: "IG_GriesserSyncPresupuestos");

            migrationBuilder.DropColumn(
                name: "price",
                table: "IG_GriesserSyncPresupuestos");

            migrationBuilder.DropColumn(
                name: "price_tapa",
                table: "IG_GriesserSyncPresupuestos");

            migrationBuilder.DropColumn(
                name: "price_testero",
                table: "IG_GriesserSyncPresupuestos");

            migrationBuilder.DropColumn(
                name: "tipo",
                table: "IG_GriesserSyncPresupuestos");

            migrationBuilder.DropColumn(
                name: "title",
                table: "IG_GriesserSyncPresupuestos");

            migrationBuilder.AddColumn<string>(
                name: "Cliente",
                table: "IG_GriesserSyncPresupuestos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Embalaje",
                table: "IG_GriesserSyncPresupuestos",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "POS1",
                table: "IG_GriesserSyncPresupuestos",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
