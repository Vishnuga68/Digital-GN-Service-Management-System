using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EGN_Service_Management.Migrations
{
    /// <inheritdoc />
    public partial class AddGNDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "download_forms");

            migrationBuilder.DropTable(
                name: "gn_leaves_holidays");

            migrationBuilder.DropPrimaryKey(
                name: "PK_document_categories",
                table: "document_categories");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "document_categories");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "document_categories",
                newName: "CategoryName");

            migrationBuilder.RenameIndex(
                name: "IX_document_categories_Name",
                table: "document_categories",
                newName: "IX_document_categories_CategoryName");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "document_categories",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "document_categories",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_document_categories",
                table: "document_categories",
                column: "CategoryId");

            migrationBuilder.CreateTable(
                name: "gn_documents",
                columns: table => new
                {
                    DocumentId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    GnOfficerNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DivisionId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    PublishDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gn_documents", x => x.DocumentId);
                    table.ForeignKey(
                        name: "FK_gn_documents_document_categories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "document_categories",
                        principalColumn: "CategoryId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gn_documents_gn_divisions_DivisionId",
                        column: x => x.DivisionId,
                        principalTable: "gn_divisions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_gn_documents_gn_officers_GnOfficerNic",
                        column: x => x.GnOfficerNic,
                        principalTable: "gn_officers",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "gn_document_logs",
                columns: table => new
                {
                    LogId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DocumentId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ActionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PerformedByNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_gn_document_logs", x => x.LogId);
                    table.ForeignKey(
                        name: "FK_gn_document_logs_gn_documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "gn_documents",
                        principalColumn: "DocumentId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_gn_document_logs_gn_officers_PerformedByNic",
                        column: x => x.PerformedByNic,
                        principalTable: "gn_officers",
                        principalColumn: "Nic",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_gn_document_logs_DocumentId",
                table: "gn_document_logs",
                column: "DocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_gn_document_logs_PerformedByNic",
                table: "gn_document_logs",
                column: "PerformedByNic");

            migrationBuilder.CreateIndex(
                name: "IX_gn_documents_CategoryId",
                table: "gn_documents",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_gn_documents_DivisionId",
                table: "gn_documents",
                column: "DivisionId");

            migrationBuilder.CreateIndex(
                name: "IX_gn_documents_GnOfficerNic",
                table: "gn_documents",
                column: "GnOfficerNic");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "gn_document_logs");

            migrationBuilder.DropTable(
                name: "gn_documents");

            migrationBuilder.DropPrimaryKey(
                name: "PK_document_categories",
                table: "document_categories");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "document_categories");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "document_categories");

            migrationBuilder.RenameColumn(
                name: "CategoryName",
                table: "document_categories",
                newName: "Name");

            migrationBuilder.RenameIndex(
                name: "IX_document_categories_CategoryName",
                table: "document_categories",
                newName: "IX_document_categories_Name");

            migrationBuilder.AddColumn<string>(
                name: "Id",
                table: "document_categories",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_document_categories",
                table: "document_categories",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "download_forms",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CategoryId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UploadedByNic = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DownloadsCount = table.Column<int>(type: "int", nullable: false),
                    FileSize = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
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
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EventDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EventType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_download_forms_CategoryId",
                table: "download_forms",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_download_forms_UploadedByNic",
                table: "download_forms",
                column: "UploadedByNic");

            migrationBuilder.CreateIndex(
                name: "IX_gn_leaves_holidays_GnOfficerNic",
                table: "gn_leaves_holidays",
                column: "GnOfficerNic");
        }
    }
}
