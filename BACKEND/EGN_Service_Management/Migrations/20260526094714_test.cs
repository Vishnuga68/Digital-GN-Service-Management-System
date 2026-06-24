using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EGN_Service_Management.Migrations
{
    /// <inheritdoc />
    public partial class test : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "accounts",
                columns: table => new
                {
                    Nic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Role = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    LastLogin = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.Nic);
                });

            migrationBuilder.CreateTable(
                name: "document_categories",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_document_categories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ds_divisions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Province = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ds_divisions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipientNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_notifications_accounts_RecipientNic",
                        column: x => x.RecipientNic,
                        principalTable: "accounts",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ds_officers",
                columns: table => new
                {
                    Nic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DsDivisionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ds_officers", x => x.Nic);
                    table.ForeignKey(
                        name: "FK_ds_officers_accounts_Nic",
                        column: x => x.Nic,
                        principalTable: "accounts",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ds_officers_ds_divisions_DsDivisionId",
                        column: x => x.DsDivisionId,
                        principalTable: "ds_divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "gn_divisions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DsDivisionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gn_divisions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gn_divisions_ds_divisions_DsDivisionId",
                        column: x => x.DsDivisionId,
                        principalTable: "ds_divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "gn_officers",
                columns: table => new
                {
                    Nic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    GnDivisionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gn_officers", x => x.Nic);
                    table.ForeignKey(
                        name: "FK_gn_officers_accounts_Nic",
                        column: x => x.Nic,
                        principalTable: "accounts",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gn_officers_gn_divisions_GnDivisionId",
                        column: x => x.GnDivisionId,
                        principalTable: "gn_divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "announcements",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GnOfficerNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<bool>(type: "bit", nullable: false),
                    ActionLabel = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ActionLink = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    AttachmentName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    EstimatedTime = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    SurveyLink = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_announcements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_announcements_gn_officers_GnOfficerNic",
                        column: x => x.GnOfficerNic,
                        principalTable: "gn_officers",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "download_forms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    CategoryId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileSize = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    DownloadsCount = table.Column<int>(type: "int", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    UploadedByNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_download_forms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_download_forms_document_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "document_categories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_download_forms_gn_officers_UploadedByNic",
                        column: x => x.UploadedByNic,
                        principalTable: "gn_officers",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "gn_leaves_holidays",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GnOfficerNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gn_leaves_holidays", x => x.Id);
                    table.ForeignKey(
                        name: "FK_gn_leaves_holidays_gn_officers_GnOfficerNic",
                        column: x => x.GnOfficerNic,
                        principalTable: "gn_officers",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transfer_requests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    GnOfficerNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CurrentDivisionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestedDivisionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    DocumentName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    DocumentUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SubmittedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReviewedByNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ReviewedDate = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfer_requests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transfer_requests_ds_officers_ReviewedByNic",
                        column: x => x.ReviewedByNic,
                        principalTable: "ds_officers",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_transfer_requests_gn_divisions_CurrentDivisionId",
                        column: x => x.CurrentDivisionId,
                        principalTable: "gn_divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transfer_requests_gn_divisions_RequestedDivisionId",
                        column: x => x.RequestedDivisionId,
                        principalTable: "gn_divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transfer_requests_gn_officers_GnOfficerNic",
                        column: x => x.GnOfficerNic,
                        principalTable: "gn_officers",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "poll_options",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AnnouncementId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    OptionText = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poll_options", x => x.Id);
                    table.ForeignKey(
                        name: "FK_poll_options_announcements_AnnouncementId",
                        column: x => x.AnnouncementId,
                        principalTable: "announcements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "announcement_target_citizens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnnouncementId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CitizenNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_announcement_target_citizens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_announcement_target_citizens_announcements_AnnouncementId",
                        column: x => x.AnnouncementId,
                        principalTable: "announcements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "appointments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CitizenNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GnOfficerNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AppointmentDate = table.Column<DateOnly>(type: "date", nullable: false),
                    TimeSlot = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Service = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_appointments_gn_officers_GnOfficerNic",
                        column: x => x.GnOfficerNic,
                        principalTable: "gn_officers",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "chat_attachments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MessageId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_attachments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ConversationId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    SenderNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MessageText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_chat_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_chat_messages_accounts_SenderNic",
                        column: x => x.SenderNic,
                        principalTable: "accounts",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Citizen_Master",
                columns: table => new
                {
                    Nic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Dob = table.Column<DateOnly>(type: "date", nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    MaritalStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Religion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Occupation = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FamilyCardNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RelationshipToHead = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LifeStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citizen_Master", x => x.Nic);
                });

            migrationBuilder.CreateTable(
                name: "citizen_special_statuses",
                columns: table => new
                {
                    CitizenNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IsNalanpuri = table.Column<bool>(type: "bit", nullable: false),
                    HasKidneyDisease = table.Column<bool>(type: "bit", nullable: false),
                    HasCancer = table.Column<bool>(type: "bit", nullable: false),
                    HasOtherDisability = table.Column<bool>(type: "bit", nullable: false),
                    WelfareDetails = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_citizen_special_statuses", x => x.CitizenNic);
                    table.ForeignKey(
                        name: "FK_citizen_special_statuses_Citizen_Master_CitizenNic",
                        column: x => x.CitizenNic,
                        principalTable: "Citizen_Master",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EGN_Service_User",
                columns: table => new
                {
                    Nic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RegisteredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "family_cards",
                columns: table => new
                {
                    FamilyCardNumber = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    HeadOfHouseholdNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GnDivisionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegisteredDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_family_cards", x => x.FamilyCardNumber);
                    table.ForeignKey(
                        name: "FK_family_cards_Citizen_Master_HeadOfHouseholdNic",
                        column: x => x.HeadOfHouseholdNic,
                        principalTable: "Citizen_Master",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_family_cards_gn_divisions_GnDivisionId",
                        column: x => x.GnDivisionId,
                        principalTable: "gn_divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "service_requests",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CitizenNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GnDivisionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ServiceType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CurrentStep = table.Column<int>(type: "int", nullable: false),
                    RejectionReason = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FinalDocumentUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    SubmittedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                name: "poll_votes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnnouncementId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CitizenNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PollOptionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_poll_votes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_poll_votes_EGN_Service_User_CitizenNic",
                        column: x => x.CitizenNic,
                        principalTable: "EGN_Service_User",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_poll_votes_announcements_AnnouncementId",
                        column: x => x.AnnouncementId,
                        principalTable: "announcements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_poll_votes_poll_options_PollOptionId",
                        column: x => x.PollOptionId,
                        principalTable: "poll_options",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "survey_responses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AnnouncementId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CitizenNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ResponseText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_survey_responses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_survey_responses_EGN_Service_User_CitizenNic",
                        column: x => x.CitizenNic,
                        principalTable: "EGN_Service_User",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_survey_responses_announcements_AnnouncementId",
                        column: x => x.AnnouncementId,
                        principalTable: "announcements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conversations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CitizenNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    GnOfficerNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conversations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_conversations_EGN_Service_User_CitizenNic",
                        column: x => x.CitizenNic,
                        principalTable: "EGN_Service_User",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_conversations_gn_officers_GnOfficerNic",
                        column: x => x.GnOfficerNic,
                        principalTable: "gn_officers",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_conversations_service_requests_RequestId",
                        column: x => x.RequestId,
                        principalTable: "service_requests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "request_attachments",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RequestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileSize = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
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
                    RequestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    AuthorNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    RoleLabel = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RemarkText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
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
                    RequestId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Step = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    OfficerNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
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
                name: "IX_accounts_Email",
                table: "accounts",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_announcement_target_citizens_AnnouncementId_CitizenNic",
                table: "announcement_target_citizens",
                columns: new[] { "AnnouncementId", "CitizenNic" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_announcement_target_citizens_CitizenNic",
                table: "announcement_target_citizens",
                column: "CitizenNic");

            migrationBuilder.CreateIndex(
                name: "IX_announcements_GnOfficerNic",
                table: "announcements",
                column: "GnOfficerNic");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_CitizenNic",
                table: "appointments",
                column: "CitizenNic");

            migrationBuilder.CreateIndex(
                name: "IX_appointments_GnOfficerNic",
                table: "appointments",
                column: "GnOfficerNic");

            migrationBuilder.CreateIndex(
                name: "IX_chat_attachments_MessageId",
                table: "chat_attachments",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_ConversationId",
                table: "chat_messages",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_chat_messages_SenderNic",
                table: "chat_messages",
                column: "SenderNic");

            migrationBuilder.CreateIndex(
                name: "IX_Citizen_Master_FamilyCardNumber",
                table: "Citizen_Master",
                column: "FamilyCardNumber");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_CitizenNic",
                table: "conversations",
                column: "CitizenNic");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_GnOfficerNic",
                table: "conversations",
                column: "GnOfficerNic");

            migrationBuilder.CreateIndex(
                name: "IX_conversations_RequestId",
                table: "conversations",
                column: "RequestId");

            migrationBuilder.CreateIndex(
                name: "IX_document_categories_Name",
                table: "document_categories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_download_forms_CategoryId",
                table: "download_forms",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_download_forms_UploadedByNic",
                table: "download_forms",
                column: "UploadedByNic");

            migrationBuilder.CreateIndex(
                name: "IX_ds_divisions_Name",
                table: "ds_divisions",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ds_officers_DsDivisionId",
                table: "ds_officers",
                column: "DsDivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_EGN_Service_User_Username",
                table: "EGN_Service_User",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_family_cards_GnDivisionId",
                table: "family_cards",
                column: "GnDivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_family_cards_HeadOfHouseholdNic",
                table: "family_cards",
                column: "HeadOfHouseholdNic");

            migrationBuilder.CreateIndex(
                name: "IX_gn_divisions_DsDivisionId",
                table: "gn_divisions",
                column: "DsDivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_gn_divisions_Name_DsDivisionId",
                table: "gn_divisions",
                columns: new[] { "Name", "DsDivisionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_gn_leaves_holidays_GnOfficerNic",
                table: "gn_leaves_holidays",
                column: "GnOfficerNic");

            migrationBuilder.CreateIndex(
                name: "IX_gn_officers_GnDivisionId",
                table: "gn_officers",
                column: "GnDivisionId",
                unique: true,
                filter: "[GnDivisionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_notifications_RecipientNic",
                table: "notifications",
                column: "RecipientNic");

            migrationBuilder.CreateIndex(
                name: "IX_poll_options_AnnouncementId",
                table: "poll_options",
                column: "AnnouncementId");

            migrationBuilder.CreateIndex(
                name: "IX_poll_votes_AnnouncementId_CitizenNic",
                table: "poll_votes",
                columns: new[] { "AnnouncementId", "CitizenNic" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_poll_votes_CitizenNic",
                table: "poll_votes",
                column: "CitizenNic");

            migrationBuilder.CreateIndex(
                name: "IX_poll_votes_PollOptionId",
                table: "poll_votes",
                column: "PollOptionId");

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

            migrationBuilder.CreateIndex(
                name: "IX_survey_responses_AnnouncementId",
                table: "survey_responses",
                column: "AnnouncementId");

            migrationBuilder.CreateIndex(
                name: "IX_survey_responses_CitizenNic",
                table: "survey_responses",
                column: "CitizenNic");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_requests_CurrentDivisionId",
                table: "transfer_requests",
                column: "CurrentDivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_requests_GnOfficerNic",
                table: "transfer_requests",
                column: "GnOfficerNic");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_requests_RequestedDivisionId",
                table: "transfer_requests",
                column: "RequestedDivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_requests_ReviewedByNic",
                table: "transfer_requests",
                column: "ReviewedByNic");

            migrationBuilder.AddForeignKey(
                name: "FK_announcement_target_citizens_Citizen_Master_CitizenNic",
                table: "announcement_target_citizens",
                column: "CitizenNic",
                principalTable: "Citizen_Master",
                principalColumn: "Nic",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_appointments_EGN_Service_User_CitizenNic",
                table: "appointments",
                column: "CitizenNic",
                principalTable: "EGN_Service_User",
                principalColumn: "Nic",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_chat_attachments_chat_messages_MessageId",
                table: "chat_attachments",
                column: "MessageId",
                principalTable: "chat_messages",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_chat_messages_conversations_ConversationId",
                table: "chat_messages",
                column: "ConversationId",
                principalTable: "conversations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Citizen_Master_family_cards_FamilyCardNumber",
                table: "Citizen_Master",
                column: "FamilyCardNumber",
                principalTable: "family_cards",
                principalColumn: "FamilyCardNumber",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_family_cards_Citizen_Master_HeadOfHouseholdNic",
                table: "family_cards");

            migrationBuilder.DropTable(
                name: "announcement_target_citizens");

            migrationBuilder.DropTable(
                name: "appointments");

            migrationBuilder.DropTable(
                name: "chat_attachments");

            migrationBuilder.DropTable(
                name: "citizen_special_statuses");

            migrationBuilder.DropTable(
                name: "download_forms");

            migrationBuilder.DropTable(
                name: "gn_leaves_holidays");

            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropTable(
                name: "poll_votes");

            migrationBuilder.DropTable(
                name: "request_attachments");

            migrationBuilder.DropTable(
                name: "request_remarks");

            migrationBuilder.DropTable(
                name: "request_timelines");

            migrationBuilder.DropTable(
                name: "survey_responses");

            migrationBuilder.DropTable(
                name: "transfer_requests");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "document_categories");

            migrationBuilder.DropTable(
                name: "poll_options");

            migrationBuilder.DropTable(
                name: "ds_officers");

            migrationBuilder.DropTable(
                name: "conversations");

            migrationBuilder.DropTable(
                name: "announcements");

            migrationBuilder.DropTable(
                name: "EGN_Service_User");

            migrationBuilder.DropTable(
                name: "service_requests");

            migrationBuilder.DropTable(
                name: "gn_officers");

            migrationBuilder.DropTable(
                name: "accounts");

            migrationBuilder.DropTable(
                name: "Citizen_Master");

            migrationBuilder.DropTable(
                name: "family_cards");

            migrationBuilder.DropTable(
                name: "gn_divisions");

            migrationBuilder.DropTable(
                name: "ds_divisions");
        }
    }
}

