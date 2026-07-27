using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EGN_Service_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestStatusHistoryIdentityKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Request_Status_History",
                table: "Request_Status_History");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "Request_Status_History",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Request_Status_History",
                table: "Request_Status_History",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_Request_Status_History_RequestId",
                table: "Request_Status_History",
                column: "RequestId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_Request_Status_History",
                table: "Request_Status_History");

            migrationBuilder.DropIndex(
                name: "IX_Request_Status_History_RequestId",
                table: "Request_Status_History");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "Request_Status_History");

            migrationBuilder.AddPrimaryKey(
                name: "PK_Request_Status_History",
                table: "Request_Status_History",
                column: "RequestId");
        }
    }
}
