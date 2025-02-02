using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagFlowApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateFilesModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FileRowsCounts",
                table: "Files",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FileStatus",
                table: "Files",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TagValue",
                table: "Files",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TagValuesIds",
                table: "Files",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TagValuesJson",
                table: "FileRows",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FileRowsCounts",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "FileStatus",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "TagValue",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "TagValuesIds",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "TagValuesJson",
                table: "FileRows");
        }
    }
}
