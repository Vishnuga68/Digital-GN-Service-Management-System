using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EGN_Service_Management.Migrations
{
    /// <inheritdoc />
    public partial class RenameHasOtherDisabilityToHasElderlyEligible : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RelationshipToHead",
                table: "Citizen_Master");

            migrationBuilder.RenameColumn(
                name: "HasOtherDisability",
                table: "citizen_special_statuses",
                newName: "HasVoterId");

            migrationBuilder.AddColumn<bool>(
                name: "HasElderlyEligible",
                table: "citizen_special_statuses",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HasElderlyEligible",
                table: "citizen_special_statuses");

            migrationBuilder.RenameColumn(
                name: "HasVoterId",
                table: "citizen_special_statuses",
                newName: "HasOtherDisability");

            migrationBuilder.AddColumn<string>(
                name: "RelationshipToHead",
                table: "Citizen_Master",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);
        }
    }
}
