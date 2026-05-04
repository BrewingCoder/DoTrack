using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoTrack.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkItemWatchers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "work_item_watchers",
                columns: table => new
                {
                    WorkItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AddedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_item_watchers", x => new { x.WorkItemId, x.UserId });
                    table.ForeignKey(
                        name: "FK_work_item_watchers_users_UserId",
                        column: x => x.UserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_work_item_watchers_work_items_WorkItemId",
                        column: x => x.WorkItemId,
                        principalTable: "work_items",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_work_item_watchers_UserId",
                table: "work_item_watchers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_item_watchers");
        }
    }
}
