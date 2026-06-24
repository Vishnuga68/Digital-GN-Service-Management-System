using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EGN_Service_Management.Migrations
{
    /// <inheritdoc />
    public partial class RemoveRoleFromAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_EGN_Service_User_CitizenNic",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_conversations_EGN_Service_User_CitizenNic",
                table: "conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_poll_votes_EGN_Service_User_CitizenNic",
                table: "poll_votes");

            migrationBuilder.DropForeignKey(
                name: "FK_survey_responses_EGN_Service_User_CitizenNic",
                table: "survey_responses");

            migrationBuilder.DropTable(
                name: "EGN_Service_User");

            migrationBuilder.CreateTable(
                name: "EGN_Service_Users",
                columns: table => new
                {
                    Nic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EGN_Service_Users", x => x.Nic);
                    table.ForeignKey(
                        name: "FK_EGN_Service_Users_Citizen_Master_Nic",
                        column: x => x.Nic,
                        principalTable: "Citizen_Master",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EGN_Service_Users_accounts_Nic",
                        column: x => x.Nic,
                        principalTable: "accounts",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_EGN_Service_Users_CitizenNic",
                table: "appointments",
                column: "CitizenNic",
                principalTable: "EGN_Service_Users",
                principalColumn: "Nic",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_conversations_EGN_Service_Users_CitizenNic",
                table: "conversations",
                column: "CitizenNic",
                principalTable: "EGN_Service_Users",
                principalColumn: "Nic",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_poll_votes_EGN_Service_Users_CitizenNic",
                table: "poll_votes",
                column: "CitizenNic",
                principalTable: "EGN_Service_Users",
                principalColumn: "Nic",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_survey_responses_EGN_Service_Users_CitizenNic",
                table: "survey_responses",
                column: "CitizenNic",
                principalTable: "EGN_Service_Users",
                principalColumn: "Nic",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_appointments_EGN_Service_Users_CitizenNic",
                table: "appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_conversations_EGN_Service_Users_CitizenNic",
                table: "conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_poll_votes_EGN_Service_Users_CitizenNic",
                table: "poll_votes");

            migrationBuilder.DropForeignKey(
                name: "FK_survey_responses_EGN_Service_Users_CitizenNic",
                table: "survey_responses");

            migrationBuilder.DropTable(
                name: "EGN_Service_Users");

            migrationBuilder.CreateTable(
                name: "EGN_Service_User",
                columns: table => new
                {
                    Nic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EGN_Service_User", x => x.Nic);
                    table.ForeignKey(
                        name: "FK_EGN_Service_User_Citizen_Master_Nic",
                        column: x => x.Nic,
                        principalTable: "Citizen_Master",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_EGN_Service_User_accounts_Nic",
                        column: x => x.Nic,
                        principalTable: "accounts",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EGN_Service_User_Username",
                table: "EGN_Service_User",
                column: "Username",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_EGN_Service_User_CitizenNic",
                table: "appointments",
                column: "CitizenNic",
                principalTable: "EGN_Service_User",
                principalColumn: "Nic",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_conversations_EGN_Service_User_CitizenNic",
                table: "conversations",
                column: "CitizenNic",
                principalTable: "EGN_Service_User",
                principalColumn: "Nic",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_poll_votes_EGN_Service_User_CitizenNic",
                table: "poll_votes",
                column: "CitizenNic",
                principalTable: "EGN_Service_User",
                principalColumn: "Nic",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_survey_responses_EGN_Service_User_CitizenNic",
                table: "survey_responses",
                column: "CitizenNic",
                principalTable: "EGN_Service_User",
                principalColumn: "Nic",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
