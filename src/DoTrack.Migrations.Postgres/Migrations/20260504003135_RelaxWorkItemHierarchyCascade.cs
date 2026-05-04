using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoTrack.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class RelaxWorkItemHierarchyCascade : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_work_item_hierarchy_work_items_AncestorId",
                table: "work_item_hierarchy");

            migrationBuilder.DropForeignKey(
                name: "FK_work_item_hierarchy_work_items_DescendantId",
                table: "work_item_hierarchy");

            migrationBuilder.AddForeignKey(
                name: "FK_work_item_hierarchy_work_items_AncestorId",
                table: "work_item_hierarchy",
                column: "AncestorId",
                principalTable: "work_items",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_work_item_hierarchy_work_items_DescendantId",
                table: "work_item_hierarchy",
                column: "DescendantId",
                principalTable: "work_items",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_work_item_hierarchy_work_items_AncestorId",
                table: "work_item_hierarchy");

            migrationBuilder.DropForeignKey(
                name: "FK_work_item_hierarchy_work_items_DescendantId",
                table: "work_item_hierarchy");

            migrationBuilder.AddForeignKey(
                name: "FK_work_item_hierarchy_work_items_AncestorId",
                table: "work_item_hierarchy",
                column: "AncestorId",
                principalTable: "work_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_work_item_hierarchy_work_items_DescendantId",
                table: "work_item_hierarchy",
                column: "DescendantId",
                principalTable: "work_items",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
