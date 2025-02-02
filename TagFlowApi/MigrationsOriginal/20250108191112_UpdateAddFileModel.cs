using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagFlowApi.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAddFileModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UploadedByUserName",
                table: "Files",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UploadedByUserName",
                table: "Files");
        }
    }
}
