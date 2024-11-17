using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TaskTracker.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddingEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "nick_name",
                table: "User",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "IdentityUser",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: true),
                    NormalizedUserName = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    NormalizedEmail = table.Column<string>(type: "text", nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityUser", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkSpace",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    work_space_type = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    DirectorUserId = table.Column<int>(type: "integer", nullable: false),
                    country = table.Column<int>(type: "integer", nullable: true),
                    registration_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    address = table.Column<string>(type: "text", nullable: true),
                    inn = table.Column<int>(type: "integer", nullable: true),
                    object_create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    object_edit_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkSpace", x => x.id);
                    table.ForeignKey(
                        name: "FK_WorkSpace_User_DirectorUserId",
                        column: x => x.DirectorUserId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Project",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    code = table.Column<string>(type: "text", nullable: false),
                    start_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValue: new DateTime(2024, 11, 17, 19, 29, 59, 357, DateTimeKind.Utc).AddTicks(3938)),
                    end_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    AuthorId = table.Column<int>(type: "integer", nullable: false),
                    ProjectMgrId = table.Column<int>(type: "integer", nullable: false),
                    WorkSpaceId = table.Column<int>(type: "integer", nullable: false),
                    object_create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    object_edit_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Project", x => x.id);
                    table.ForeignKey(
                        name: "FK_Project_User_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Project_User_ProjectMgrId",
                        column: x => x.ProjectMgrId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Project_WorkSpace_WorkSpaceId",
                        column: x => x.WorkSpaceId,
                        principalTable: "WorkSpace",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkSpaceMember",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    team_role = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    user_status = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    WorkSpaceId = table.Column<int>(type: "integer", nullable: false),
                    object_create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    object_edit_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkSpaceMember", x => x.id);
                    table.ForeignKey(
                        name: "FK_WorkSpaceMember_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WorkSpaceMember_WorkSpace_WorkSpaceId",
                        column: x => x.WorkSpaceId,
                        principalTable: "WorkSpace",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Issue",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    status = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    priority = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    estimate = table.Column<TimeSpan>(type: "interval", nullable: false, defaultValue: new TimeSpan(0, 0, 0, 0, 0)),
                    Index = table.Column<int>(type: "integer", nullable: false),
                    EpicId = table.Column<int>(type: "integer", nullable: true),
                    AuthorId = table.Column<int>(type: "integer", nullable: false),
                    AssigneeId = table.Column<int>(type: "integer", nullable: true),
                    ProjectId = table.Column<int>(type: "integer", nullable: false),
                    object_create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    object_edit_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Issue", x => x.id);
                    table.ForeignKey(
                        name: "FK_Issue_Issue_EpicId",
                        column: x => x.EpicId,
                        principalTable: "Issue",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Issue_Project_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Project",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Issue_User_AssigneeId",
                        column: x => x.AssigneeId,
                        principalTable: "User",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Issue_User_AuthorId",
                        column: x => x.AuthorId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TimeTracking",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    time_spent = table.Column<TimeSpan>(type: "interval", nullable: false, defaultValue: new TimeSpan(0, 0, 0, 0, 0)),
                    date_begin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    IssueId = table.Column<int>(type: "integer", nullable: false),
                    object_create_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    object_edit_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeTracking", x => x.id);
                    table.ForeignKey(
                        name: "FK_TimeTracking_Issue_IssueId",
                        column: x => x.IssueId,
                        principalTable: "Issue",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TimeTracking_User_UserId",
                        column: x => x.UserId,
                        principalTable: "User",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_User_UserId",
                table: "User",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "index",
                table: "Issue",
                column: "Index",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Issue_AssigneeId",
                table: "Issue",
                column: "AssigneeId");

            migrationBuilder.CreateIndex(
                name: "IX_Issue_AuthorId",
                table: "Issue",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Issue_EpicId",
                table: "Issue",
                column: "EpicId");

            migrationBuilder.CreateIndex(
                name: "IX_Issue_ProjectId",
                table: "Issue",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Project_AuthorId",
                table: "Project",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_Project_ProjectMgrId",
                table: "Project",
                column: "ProjectMgrId");

            migrationBuilder.CreateIndex(
                name: "IX_Project_WorkSpaceId",
                table: "Project",
                column: "WorkSpaceId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeTracking_IssueId",
                table: "TimeTracking",
                column: "IssueId");

            migrationBuilder.CreateIndex(
                name: "IX_TimeTracking_UserId",
                table: "TimeTracking",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSpace_DirectorUserId",
                table: "WorkSpace",
                column: "DirectorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSpaceMember_UserId",
                table: "WorkSpaceMember",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkSpaceMember_WorkSpaceId",
                table: "WorkSpaceMember",
                column: "WorkSpaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_User_IdentityUser_UserId",
                table: "User",
                column: "UserId",
                principalTable: "IdentityUser",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_User_IdentityUser_UserId",
                table: "User");

            migrationBuilder.DropTable(
                name: "IdentityUser");

            migrationBuilder.DropTable(
                name: "TimeTracking");

            migrationBuilder.DropTable(
                name: "WorkSpaceMember");

            migrationBuilder.DropTable(
                name: "Issue");

            migrationBuilder.DropTable(
                name: "Project");

            migrationBuilder.DropTable(
                name: "WorkSpace");

            migrationBuilder.DropIndex(
                name: "IX_User_UserId",
                table: "User");

            migrationBuilder.DropColumn(
                name: "nick_name",
                table: "User");
        }
    }
}
