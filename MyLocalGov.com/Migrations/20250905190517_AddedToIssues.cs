using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MyLocalGov.com.Migrations
{
    /// <inheritdoc />
    public partial class AddedToIssues : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationText",
                table: "Issues");

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "Issues",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "Issues",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0,
                oldClrType: typeof(double),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Issues",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Issues");

            migrationBuilder.AlterColumn<double>(
                name: "Longitude",
                table: "Issues",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AlterColumn<double>(
                name: "Latitude",
                table: "Issues",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "REAL");

            migrationBuilder.AddColumn<string>(
                name: "LocationText",
                table: "Issues",
                type: "TEXT",
                maxLength: 200,
                nullable: true);
        }
    }
}
