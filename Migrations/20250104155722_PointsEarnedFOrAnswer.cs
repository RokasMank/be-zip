using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace math4ktu_be.Migrations
{
    /// <inheritdoc />
    public partial class PointsEarnedFOrAnswer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "PointsEarned",
                table: "StudentAnswers",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PointsEarned",
                table: "StudentAnswers");
        }
    }
}
