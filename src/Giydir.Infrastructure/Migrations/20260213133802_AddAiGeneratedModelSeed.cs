using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Giydir.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAiGeneratedModelSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "ModelAssets",
                columns: new[] { "Id", "Category", "DefaultBackground", "DefaultCameraAngle", "DefaultLighting", "DefaultMood", "DefaultPose", "FullImagePath", "Gender", "Name", "ThumbnailPath" },
                values: new object[] { "ai-generated", "AI", null, null, null, null, null, "/images/ai-placeholder.png", "Unisex", "AI Generated", "/images/ai-placeholder.png" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "ModelAssets",
                keyColumn: "Id",
                keyValue: "ai-generated");
        }
    }
}
