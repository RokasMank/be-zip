using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace math4ktu_be.Migrations
{
    /// <inheritdoc />
    public partial class AddPublishedTest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Published",
                table: "Tests",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Published",
                table: "Tests");
        }
    }
}
