using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EducationalCenter.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PerBranchUniqueNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sections_CourseId_Name",
                table: "Sections");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_Name",
                table: "Rooms");

            migrationBuilder.CreateIndex(
                name: "IX_Sections_BranchId_CourseId_Name",
                table: "Sections",
                columns: new[] { "BranchId", "CourseId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Sections_CourseId",
                table: "Sections",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_BranchId_Name",
                table: "Rooms",
                columns: new[] { "BranchId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sections_BranchId_CourseId_Name",
                table: "Sections");

            migrationBuilder.DropIndex(
                name: "IX_Sections_CourseId",
                table: "Sections");

            migrationBuilder.DropIndex(
                name: "IX_Rooms_BranchId_Name",
                table: "Rooms");

            migrationBuilder.CreateIndex(
                name: "IX_Sections_CourseId_Name",
                table: "Sections",
                columns: new[] { "CourseId", "Name" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Rooms_Name",
                table: "Rooms",
                column: "Name",
                unique: true,
                filter: "[IsDeleted] = 0");
        }
    }
}
