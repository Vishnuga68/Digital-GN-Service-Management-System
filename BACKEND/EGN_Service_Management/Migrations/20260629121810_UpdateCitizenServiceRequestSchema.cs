using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EGN_Service_Management.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCitizenServiceRequestSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_gn_officers_AssignedOfficerNic",
                table: "Requests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Request_Data",
                table: "Request_Data");

            migrationBuilder.DropIndex(
                name: "IX_Request_Data_RequestId",
                table: "Request_Data");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Request_Data");

            migrationBuilder.RenameColumn(
                name: "AssignedOfficerNic",
                table: "Requests",
                newName: "AssignedGnOfficerNic");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_AssignedOfficerNic",
                table: "Requests",
                newName: "IX_Requests_AssignedGnOfficerNic");

            migrationBuilder.AddColumn<string>(
                name: "FinalDocumentUrl",
                table: "Requests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Request_Data",
                table: "Request_Data",
                column: "RequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_gn_officers_AssignedGnOfficerNic",
                table: "Requests",
                column: "AssignedGnOfficerNic",
                principalTable: "gn_officers",
                principalColumn: "Nic",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_gn_officers_AssignedGnOfficerNic",
                table: "Requests");

            migrationBuilder.DropPrimaryKey(
                name: "PK_Request_Data",
                table: "Request_Data");

            migrationBuilder.DropColumn(
                name: "FinalDocumentUrl",
                table: "Requests");

            migrationBuilder.RenameColumn(
                name: "AssignedGnOfficerNic",
                table: "Requests",
                newName: "AssignedOfficerNic");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_AssignedGnOfficerNic",
                table: "Requests",
                newName: "IX_Requests_AssignedOfficerNic");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Request_Data",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Request_Data",
                table: "Request_Data",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Request_Data_RequestId",
                table: "Request_Data",
                column: "RequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_gn_officers_AssignedOfficerNic",
                table: "Requests",
                column: "AssignedOfficerNic",
                principalTable: "gn_officers",
                principalColumn: "Nic",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
