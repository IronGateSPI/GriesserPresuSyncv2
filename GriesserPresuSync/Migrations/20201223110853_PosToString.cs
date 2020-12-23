using Microsoft.EntityFrameworkCore.Migrations;

namespace GriesserPresuSync.Migrations
{
    public partial class PosToString : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "POS",
                table: "IG_GriesserSyncPresupuestos",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "POS",
                table: "IG_GriesserSyncPresupuestos",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);
        }
    }
}
