using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyLocalGov.com.Migrations
{
    /// <inheritdoc />
    public partial class AddIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_AspNetUsers_Id",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "UserProfiles");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "UserProfiles",
                newName: "UserID");

            migrationBuilder.AddColumn<string>(
                name: "DefaultAddressLine",
                table: "UserProfiles",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultCity",
                table: "UserProfiles",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DefaultLatitude",
                table: "UserProfiles",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DefaultLongitude",
                table: "UserProfiles",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultPostalCode",
                table: "UserProfiles",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultSuburb",
                table: "UserProfiles",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "UserProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ReputationPoints",
                table: "UserProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "Issues",
                columns: table => new
                {
                    IssueID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ReporterUserID = table.Column<string>(type: "TEXT", nullable: false),
                    LocationText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: true),
                    Longitude = table.Column<double>(type: "REAL", nullable: true),
                    CategoryID = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    StatusID = table.Column<int>(type: "INTEGER", nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    DateReported = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Issues", x => x.IssueID);
                    table.ForeignKey(
                        name: "FK_Issues_UserProfiles_ReporterUserID",
                        column: x => x.ReporterUserID,
                        principalTable: "UserProfiles",
                        principalColumn: "UserID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IssueAttachments",
                columns: table => new
                {
                    AttachmentID = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IssueID = table.Column<int>(type: "INTEGER", nullable: false),
                    FileName = table.Column<string>(type: "TEXT", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    FileBlob = table.Column<byte[]>(type: "BLOB", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "INTEGER", nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IssueAttachments", x => x.AttachmentID);
                    table.ForeignKey(
                        name: "FK_IssueAttachments_Issues_IssueID",
                        column: x => x.IssueID,
                        principalTable: "Issues",
                        principalColumn: "IssueID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IssueAttachments_IssueID",
                table: "IssueAttachments",
                column: "IssueID");

            migrationBuilder.CreateIndex(
                name: "IX_Issues_ReporterUserID",
                table: "Issues",
                column: "ReporterUserID");

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_AspNetUsers_UserID",
                table: "UserProfiles",
                column: "UserID",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserProfiles_AspNetUsers_UserID",
                table: "UserProfiles");

            migrationBuilder.DropTable(
                name: "IssueAttachments");

            migrationBuilder.DropTable(
                name: "Issues");

            migrationBuilder.DropColumn(
                name: "DefaultAddressLine",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "DefaultCity",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "DefaultLatitude",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "DefaultLongitude",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "DefaultPostalCode",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "DefaultSuburb",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "ReputationPoints",
                table: "UserProfiles");

            migrationBuilder.RenameColumn(
                name: "UserID",
                table: "UserProfiles",
                newName: "Id");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "UserProfiles",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_UserProfiles_AspNetUsers_Id",
                table: "UserProfiles",
                column: "Id",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
