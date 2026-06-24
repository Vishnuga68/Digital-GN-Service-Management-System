using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EGN_Service_Management.Migrations
{
    public partial class AddProvinceDistrictMaster : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Provinces",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Provinces", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Districts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProvinceId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Districts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Districts_Provinces_ProvinceId",
                        column: x => x.ProvinceId,
                        principalTable: "Provinces",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Provinces",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Northern" },
                    { 2, "Eastern" },
                    { 3, "North Western" },
                    { 4, "North Central" },
                    { 5, "Central" },
                    { 6, "Western" },
                    { 7, "Southern" },
                    { 8, "Uva" },
                    { 9, "Sabaragamuwa" }
                });

            migrationBuilder.InsertData(
                table: "Districts",
                columns: new[] { "Id", "Name", "ProvinceId" },
                values: new object[,]
                {
                    { 1, "Jaffna", 1 },
                    { 2, "Kilinochchi", 1 },
                    { 3, "Mannar", 1 },
                    { 4, "Mullaitivu", 1 },
                    { 5, "Vavuniya", 1 },
                    { 6, "Trincomalee", 2 },
                    { 7, "Batticaloa", 2 },
                    { 8, "Ampara", 2 },
                    { 9, "Kurunegala", 3 },
                    { 10, "Puttalam", 3 },
                    { 11, "Anuradhapura", 4 },
                    { 12, "Polonnaruwa", 4 },
                    { 13, "Kandy", 5 },
                    { 14, "Matale", 5 },
                    { 15, "Nuwara Eliya", 5 },
                    { 16, "Colombo", 6 },
                    { 17, "Gampaha", 6 },
                    { 18, "Kalutara", 6 },
                    { 19, "Galle", 7 },
                    { 20, "Matara", 7 },
                    { 21, "Hambantota", 7 },
                    { 22, "Badulla", 8 },
                    { 23, "Monaragala", 8 },
                    { 24, "Ratnapura", 9 },
                    { 25, "Kegalle", 9 }
                });

            migrationBuilder.AddColumn<int>(
                name: "DistrictId",
                table: "ds_divisions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProvinceId",
                table: "ds_divisions",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Districts_ProvinceId",
                table: "Districts",
                column: "ProvinceId");

            migrationBuilder.CreateIndex(
                name: "IX_ds_divisions_DistrictId",
                table: "ds_divisions",
                column: "DistrictId");

            migrationBuilder.CreateIndex(
                name: "IX_ds_divisions_ProvinceId",
                table: "ds_divisions",
                column: "ProvinceId");

            migrationBuilder.AddForeignKey(
                name: "FK_ds_divisions_Districts_DistrictId",
                table: "ds_divisions",
                column: "DistrictId",
                principalTable: "Districts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ds_divisions_Provinces_ProvinceId",
                table: "ds_divisions",
                column: "ProvinceId",
                principalTable: "Provinces",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            // CUSTOM SQL TO PRESERVE JAFFNA DATA
            migrationBuilder.Sql(@"
                UPDATE ds_divisions
                SET 
                    DistrictId = (SELECT Id FROM Districts WHERE Name = ds_divisions.District),
                    ProvinceId = (SELECT Id FROM Provinces WHERE Name = ds_divisions.Province)
                WHERE District IS NOT NULL AND Province IS NOT NULL;
            ");

            // NOW WE CAN SAFELY DROP THE OLD COLUMNS
            migrationBuilder.DropColumn(
                name: "District",
                table: "ds_divisions");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "ds_divisions");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "ds_divisions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "ds_divisions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(@"
                UPDATE ds_divisions
                SET 
                    District = (SELECT Name FROM Districts WHERE Id = ds_divisions.DistrictId),
                    Province = (SELECT Name FROM Provinces WHERE Id = ds_divisions.ProvinceId)
                WHERE DistrictId IS NOT NULL AND ProvinceId IS NOT NULL;
            ");

            migrationBuilder.DropForeignKey(
                name: "FK_ds_divisions_Districts_DistrictId",
                table: "ds_divisions");

            migrationBuilder.DropForeignKey(
                name: "FK_ds_divisions_Provinces_ProvinceId",
                table: "ds_divisions");

            migrationBuilder.DropIndex(
                name: "IX_ds_divisions_DistrictId",
                table: "ds_divisions");

            migrationBuilder.DropIndex(
                name: "IX_ds_divisions_ProvinceId",
                table: "ds_divisions");

            migrationBuilder.DropColumn(
                name: "DistrictId",
                table: "ds_divisions");

            migrationBuilder.DropColumn(
                name: "ProvinceId",
                table: "ds_divisions");

            migrationBuilder.DropTable(
                name: "Districts");

            migrationBuilder.DropTable(
                name: "Provinces");
        }
    }
}
