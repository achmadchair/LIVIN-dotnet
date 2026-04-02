using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Livin.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPartEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InspectionTasks_Equipments_EquipmentId",
                table: "InspectionTasks");

            migrationBuilder.RenameColumn(
                name: "EquipmentId",
                table: "InspectionTasks",
                newName: "PartId");

            migrationBuilder.RenameIndex(
                name: "IX_InspectionTasks_EquipmentId",
                table: "InspectionTasks",
                newName: "IX_InspectionTasks_PartId");

            migrationBuilder.AddColumn<string>(
                name: "Group",
                table: "TaskStandards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "TaskStandards",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Group",
                table: "InspectionTasks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "InspectionTasks",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Parts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HACCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SiteId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Group = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EquipmentId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Parts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Parts_Equipments_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "Equipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Parts_Sites_SiteId",
                        column: x => x.SiteId,
                        principalTable: "Sites",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Parts_EquipmentId",
                table: "Parts",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_Parts_SiteId",
                table: "Parts",
                column: "SiteId");

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionTasks_Parts_PartId",
                table: "InspectionTasks",
                column: "PartId",
                principalTable: "Parts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InspectionTasks_Parts_PartId",
                table: "InspectionTasks");

            migrationBuilder.DropTable(
                name: "Parts");

            migrationBuilder.DropColumn(
                name: "Group",
                table: "TaskStandards");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "TaskStandards");

            migrationBuilder.DropColumn(
                name: "Group",
                table: "InspectionTasks");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "InspectionTasks");

            migrationBuilder.RenameColumn(
                name: "PartId",
                table: "InspectionTasks",
                newName: "EquipmentId");

            migrationBuilder.RenameIndex(
                name: "IX_InspectionTasks_PartId",
                table: "InspectionTasks",
                newName: "IX_InspectionTasks_EquipmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_InspectionTasks_Equipments_EquipmentId",
                table: "InspectionTasks",
                column: "EquipmentId",
                principalTable: "Equipments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
