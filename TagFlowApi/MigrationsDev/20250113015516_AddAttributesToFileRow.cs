using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagFlowApi.Migrations
{
    /// <inheritdoc />
    public partial class AddAttributesToFileRow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessedData",
                table: "FileRows");

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryNumber",
                table: "FileRows",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BeneficiaryType",
                table: "FileRows",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Class",
                table: "FileRows",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DeductIblerate",
                table: "FileRows",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "IdentityNumber",
                table: "FileRows",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InsuranceCompany",
                table: "FileRows",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "InsuranceExpiryDate",
                table: "FileRows",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MaxLimit",
                table: "FileRows",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "MedicalNetwork",
                table: "FileRows",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PolicyNumber",
                table: "FileRows",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UploadDate",
                table: "FileRows",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BeneficiaryNumber",
                table: "FileRows");

            migrationBuilder.DropColumn(
                name: "BeneficiaryType",
                table: "FileRows");

            migrationBuilder.DropColumn(
                name: "Class",
                table: "FileRows");

            migrationBuilder.DropColumn(
                name: "DeductIblerate",
                table: "FileRows");

            migrationBuilder.DropColumn(
                name: "IdentityNumber",
                table: "FileRows");

            migrationBuilder.DropColumn(
                name: "InsuranceCompany",
                table: "FileRows");

            migrationBuilder.DropColumn(
                name: "InsuranceExpiryDate",
                table: "FileRows");

            migrationBuilder.DropColumn(
                name: "MaxLimit",
                table: "FileRows");

            migrationBuilder.DropColumn(
                name: "MedicalNetwork",
                table: "FileRows");

            migrationBuilder.DropColumn(
                name: "PolicyNumber",
                table: "FileRows");

            migrationBuilder.DropColumn(
                name: "UploadDate",
                table: "FileRows");

            migrationBuilder.AddColumn<string>(
                name: "ProcessedData",
                table: "FileRows",
                type: "nvarchar(max)",
                nullable: true);
        }
    }
}
