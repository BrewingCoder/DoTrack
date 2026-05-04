using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoTrack.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkItemLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "work_item_links",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TargetId = table.Column<Guid>(type: "TEXT", nullable: false),
                    LinkType = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_item_links", x => x.Id);
                    table.ForeignKey(
                        name: "FK_work_item_links_users_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_work_item_links_work_items_SourceId",
                        column: x => x.SourceId,
                        principalTable: "work_items",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_work_item_links_work_items_TargetId",
                        column: x => x.TargetId,
                        principalTable: "work_items",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_work_item_links_CreatedByUserId",
                table: "work_item_links",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_work_item_links_SourceId",
                table: "work_item_links",
                column: "SourceId");

            migrationBuilder.CreateIndex(
                name: "IX_work_item_links_SourceId_TargetId_LinkType",
                table: "work_item_links",
                columns: new[] { "SourceId", "TargetId", "LinkType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_item_links_TargetId",
                table: "work_item_links",
                column: "TargetId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_item_links");
        }
    }
}
