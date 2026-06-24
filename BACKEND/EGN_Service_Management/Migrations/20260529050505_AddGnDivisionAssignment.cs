using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EGN_Service_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddGnDivisionAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // migrationBuilder.DropColumn(
            //     name: "Role",
            //     table: "accounts");

            migrationBuilder.CreateTable(
                name: "gn_division_assignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GnOfficerNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GnDivisionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gn_division_assignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gn_division_assignments_gn_divisions_GnDivisionId",
                        column: x => x.GnDivisionId,
                        principalTable: "gn_divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gn_division_assignments_gn_officers_GnOfficerNic",
                        column: x => x.GnOfficerNic,
                        principalTable: "gn_officers",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gn_division_assignments_GnDivisionId",
                table: "gn_division_assignments",
                column: "GnDivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_gn_division_assignments_GnOfficerNic",
                table: "gn_division_assignments",
                column: "GnOfficerNic");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gn_division_assignments");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "accounts",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");
        }
    }
}

