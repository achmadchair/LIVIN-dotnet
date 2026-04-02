using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Livin.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateTaskAndStandardColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HACCode",
                table: "TaskStandards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PartName",
                table: "TaskStandards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TaskName",
                table: "TaskStandards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "HACCode",
                table: "InspectionTasks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PartName",
                table: "InspectionTasks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HACCode",
                table: "TaskStandards");

            migrationBuilder.DropColumn(
                name: "PartName",
                table: "TaskStandards");

            migrationBuilder.DropColumn(
                name: "TaskName",
                table: "TaskStandards");

            migrationBuilder.DropColumn(
                name: "HACCode",
                table: "InspectionTasks");

            migrationBuilder.DropColumn(
                name: "PartName",
                table: "InspectionTasks");
        }
    }
}
