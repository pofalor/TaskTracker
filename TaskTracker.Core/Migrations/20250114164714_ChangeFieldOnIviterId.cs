using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaskTracker.Core.Migrations
{
    /// <inheritdoc />
    public partial class ChangeFieldOnIviterId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserWorkspaceStatusChangeRequest_User_RequestCreatorId",
                table: "UserWorkspaceStatusChangeRequest");

            migrationBuilder.RenameColumn(
                name: "RequestCreatorId",
                table: "UserWorkspaceStatusChangeRequest",
                newName: "InviterId");

            migrationBuilder.RenameIndex(
                name: "IX_UserWorkspaceStatusChangeRequest_RequestCreatorId",
                table: "UserWorkspaceStatusChangeRequest",
                newName: "IX_UserWorkspaceStatusChangeRequest_InviterId");

            migrationBuilder.AlterColumn<int>(
                name: "previous_status",
                table: "UserWorkspaceStatusChangeRequest",
                type: "integer",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddForeignKey(
                name: "FK_UserWorkspaceStatusChangeRequest_User_InviterId",
                table: "UserWorkspaceStatusChangeRequest",
                column: "InviterId",
                principalTable: "User",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserWorkspaceStatusChangeRequest_User_InviterId",
                table: "UserWorkspaceStatusChangeRequest");

            migrationBuilder.RenameColumn(
                name: "InviterId",
                table: "UserWorkspaceStatusChangeRequest",
                newName: "RequestCreatorId");

            migrationBuilder.RenameIndex(
                name: "IX_UserWorkspaceStatusChangeRequest_InviterId",
                table: "UserWorkspaceStatusChangeRequest",
                newName: "IX_UserWorkspaceStatusChangeRequest_RequestCreatorId");

            migrationBuilder.AlterColumn<int>(
                name: "previous_status",
                table: "UserWorkspaceStatusChangeRequest",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_UserWorkspaceStatusChangeRequest_User_RequestCreatorId",
                table: "UserWorkspaceStatusChangeRequest",
                column: "RequestCreatorId",
                principalTable: "User",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
