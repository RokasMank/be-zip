using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace math4ktu_be.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEnumsAndRemoveCognitive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop CognitiveArea column from Questions table
            migrationBuilder.DropColumn(
                name: "CognitiveArea",
                table: "Questions");

            // Update AchievementArea enum values
            // Old: AOne = 1, ATwo = 2, AThree = 3
            // New: KnowledgeUnderstandingArgumentation = 1, MathematicalCommunication = 2, ProblemSolving = 3
            // The numeric values remain the same, so no data migration needed

            // Update ContentType enum values
            // Old: CotOne = 1, CotTwo = 2, CotThree = 3
            // New: NumbersAndCalculations = 1, ModelsAndRelationships = 2, GeometryAndMeasurements = 3, DataAndProbability = 4
            // We need to add the new enum value 4 for DataAndProbability
            // No direct migration of existing data needed as numeric values stay the same
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore CognitiveArea column
            migrationBuilder.AddColumn<int>(
                name: "CognitiveArea",
                table: "Questions",
                type: "int",
                nullable: false,
                defaultValue: 1);
        }
    }
}
