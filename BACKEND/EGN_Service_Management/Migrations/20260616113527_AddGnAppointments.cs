using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EGN_Service_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddGnAppointments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_appointments",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "Service",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "TimeSlot",
                table: "appointments");

            migrationBuilder.AddColumn<int>(
                name: "AppointmentId",
                table: "appointments",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<string>(
                name: "CancellationReason",
                table: "appointments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "EndTime",
                table: "appointments",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<int>(
                name: "RescheduledFrom",
                table: "appointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ServiceType",
                table: "appointments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<TimeSpan>(
                name: "StartTime",
                table: "appointments",
                type: "time",
                nullable: false,
                defaultValue: new TimeSpan(0, 0, 0, 0, 0));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "appointments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_appointments",
                table: "appointments",
                column: "AppointmentId");

            migrationBuilder.CreateTable(
                name: "gn_availability",
                columns: table => new
                {
                    AvailabilityId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GnOfficerNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AvailableDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gn_availability", x => x.AvailabilityId);
                    table.ForeignKey(
                        name: "FK_gn_availability_gn_officers_GnOfficerNic",
                        column: x => x.GnOfficerNic,
                        principalTable: "gn_officers",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gn_availability_GnOfficerNic_AvailableDate",
                table: "gn_availability",
                columns: new[] { "GnOfficerNic", "AvailableDate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gn_availability");

            migrationBuilder.DropPrimaryKey(
                name: "PK_appointments",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "CancellationReason",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "EndTime",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "RescheduledFrom",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "ServiceType",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "StartTime",
                table: "appointments");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "appointments");

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "appointments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Service",
                table: "appointments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "TimeSlot",
                table: "appointments",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_appointments",
                table: "appointments",
                column: "Id");
        }
    }
}
