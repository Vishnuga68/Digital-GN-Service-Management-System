using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EGN_Service_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddGnDivisionToCitizenMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GnDivisionId",
                table: "Citizen_Master",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Citizen_Master_GnDivisionId",
                table: "Citizen_Master",
                column: "GnDivisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Citizen_Master_gn_divisions_GnDivisionId",
                table: "Citizen_Master",
                column: "GnDivisionId",
                principalTable: "gn_divisions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Citizen_Master_gn_divisions_GnDivisionId",
                table: "Citizen_Master");

            migrationBuilder.DropIndex(
                name: "IX_Citizen_Master_GnDivisionId",
                table: "Citizen_Master");

            migrationBuilder.DropColumn(
                name: "GnDivisionId",
                table: "Citizen_Master");
        }
    }
}

