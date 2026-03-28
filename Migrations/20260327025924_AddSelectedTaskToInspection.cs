using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Livin.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddSelectedTaskToInspection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SelectedTask",
                table: "InspectionDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SelectedTask",
                table: "InspectionDetails");
        }
    }
}
