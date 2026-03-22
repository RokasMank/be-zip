using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace math4ktu_be.Migrations
{
    /// <inheritdoc />
    public partial class StudentSessionAndAnswers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_TestsAssignments_TestAssignmentId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_TestsAssignments_Tests_TestId",
                table: "TestsAssignments");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TestsAssignments",
                table: "TestsAssignments");

            migrationBuilder.RenameTable(
                name: "TestsAssignments",
                newName: "TestAssignments");

            migrationBuilder.RenameIndex(
                name: "IX_TestsAssignments_TestId",
                table: "TestAssignments",
                newName: "IX_TestAssignments_TestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TestAssignments",
                table: "TestAssignments",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "StudentTestSessions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentTestSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentTestSessions_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentTestSessions_Tests_TestId",
                        column: x => x.TestId,
                        principalTable: "Tests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StudentTestSessionId = table.Column<int>(type: "int", nullable: false),
                    QuestionId = table.Column<int>(type: "int", nullable: false),
                    ProvidedAnswers = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAnswers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StudentAnswers_StudentTestSessions_StudentTestSessionId",
                        column: x => x.StudentTestSessionId,
                        principalTable: "StudentTestSessions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswers_QuestionId",
                table: "StudentAnswers",
                column: "QuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswers_StudentTestSessionId",
                table: "StudentAnswers",
                column: "StudentTestSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentTestSessions_StudentId",
                table: "StudentTestSessions",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentTestSessions_TestId",
                table: "StudentTestSessions",
                column: "TestId");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_TestAssignments_TestAssignmentId",
                table: "Students",
                column: "TestAssignmentId",
                principalTable: "TestAssignments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TestAssignments_Tests_TestId",
                table: "TestAssignments",
                column: "TestId",
                principalTable: "Tests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Students_TestAssignments_TestAssignmentId",
                table: "Students");

            migrationBuilder.DropForeignKey(
                name: "FK_TestAssignments_Tests_TestId",
                table: "TestAssignments");

            migrationBuilder.DropTable(
                name: "StudentAnswers");

            migrationBuilder.DropTable(
                name: "StudentTestSessions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TestAssignments",
                table: "TestAssignments");

            migrationBuilder.RenameTable(
                name: "TestAssignments",
                newName: "TestsAssignments");

            migrationBuilder.RenameIndex(
                name: "IX_TestAssignments_TestId",
                table: "TestsAssignments",
                newName: "IX_TestsAssignments_TestId");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TestsAssignments",
                table: "TestsAssignments",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Students_TestsAssignments_TestAssignmentId",
                table: "Students",
                column: "TestAssignmentId",
                principalTable: "TestsAssignments",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TestsAssignments_Tests_TestId",
                table: "TestsAssignments",
                column: "TestId",
                principalTable: "Tests",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
