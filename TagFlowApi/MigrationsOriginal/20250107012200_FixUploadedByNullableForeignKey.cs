using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagFlowApi.Migrations
{
    /// <inheritdoc />
    public partial class FixUploadedByNullableForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Users_UploadedBy",
                table: "Files");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Users_UploadedBy",
                table: "Files",
                column: "UploadedBy",
                principalTable: "Users",
                principalColumn: "UserId",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Users_UploadedBy",
                table: "Files");

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Users_UploadedBy",
                table: "Files",
                column: "UploadedBy",
                principalTable: "Users",
                principalColumn: "UserId");
        }
    }
}
