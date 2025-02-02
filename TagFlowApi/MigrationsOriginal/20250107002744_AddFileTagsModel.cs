using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagFlowApi.Migrations
{
    /// <inheritdoc />
    public partial class AddFileTagsModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileRows");

            migrationBuilder.DropColumn(
                name: "TagValue",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "TagValuesIds",
                table: "Files");

            migrationBuilder.CreateTable(
                name: "FileTags",
                columns: table => new
                {
                    FileTagId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false),
                    TagValuesIds = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TagValueId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileTags", x => x.FileTagId);
                    table.ForeignKey(
                        name: "FK_FileTags_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FileTags_TagValues_TagValueId",
                        column: x => x.TagValueId,
                        principalTable: "TagValues",
                        principalColumn: "TagValueId");
                    table.ForeignKey(
                        name: "FK_FileTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileTags_FileId_TagId",
                table: "FileTags",
                columns: new[] { "FileId", "TagId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileTags_TagId",
                table: "FileTags",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_FileTags_TagValueId",
                table: "FileTags",
                column: "TagValueId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FileTags");

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

            migrationBuilder.CreateTable(
                name: "FileRows",
                columns: table => new
                {
                    RowId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FileId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false),
                    TagValueId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TagValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FileRows", x => x.RowId);
                    table.ForeignKey(
                        name: "FK_FileRows_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FileRows_TagValues_TagValueId",
                        column: x => x.TagValueId,
                        principalTable: "TagValues",
                        principalColumn: "TagValueId",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_FileRows_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "TagId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FileRows_FileId",
                table: "FileRows",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_FileRows_TagId",
                table: "FileRows",
                column: "TagId");

            migrationBuilder.CreateIndex(
                name: "IX_FileRows_TagValueId",
                table: "FileRows",
                column: "TagValueId");
        }
    }
}
