using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TaskTracker.Core.Migrations
{
    /// <inheritdoc />
    public partial class UserWorkspaceRequestsEntityCreating : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "start_date",
                table: "Project",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValue: new DateTime(2024, 11, 18, 7, 38, 8, 436, DateTimeKind.Utc).AddTicks(8580));

            migrationBuilder.CreateTable(
                name: "UserWorkspaceStatusChangeRequest",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    WorkSpaceId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    previous_status = table.Column<int>(type: "integer", nullable: false),
                    new_status = table.Column<int>(type: "integer", nullable: false),
                    request_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_checked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    object_create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    object_edit_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserWorkspaceStatusChangeRequest", x => x.id);
                    table.ForeignKey(
                        name: "FK_UserWorkspaceStatusChangeRequest_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserWorkspaceStatusChangeRequest_WorkSpace_WorkSpaceId",
                        column: x => x.WorkSpaceId,
                        principalTable: "WorkSpace",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserWorkspaceStatusChangeRequest_UserId",
                table: "UserWorkspaceStatusChangeRequest",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserWorkspaceStatusChangeRequest_WorkSpaceId",
                table: "UserWorkspaceStatusChangeRequest",
                column: "WorkSpaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserWorkspaceStatusChangeRequest");

            migrationBuilder.AlterColumn<DateTime>(
                name: "start_date",
                table: "Project",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(2024, 11, 18, 7, 38, 8, 436, DateTimeKind.Utc).AddTicks(8580),
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");
        }
    }
}
