using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoTrack.Migrations.Sqlite.Migrations
{
    /// <inheritdoc />
    public partial class AddProjectNextWorkItemNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "NextWorkItemNumber",
                table: "projects",
                type: "INTEGER",
                nullable: false,
                defaultValue: 1);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextWorkItemNumber",
                table: "projects");
        }
    }
}
