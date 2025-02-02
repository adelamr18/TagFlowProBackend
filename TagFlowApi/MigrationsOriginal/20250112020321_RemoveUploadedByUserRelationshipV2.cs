using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagFlowApi.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUploadedByUserRelationshipV2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UploadedBy",
                table: "Files");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UploadedBy",
                table: "Files",
                type: "int",
                nullable: true);
        }
    }
}
