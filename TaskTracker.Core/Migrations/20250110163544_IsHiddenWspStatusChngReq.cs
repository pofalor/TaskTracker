using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Core.Migrations
{
    /// <inheritdoc />
    public partial class IsHiddenWspStatusChngReq : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RequestCreatorId",
                table: "UserWorkspaceStatusChangeRequest",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "is_hidden",
                table: "UserWorkspaceStatusChangeRequest",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_UserWorkspaceStatusChangeRequest_RequestCreatorId",
                table: "UserWorkspaceStatusChangeRequest",
                column: "RequestCreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_UserWorkspaceStatusChangeRequest_User_RequestCreatorId",
                table: "UserWorkspaceStatusChangeRequest",
                column: "RequestCreatorId",
                principalTable: "User",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserWorkspaceStatusChangeRequest_User_RequestCreatorId",
                table: "UserWorkspaceStatusChangeRequest");

            migrationBuilder.DropIndex(
                name: "IX_UserWorkspaceStatusChangeRequest_RequestCreatorId",
                table: "UserWorkspaceStatusChangeRequest");

            migrationBuilder.DropColumn(
                name: "RequestCreatorId",
                table: "UserWorkspaceStatusChangeRequest");

            migrationBuilder.DropColumn(
                name: "is_hidden",
                table: "UserWorkspaceStatusChangeRequest");
        }
    }
}
