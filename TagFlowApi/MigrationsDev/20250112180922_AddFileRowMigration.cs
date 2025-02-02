using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagFlowApi.Migrations
{
    /// <inheritdoc />
    public partial class AddFileRowMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create the FileRows table
            migrationBuilder.CreateTable(
                name: "FileRows",
                columns: table => new
                {
                    FileRowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileId = table.Column<int>(type: "int", nullable: false),
                    SsnId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false, defaultValue: "Unprocessed"),
                    ProcessedData = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileRows", x => x.FileRowId);
                    table.ForeignKey(
                        name: "FK_FileRows_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.SetNull); // Set appropriate action on delete (e.g., Cascade or SetNull)
                });

            // Add any additional constraints or indexes as needed, e.g., unique indexes on certain columns.
            migrationBuilder.CreateIndex(
                name: "IX_FileRows_SsnId",
                table: "FileRows",
                column: "SsnId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop the FileRows table if rolling back the migration
            migrationBuilder.DropTable(
                name: "FileRows");
        }
    }
}
