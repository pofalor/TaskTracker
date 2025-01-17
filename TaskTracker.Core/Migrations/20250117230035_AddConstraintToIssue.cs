using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddConstraintToIssue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "index",
                table: "Issue");

            migrationBuilder.DropIndex(
                name: "IX_Issue_ProjectId",
                table: "Issue");

            migrationBuilder.CreateIndex(
                name: "index",
                table: "Issue",
                column: "Index");

            migrationBuilder.CreateIndex(
                name: "IX_Issue_ProjectId_Index",
                table: "Issue",
                columns: new[] { "ProjectId", "Index" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "index",
                table: "Issue");

            migrationBuilder.DropIndex(
                name: "IX_Issue_ProjectId_Index",
                table: "Issue");

            migrationBuilder.CreateIndex(
                name: "index",
                table: "Issue",
                column: "Index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Issue_ProjectId",
                table: "Issue",
                column: "ProjectId");
        }
    }
}
