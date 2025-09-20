using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagFlowApi.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessingStartedAtToFileRow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessingStartedAt",
                table: "FileRows",
                type: "timestamptz",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FileRows_Status_ProcessingStartedAt",
                table: "FileRows",
                columns: new[] { "Status", "ProcessingStartedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FileRows_Status_ProcessingStartedAt",
                table: "FileRows");

            migrationBuilder.DropColumn(
                name: "ProcessingStartedAt",
                table: "FileRows");
        }
    }
}
