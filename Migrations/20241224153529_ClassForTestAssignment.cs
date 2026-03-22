using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace math4ktu_be.Migrations
{
    /// <inheritdoc />
    public partial class ClassForTestAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Class",
                table: "TestsAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Class",
                table: "TestsAssignments");
        }
    }
}
