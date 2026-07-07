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
builder.Services.AddScoped<ICitizenSpecialStatusRepository, CitizenSpecialStatusRepository>();
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
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IPollOptionRepository, PollOptionRepository>();
builder.Services.AddScoped<IPollVoteRepository, PollVoteRepository>();
builder.Services.AddScoped<IRequestAttachmentRepository, RequestAttachmentRepository>();
builder.Services.AddScoped<IRequestRemarkRepository, RequestRemarkRepository>();
builder.Services.AddScoped<IRequestTimelineRepository, RequestTimelineRepository>();
builder.Services.AddScoped<IServiceRequestRepository, ServiceRequestRepository>();
builder.Services.AddScoped<ISurveyResponseRepository, SurveyResponseRepository>();
builder.Services.AddScoped<ITransferRequestRepository, TransferRequestRepository>();

// Service registrations
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IAnnouncementTargetCitizenService, AnnouncementTargetCitizenService>();

builder.Services.AddScoped<IChatAttachmentService, ChatAttachmentService>();
builder.Services.AddScoped<IChatMessageService, ChatMessageService>();
builder.Services.AddScoped<ICitizenMasterService, CitizenMasterService>();
builder.Services.AddScoped<ICitizenSpecialStatusService, CitizenSpecialStatusService>();
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
builder.Services.AddScoped<INotificationService, NotificationService>();
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

app.MapControllers();
app.Run();


