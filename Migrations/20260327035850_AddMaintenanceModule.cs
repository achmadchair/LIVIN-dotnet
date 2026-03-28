using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Livin.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "InpsectionDate",
                table: "InspectionRecords",
                newName: "InspectionDate");

            migrationBuilder.AddColumn<string>(
                name: "FollowUpAction",
                table: "InspectionDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TaskStandardId",
                table: "InspectionDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MaintenanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EquipmentId = table.Column<int>(type: "int", nullable: false),
                    InspectionRecordId = table.Column<int>(type: "int", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Technician = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompletionNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RequestedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MaintenanceRecords_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MaintenanceRecords_InspectionRecords_InspectionRecordId",
                        column: x => x.InspectionRecordId,
                        principalTable: "InspectionRecords",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_InspectionDetails_TaskStandardId",
                table: "InspectionDetails",
                column: "TaskStandardId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_EquipmentId",
                table: "MaintenanceRecords",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceRecords_InspectionRecordId",
                table: "MaintenanceRecords",
                column: "InspectionRecordId");

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionDetails_TaskStandards_TaskStandardId",
                table: "InspectionDetails",
                column: "TaskStandardId",
                principalTable: "TaskStandards",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InspectionDetails_TaskStandards_TaskStandardId",
                table: "InspectionDetails");

            migrationBuilder.DropTable(
                name: "MaintenanceRecords");

            migrationBuilder.DropIndex(
                name: "IX_InspectionDetails_TaskStandardId",
                table: "InspectionDetails");

            migrationBuilder.DropColumn(
                name: "FollowUpAction",
                table: "InspectionDetails");

            migrationBuilder.DropColumn(
                name: "TaskStandardId",
                table: "InspectionDetails");

            migrationBuilder.RenameColumn(
                name: "InspectionDate",
                table: "InspectionRecords",
                newName: "InpsectionDate");
        }
    }
}
