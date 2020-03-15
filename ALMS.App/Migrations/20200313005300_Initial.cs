using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

namespace ALMS.App.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SandboxTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(maxLength: 32, nullable: false),
                    Subject = table.Column<string>(maxLength: 64, nullable: false),
                    Description = table.Column<string>(nullable: true),
                    SetupCommands = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SandboxTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "User",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Account = table.Column<string>(maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(maxLength: 64, nullable: false),
                    EmailAddress = table.Column<string>(nullable: false),
                    EncryptedPassword = table.Column<string>(nullable: true),
                    IsAdmin = table.Column<bool>(nullable: false),
                    IsSenior = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_User", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lecture",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(maxLength: 32, nullable: false),
                    Subject = table.Column<string>(maxLength: 64, nullable: false),
                    Description = table.Column<string>(nullable: true),
                    OwnerId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lecture", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lecture_User_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sandboxes",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(maxLength: 32, nullable: false),
                    Description = table.Column<string>(nullable: true),
                    OwnerId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sandboxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sandboxes_User_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ActivityActionHistory",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(nullable: false),
                    LectureId = table.Column<int>(nullable: false),
                    ActionType = table.Column<int>(nullable: false),
                    ActivityName = table.Column<string>(nullable: true),
                    DateTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityActionHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityActionHistory_Lecture_LectureId",
                        column: x => x.LectureId,
                        principalTable: "Lecture",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ActivityActionHistory_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LectureSandboxes",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(maxLength: 32, nullable: false),
                    Description = table.Column<string>(nullable: true),
                    LectureId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LectureSandboxes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LectureSandboxes_Lecture_LectureId",
                        column: x => x.LectureId,
                        principalTable: "Lecture",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LectureUser",
                columns: table => new
                {
                    LectureId = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    Role = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LectureUser", x => new { x.UserId, x.LectureId, x.Role });
                    table.ForeignKey(
                        name: "FK_LectureUser_Lecture_LectureId",
                        column: x => x.LectureId,
                        principalTable: "Lecture",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LectureUser_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityActionHistory_LectureId",
                table: "ActivityActionHistory",
                column: "LectureId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityActionHistory_UserId",
                table: "ActivityActionHistory",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Lecture_Name",
                table: "Lecture",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lecture_OwnerId",
                table: "Lecture",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_LectureSandboxes_LectureId",
                table: "LectureSandboxes",
                column: "LectureId");

            migrationBuilder.CreateIndex(
                name: "IX_LectureSandboxes_Name",
                table: "LectureSandboxes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LectureUser_LectureId",
                table: "LectureUser",
                column: "LectureId");

            migrationBuilder.CreateIndex(
                name: "IX_Sandboxes_Name",
                table: "Sandboxes",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Sandboxes_OwnerId",
                table: "Sandboxes",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_SandboxTemplates_Name",
                table: "SandboxTemplates",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_User_Account",
                table: "User",
                column: "Account",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityActionHistory");

            migrationBuilder.DropTable(
                name: "LectureSandboxes");

            migrationBuilder.DropTable(
                name: "LectureUser");

            migrationBuilder.DropTable(
                name: "Sandboxes");

            migrationBuilder.DropTable(
                name: "SandboxTemplates");

            migrationBuilder.DropTable(
                name: "Lecture");

            migrationBuilder.DropTable(
                name: "User");
        }
    }
}
