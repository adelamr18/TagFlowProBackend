using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TagFlowApi.Migrations
{
    /// <inheritdoc />
    public partial class AddRoleIdToAdminsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RoleId",
                table: "Admins",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Admins");
        }
    }
}
