using Microsoft.EntityFrameworkCore.Migrations;

namespace ALMS.App.Migrations
{
    public partial class ChangeUniqueConstrainsOfLectureAndLectureSandbox : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LectureSandboxes_Name",
                table: "LectureSandboxes");

            migrationBuilder.DropIndex(
                name: "IX_Lecture_Name",
                table: "Lecture");

            migrationBuilder.CreateIndex(
                name: "IX_LectureSandboxes_Name_LectureId",
                table: "LectureSandboxes",
                columns: new[] { "Name", "LectureId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lecture_Name_OwnerId",
                table: "Lecture",
                columns: new[] { "Name", "OwnerId" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_LectureSandboxes_Name_LectureId",
                table: "LectureSandboxes");

            migrationBuilder.DropIndex(
                name: "IX_Lecture_Name_OwnerId",
                table: "Lecture");

            migrationBuilder.CreateIndex(
                name: "IX_LectureSandboxes_Name",
                table: "LectureSandboxes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lecture_Name",
                table: "Lecture",
                column: "Name",
                unique: true);
        }
    }
}
