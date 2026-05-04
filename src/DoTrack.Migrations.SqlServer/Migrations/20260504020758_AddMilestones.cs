using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoTrack.Migrations.SqlServer.Migrations
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
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", maxLength: 4096, nullable: true),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: true),
                    HoursBudget = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: true),
                    VisibleToClient = table.Column<bool>(type: "bit", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_milestones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "milestone_scope",
                columns: table => new
                {
                    MilestoneId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    WorkItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
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
