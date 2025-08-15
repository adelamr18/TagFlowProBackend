using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TagFlowApi.Migrations
{
    /// <inheritdoc />
    public partial class AddRobotErrorsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RobotErrors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Module = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    FileId = table.Column<int>(type: "integer", nullable: true),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    PatientId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RobotErrors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RobotErrors_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "FileId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RobotErrors_FileId",
                table: "RobotErrors",
                column: "FileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RobotErrors");
        }
    }
}
