using Microsoft.EntityFrameworkCore.Migrations;

namespace ALMS.App.Migrations
{
    public partial class AddDirecotryToActionHistory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Directory",
                table: "ActivityActionHistory",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Directory",
                table: "ActivityActionHistory");
        }
    }
}
