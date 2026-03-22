using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace math4ktu_be.Migrations
{
    /// <inheritdoc />
    public partial class AssigmentToStudentSession : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TestAssignmentId",
                table: "StudentTestSessions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_StudentTestSessions_TestAssignmentId",
                table: "StudentTestSessions",
                column: "TestAssignmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentTestSessions_TestAssignments_TestAssignmentId",
                table: "StudentTestSessions",
                column: "TestAssignmentId",
                principalTable: "TestAssignments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudentTestSessions_TestAssignments_TestAssignmentId",
                table: "StudentTestSessions");

            migrationBuilder.DropIndex(
                name: "IX_StudentTestSessions_TestAssignmentId",
                table: "StudentTestSessions");

            migrationBuilder.DropColumn(
                name: "TestAssignmentId",
                table: "StudentTestSessions");
        }
    }
}
