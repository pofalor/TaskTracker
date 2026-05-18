using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TaskTracker.Core.Migrations
{
    /// <inheritdoc />
    public partial class ai_16052026 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Issue_Issue_EpicId",
                table: "Issue");

            migrationBuilder.RenameColumn(
                name: "EpicId",
                table: "Issue",
                newName: "ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_Issue_EpicId",
                table: "Issue",
                newName: "IX_Issue_ParentId");

            migrationBuilder.CreateTable(
                name: "IssueStatusHistory",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    old_status = table.Column<int>(type: "integer", nullable: true),
                    new_status = table.Column<int>(type: "integer", nullable: false),
                    changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    IssueId = table.Column<int>(type: "integer", nullable: false),
                    ChangedByUserId = table.Column<int>(type: "integer", nullable: false),
                    object_create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    object_edit_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: false, defaultValueSql: "gen_random_bytes(8)"),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueStatusHistory", x => x.id);
                    table.ForeignKey(
                        name: "FK_IssueStatusHistory_Issue_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issue",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IssueStatusHistory_User_ChangedByUserId",
                        column: x => x.ChangedByUserId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssueStatusHistory_ChangedByUserId",
                table: "IssueStatusHistory",
                column: "ChangedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_IssueStatusHistory_IssueId",
                table: "IssueStatusHistory",
                column: "IssueId");

            migrationBuilder.AddForeignKey(
                name: "FK_Issue_Issue_ParentId",
                table: "Issue",
                column: "ParentId",
                principalTable: "Issue",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Issue_Issue_ParentId",
                table: "Issue");

            migrationBuilder.DropTable(
                name: "IssueStatusHistory");

            migrationBuilder.RenameColumn(
                name: "ParentId",
                table: "Issue",
                newName: "EpicId");

            migrationBuilder.RenameIndex(
                name: "IX_Issue_ParentId",
                table: "Issue",
                newName: "IX_Issue_EpicId");

            migrationBuilder.AddForeignKey(
                name: "FK_Issue_Issue_EpicId",
                table: "Issue",
                column: "EpicId",
                principalTable: "Issue",
                principalColumn: "id");
        }
    }
}
