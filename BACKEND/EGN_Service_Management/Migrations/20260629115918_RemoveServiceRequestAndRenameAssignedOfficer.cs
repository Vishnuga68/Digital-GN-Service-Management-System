using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EGN_Service_Management.Migrations
{
    /// <inheritdoc />
    public partial class RemoveServiceRequestAndRenameAssignedOfficer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_conversations_service_requests_RequestId",
                table: "conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_gn_document_logs_gn_documents_DocumentId",
                table: "gn_document_logs");

            migrationBuilder.DropForeignKey(
                name: "FK_Requests_gn_officers_AssignedGnDivisionNic",
                table: "Requests");

            migrationBuilder.DropTable(
                name: "request_attachments");

            migrationBuilder.DropTable(
                name: "request_remarks");

            migrationBuilder.DropTable(
                name: "request_timelines");

            migrationBuilder.DropTable(
                name: "service_requests");

            migrationBuilder.DropIndex(
                name: "IX_gn_document_logs_DocumentId",
                table: "gn_document_logs");

            migrationBuilder.DropIndex(
                name: "IX_conversations_RequestId",
                table: "conversations");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "document_vault_documents");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "document_vault_documents");

            migrationBuilder.DropColumn(
                name: "DeletedBy",
                table: "document_vault_documents");

            migrationBuilder.DropColumn(
                name: "FileSize",
                table: "document_vault_documents");

            migrationBuilder.RenameColumn(
                name: "AssignedGnDivisionNic",
                table: "Requests",
                newName: "AssignedOfficerNic");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_AssignedGnDivisionNic",
                table: "Requests",
                newName: "IX_Requests_AssignedOfficerNic");

            migrationBuilder.AddColumn<string>(
                name: "CategoryName",
                table: "document_vault_documents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "document_vault_categories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NIC = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CategoriesJson = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_vault_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "gn_announcements",
                columns: table => new
                {
                    AnnouncementId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GnOfficerNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GnDivisionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AnnouncementType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DocumentId = table.Column<int>(type: "int", nullable: true),
                    ExternalSurveyUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PollQuestion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PublishDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ModifiedBy = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gn_announcements", x => x.AnnouncementId);
                    table.ForeignKey(
                        name: "FK_gn_announcements_gn_divisions_GnDivisionId",
                        column: x => x.GnDivisionId,
                        principalTable: "gn_divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gn_announcements_gn_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "gn_documents",
                        principalColumn: "DocumentId");
                    table.ForeignKey(
                        name: "FK_gn_announcements_gn_officers_GnOfficerNic",
                        column: x => x.GnOfficerNic,
                        principalTable: "gn_officers",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gn_poll_options",
                columns: table => new
                {
                    OptionId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnnouncementId = table.Column<int>(type: "int", nullable: false),
                    OptionText = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gn_poll_options", x => x.OptionId);
                    table.ForeignKey(
                        name: "FK_gn_poll_options_gn_announcements_AnnouncementId",
                        column: x => x.AnnouncementId,
                        principalTable: "gn_announcements",
                        principalColumn: "AnnouncementId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "gn_poll_votes",
                columns: table => new
                {
                    VoteId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnnouncementId = table.Column<int>(type: "int", nullable: false),
                    OptionId = table.Column<int>(type: "int", nullable: false),
                    CitizenNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    VotedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gn_poll_votes", x => x.VoteId);
                    table.ForeignKey(
                        name: "FK_gn_poll_votes_Citizen_Master_CitizenNic",
                        column: x => x.CitizenNic,
                        principalTable: "Citizen_Master",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gn_poll_votes_gn_announcements_AnnouncementId",
                        column: x => x.AnnouncementId,
                        principalTable: "gn_announcements",
                        principalColumn: "AnnouncementId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gn_poll_votes_gn_poll_options_OptionId",
                        column: x => x.OptionId,
                        principalTable: "gn_poll_options",
                        principalColumn: "OptionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gn_announcements_DocumentId",
                table: "gn_announcements",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_gn_announcements_GnDivisionId",
                table: "gn_announcements",
                column: "GnDivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_gn_announcements_GnOfficerNic",
                table: "gn_announcements",
                column: "GnOfficerNic");

            migrationBuilder.CreateIndex(
                name: "IX_gn_poll_options_AnnouncementId",
                table: "gn_poll_options",
                column: "AnnouncementId");

            migrationBuilder.CreateIndex(
                name: "IX_gn_poll_votes_AnnouncementId_CitizenNic",
                table: "gn_poll_votes",
                columns: new[] { "AnnouncementId", "CitizenNic" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gn_poll_votes_CitizenNic",
                table: "gn_poll_votes",
                column: "CitizenNic");

            migrationBuilder.CreateIndex(
                name: "IX_gn_poll_votes_OptionId",
                table: "gn_poll_votes",
                column: "OptionId");

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_gn_officers_AssignedOfficerNic",
                table: "Requests",
                column: "AssignedOfficerNic",
                principalTable: "gn_officers",
                principalColumn: "Nic",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requests_gn_officers_AssignedOfficerNic",
                table: "Requests");

            migrationBuilder.DropTable(
                name: "document_vault_categories");

            migrationBuilder.DropTable(
                name: "gn_poll_votes");

            migrationBuilder.DropTable(
                name: "gn_poll_options");

            migrationBuilder.DropTable(
                name: "gn_announcements");

            migrationBuilder.DropColumn(
                name: "CategoryName",
                table: "document_vault_documents");

            migrationBuilder.RenameColumn(
                name: "AssignedOfficerNic",
                table: "Requests",
                newName: "AssignedGnDivisionNic");

            migrationBuilder.RenameIndex(
                name: "IX_Requests_AssignedOfficerNic",
                table: "Requests",
                newName: "IX_Requests_AssignedGnDivisionNic");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "document_vault_documents",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "document_vault_documents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedBy",
                table: "document_vault_documents",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FileSize",
                table: "document_vault_documents",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "service_requests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CitizenNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GnDivisionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CurrentStep = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FinalDocumentUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_service_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_service_requests_Citizen_Master_CitizenNic",
                        column: x => x.CitizenNic,
                        principalTable: "Citizen_Master",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_service_requests_gn_divisions_GnDivisionId",
                        column: x => x.GnDivisionId,
                        principalTable: "gn_divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "request_attachments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileSize = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_attachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_request_attachments_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "service_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "request_remarks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AuthorNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RemarkText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RoleLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_remarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_request_remarks_accounts_AuthorNic",
                        column: x => x.AuthorNic,
                        principalTable: "accounts",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_request_remarks_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "service_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "request_timelines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OfficerNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    RequestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    Step = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_request_timelines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_request_timelines_accounts_OfficerNic",
                        column: x => x.OfficerNic,
                        principalTable: "accounts",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_request_timelines_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "service_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gn_document_logs_DocumentId",
                table: "gn_document_logs",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_RequestId",
                table: "conversations",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_request_attachments_RequestId",
                table: "request_attachments",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_request_remarks_AuthorNic",
                table: "request_remarks",
                column: "AuthorNic");

            migrationBuilder.CreateIndex(
                name: "IX_request_remarks_RequestId",
                table: "request_remarks",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_request_timelines_OfficerNic",
                table: "request_timelines",
                column: "OfficerNic");

            migrationBuilder.CreateIndex(
                name: "IX_request_timelines_RequestId",
                table: "request_timelines",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_CitizenNic",
                table: "service_requests",
                column: "CitizenNic");

            migrationBuilder.CreateIndex(
                name: "IX_service_requests_GnDivisionId",
                table: "service_requests",
                column: "GnDivisionId");

            migrationBuilder.AddForeignKey(
                name: "FK_conversations_service_requests_RequestId",
                table: "conversations",
                column: "RequestId",
                principalTable: "service_requests",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_gn_document_logs_gn_documents_DocumentId",
                table: "gn_document_logs",
                column: "DocumentId",
                principalTable: "gn_documents",
                principalColumn: "DocumentId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Requests_gn_officers_AssignedGnDivisionNic",
                table: "Requests",
                column: "AssignedGnDivisionNic",
                principalTable: "gn_officers",
                principalColumn: "Nic",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
