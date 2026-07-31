using EGN_Service_Management.Database;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;
using EGN_Service_Management.Repositories;
using EGN_Service_Management.Services;
using EGN_Service_Management.Middleware;
using EGN_Service_Management.Mappings;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(option =>
{
    option.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    option.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddEndpointsApiExplorer();

// Swagger security setup
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo { Title = "Digital GN API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddDbContext<EgnDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("EGNServiceDB")));

// AutoMapper Setup
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

// Token Service Setup
builder.Services.AddScoped<ITokenService, TokenService>();

// JWT Authentication Configuration
var jwtKey = builder.Configuration["JwtSettings:Key"] ?? "SUPER_SECRET_KEY_DO_NOT_SHARE_WITH_ANYONE_1234567890";
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "EgnServiceManagementSystem",
        ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "EgnServiceManagementClients",
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});


// Repository registrations
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
builder.Services.AddScoped<IAnnouncementTargetCitizenRepository, AnnouncementTargetCitizenRepository>();
builder.Services.AddScoped<IChatAttachmentRepository, ChatAttachmentRepository>();
builder.Services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
builder.Services.AddScoped<ICitizenMasterRepository, CitizenMasterRepository>();
builder.Services.AddScoped<IConversationRepository, ConversationRepository>();
builder.Services.AddScoped<IGNDocumentRepository, GNDocumentRepository>();
builder.Services.AddScoped<IDocumentVaultRepository, DocumentVaultRepository>();
builder.Services.AddScoped<IDocumentVaultCategoryRepository, DocumentVaultCategoryRepository>();
builder.Services.AddScoped<IDocumentCategoryRepository, DocumentCategoryRepository>();
builder.Services.AddScoped<IDsDivisionRepository, DsDivisionRepository>();
builder.Services.AddScoped<IDsOfficerRepository, DsOfficerRepository>();
builder.Services.AddScoped<IEgnServiceUserRepository, EgnServiceUserRepository>();
builder.Services.AddScoped<IFamilyCardRepository, FamilyCardRepository>();
builder.Services.AddScoped<IGnDivisionRepository, GnDivisionRepository>();
builder.Services.AddScoped<IGnOfficerRepository, GnOfficerRepository>();
// builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IPollOptionRepository, PollOptionRepository>();
builder.Services.AddScoped<IPollVoteRepository, PollVoteRepository>();
builder.Services.AddScoped<IRequestAttachmentRepository, RequestAttachmentRepository>();
builder.Services.AddScoped<IRequestRemarkRepository, RequestRemarkRepository>();
builder.Services.AddScoped<IRequestTimelineRepository, RequestTimelineRepository>();
builder.Services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();
builder.Services.AddScoped<ISurveyResponseRepository, SurveyResponseRepository>();
builder.Services.AddScoped<ITransferRequestRepository, TransferRequestRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();

// Service registrations
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IChatService, ChatService>();
builder.Services.AddScoped<INewChatAttachmentService, NewChatAttachmentService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IAnnouncementTargetCitizenService, AnnouncementTargetCitizenService>();

builder.Services.AddScoped<IChatAttachmentService, ChatAttachmentService>();
builder.Services.AddScoped<IChatMessageService, ChatMessageService>();
builder.Services.AddScoped<ICitizenMasterService, CitizenMasterService>();
builder.Services.AddScoped<IConversationService, ConversationService>();
builder.Services.AddScoped<IGNDocumentService, GNDocumentService>();
builder.Services.AddScoped<IDocumentVaultService, DocumentVaultService>();
builder.Services.AddScoped<IDocumentVaultCategoryService, DocumentVaultCategoryService>();
builder.Services.AddScoped<IDocumentCategoryService, DocumentCategoryService>();
builder.Services.AddScoped<IDsDivisionService, DsDivisionService>();
builder.Services.AddScoped<IDsOfficerService, DsOfficerService>();
builder.Services.AddScoped<IEgnServiceUserService, EgnServiceUserService>();
builder.Services.AddScoped<IFamilyCardService, FamilyCardService>();
builder.Services.AddScoped<IAppointmentService, AppointmentService>();
builder.Services.AddScoped<IGnDivisionService, GnDivisionService>();
builder.Services.AddScoped<IGnOfficerService, GnOfficerService>();
// builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IPollOptionService, PollOptionService>();
builder.Services.AddScoped<IPollVoteService, PollVoteService>();
builder.Services.AddScoped<IRequestAttachmentService, RequestAttachmentService>();
builder.Services.AddScoped<IRequestRemarkService, RequestRemarkService>();
builder.Services.AddScoped<IRequestTimelineService, RequestTimelineService>();
builder.Services.AddScoped<IServiceRequestService, ServiceRequestService>();
builder.Services.AddScoped<ISurveyResponseService, SurveyResponseService>();
builder.Services.AddScoped<ITransferRequestService, TransferRequestService>();

// Gn Announcement Module
builder.Services.AddScoped<IGnAnnouncementRepository, GnAnnouncementRepository>();
builder.Services.AddScoped<IGnAnnouncementService, GnAnnouncementService>();

builder.Services.AddScoped<ICitizenRequestService, CitizenRequestService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("CORSOpenPolicy", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

var app = builder.Build();


// Global Exception Handler Middleware (placed first)
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("CORSOpenPolicy");
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<GnActiveAssignmentMiddleware>();

// Automatic database migrations/updates for Document Verification Phase 1
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<EgnDbContext>();
    try
    {
        var sql = @"
            -- Alter Request_Attachments table to add new columns
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Request_Attachments]') AND name = N'DocumentRole')
            BEGIN
                ALTER TABLE [Request_Attachments] ADD [DocumentRole] nvarchar(50) NULL;
            END

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Request_Attachments]') AND name = N'DisplayName')
            BEGIN
                ALTER TABLE [Request_Attachments] ADD [DisplayName] nvarchar(255) NULL;
            END

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Request_Attachments]') AND name = N'IsPrimary')
            BEGIN
                ALTER TABLE [Request_Attachments] ADD [IsPrimary] bit NOT NULL DEFAULT 0;
            END

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Request_Attachments]') AND name = N'UploadedBy')
            BEGIN
                ALTER TABLE [Request_Attachments] ADD [UploadedBy] nvarchar(100) NULL;
            END

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Request_Attachments]') AND name = N'FileSize')
            BEGIN
                ALTER TABLE [Request_Attachments] ADD [FileSize] bigint NULL;
            END

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Request_Attachments]') AND name = N'MimeType')
            BEGIN
                ALTER TABLE [Request_Attachments] ADD [MimeType] nvarchar(100) NULL;
            END

            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Request_Attachments]') AND name = N'IsActive')
            BEGIN
                ALTER TABLE [Request_Attachments] ADD [IsActive] bit NOT NULL DEFAULT 1;
            END

            -- Create Document_Verifications table
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Document_Verifications]') AND type in (N'U'))
            BEGIN
                CREATE TABLE [Document_Verifications] (
                    [VerificationId] int IDENTITY(1,1) NOT NULL,
                    [RequestId] int NOT NULL,
                    [MainAttachmentId] int NOT NULL,
                    [AssignedGNOfficerNIC] nvarchar(20) NULL,
                    [VerificationStatus] nvarchar(50) NOT NULL,
                    [VerificationRemarks] nvarchar(max) NULL,
                    [VerifiedDate] datetime2 NULL,
                    [FinalVerifiedAttachmentId] int NULL,
                    [VerificationTokenHash] nvarchar(255) NULL,
                    [VerificationUrl] nvarchar(500) NULL,
                    [OriginalFileHash] nvarchar(255) NULL,
                    [FinalFileHash] nvarchar(255) NULL,
                    [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                    [UpdatedDate] datetime2 NULL,
                    [RevocationReason] nvarchar(max) NULL,
                    [RevokedDate] datetime2 NULL,
                    [RevokedBy] nvarchar(100) NULL,
                    CONSTRAINT [PK_Document_Verifications] PRIMARY KEY ([VerificationId]),
                    CONSTRAINT [FK_Document_Verifications_Requests_RequestId] FOREIGN KEY ([RequestId]) REFERENCES [Requests] ([RequestId]) ON DELETE CASCADE,
                    CONSTRAINT [FK_Document_Verifications_Request_Attachments_MainAttachmentId] FOREIGN KEY ([MainAttachmentId]) REFERENCES [Request_Attachments] ([AttachmentId]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_Document_Verifications_Request_Attachments_FinalVerifiedAttachmentId] FOREIGN KEY ([FinalVerifiedAttachmentId]) REFERENCES [Request_Attachments] ([AttachmentId]) ON DELETE NO ACTION,
                    CONSTRAINT [FK_Document_Verifications_gn_officers_AssignedGNOfficerNIC] FOREIGN KEY ([AssignedGNOfficerNIC]) REFERENCES [gn_officers] ([Nic]) ON DELETE NO ACTION
                );
            END

            IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = N'IX_Document_Verifications_RequestId' AND object_id = OBJECT_ID(N'[Document_Verifications]'))
            BEGIN
                CREATE UNIQUE INDEX [IX_Document_Verifications_RequestId] ON [Document_Verifications] ([RequestId]);
            END

            -- Create Document_Placement_Elements table
            IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[Document_Placement_Elements]') AND type in (N'U'))
            BEGIN
                CREATE TABLE [Document_Placement_Elements] (
                    [ElementId] int IDENTITY(1,1) NOT NULL,
                    [VerificationId] int NOT NULL,
                    [ElementType] nvarchar(50) NOT NULL,
                    [PageNumber] int NOT NULL,
                    [X] float NOT NULL,
                    [Y] float NOT NULL,
                    [Width] float NOT NULL,
                    [Height] float NOT NULL,
                    [Rotation] float NOT NULL,
                    [LayerOrder] int NOT NULL,
                    [TextValue] nvarchar(2000) NULL,
                    [CreatedDate] datetime2 NOT NULL DEFAULT GETUTCDATE(),
                    CONSTRAINT [PK_Document_Placement_Elements] PRIMARY KEY ([ElementId]),
                    CONSTRAINT [FK_Document_Placement_Elements_Document_Verifications_VerificationId] FOREIGN KEY ([VerificationId]) REFERENCES [Document_Verifications] ([VerificationId]) ON DELETE CASCADE
                );
            END

            -- Add IsActive column to Citizen_Welfare_Status if it does not exist
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Citizen_Welfare_Status]') AND name = N'IsActive')
            BEGIN
                ALTER TABLE [Citizen_Welfare_Status] ADD [IsActive] bit NOT NULL DEFAULT 1;
            END

            -- Add IsPublicAssistanceEligible column to Citizen_Welfare_Status if it does not exist
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Citizen_Welfare_Status]') AND name = N'IsPublicAssistanceEligible')
            BEGIN
                ALTER TABLE [Citizen_Welfare_Status] ADD [IsPublicAssistanceEligible] bit NOT NULL DEFAULT 0;
            END

            -- Add IsHousingAssistanceEligible column to Citizen_Welfare_Status if it does not exist
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[Citizen_Welfare_Status]') AND name = N'IsHousingAssistanceEligible')
            BEGIN
                ALTER TABLE [Citizen_Welfare_Status] ADD [IsHousingAssistanceEligible] bit NOT NULL DEFAULT 0;
            END

            -- Safely Drop citizen_special_statuses table if it exists
            IF EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[citizen_special_statuses]') AND type in (N'U'))
            BEGIN
                DROP TABLE [citizen_special_statuses];
            END

            -- Insert the newFamilyHead field for Category 3 if it does not exist
            IF NOT EXISTS (SELECT * FROM [Service_Form_Schema] WHERE [CategoryId] = 3 AND [FieldName] = 'newFamilyHead')
            BEGIN
                INSERT INTO [Service_Form_Schema] (CategoryId, FieldName, FieldLabel, FieldType, DropdownOptions, IsRequired, ValidationRules, DisplayOrder)
                VALUES (3, 'newFamilyHead', 'New Family Head', 'dropdown', NULL, 0, '{{""dataSource"": ""FamilyMembers"", ""filterCriteria"": {{""minAge"": 18}}, ""displayField"": ""fullName"", ""valueField"": ""nic""}}', 14);
            END
        ";
        context.Database.ExecuteSqlRaw(sql);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[DB Startup Schema Migration Error] {ex.Message}");
    }
}

app.MapControllers();
app.Run();


