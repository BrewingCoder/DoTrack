using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoTrack.Migrations.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class AddSprints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SprintId",
                table: "work_items",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "sprints",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    StartsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    EndsOn = table.Column<DateOnly>(type: "date", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sprints_projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_work_items_SprintId",
                table: "work_items",
                column: "SprintId");

            migrationBuilder.CreateIndex(
                name: "IX_sprints_ProjectId",
                table: "sprints",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_sprints_ProjectId_State",
                table: "sprints",
                columns: new[] { "ProjectId", "State" });

            migrationBuilder.AddForeignKey(
                name: "FK_work_items_sprints_SprintId",
                table: "work_items",
                column: "SprintId",
                principalTable: "sprints",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_work_items_sprints_SprintId",
                table: "work_items");

            migrationBuilder.DropTable(
                name: "sprints");

            migrationBuilder.DropIndex(
                name: "IX_work_items_SprintId",
                table: "work_items");

            migrationBuilder.DropColumn(
                name: "SprintId",
                table: "work_items");
        }
    }
}
