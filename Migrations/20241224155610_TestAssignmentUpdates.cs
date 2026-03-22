using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace math4ktu_be.Migrations
{
    /// <inheritdoc />
    public partial class TestAssignmentUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "TestsAssignments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Title",
                table: "TestsAssignments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Description",
                table: "TestsAssignments");

            migrationBuilder.DropColumn(
                name: "Title",
                table: "TestsAssignments");
        }
    }
}
