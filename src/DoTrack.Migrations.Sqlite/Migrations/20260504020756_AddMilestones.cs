using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoTrack.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddMilestones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "milestones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 4096, nullable: true),
                    TargetDate = table.Column<DateOnly>(type: "TEXT", nullable: true),
                    HoursBudget = table.Column<decimal>(type: "TEXT", precision: 10, scale: 2, nullable: true),
                    VisibleToClient = table.Column<bool>(type: "INTEGER", nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_scope",
                columns: table => new
                {
                    MilestoneId = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkItemId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestone_scope", x => new { x.MilestoneId, x.WorkItemId });
                    table.ForeignKey(
                        name: "FK_milestone_scope_milestones_MilestoneId",
                        column: x => x.MilestoneId,
                        principalTable: "milestones",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_milestone_scope_work_items_WorkItemId",
                        column: x => x.WorkItemId,
                        principalTable: "work_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_milestone_scope_WorkItemId",
                table: "milestone_scope",
                column: "WorkItemId");

            migrationBuilder.CreateIndex(
                name: "IX_milestones_State",
                table: "milestones",
                column: "State");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "milestone_scope");

            migrationBuilder.DropTable(
                name: "milestones");
        }
    }
}
