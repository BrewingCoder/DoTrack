using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DoTrack.Migrations.Postgres.Migrations
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
                type: "integer",
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
