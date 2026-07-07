using EGN_Service_Management.Entity;
using Microsoft.EntityFrameworkCore;

namespace EGN_Service_Management.Database
{
    public class EgnDbContext : DbContext
    {
        public EgnDbContext(DbContextOptions<EgnDbContext> options) : base(options)
        {
        }

        public DbSet<DsDivision> DsDivisions { get; set; }
        public DbSet<GnDivision> GnDivisions { get; set; }
        public DbSet<Province> Provinces { get; set; }
        public DbSet<District> Districts { get; set; }
        public DbSet<Account> Accounts { get; set; }
        public DbSet<DsOfficer> DsOfficers { get; set; }
        public DbSet<GnOfficer> GnOfficers { get; set; }
        public DbSet<CitizenMaster> CitizenMasters { get; set; }
        public DbSet<EgnServiceUser> EgnServiceUsers { get; set; }
        public DbSet<FamilyCard> FamilyCards { get; set; }
        public DbSet<CitizenSpecialStatus> CitizenSpecialStatuses { get; set; }
        
        // Unified Request Engine DbSets
        public DbSet<ServiceParentHeading> ServiceParentHeadings { get; set; }
        public DbSet<ServiceFormSchema> ServiceFormSchemas { get; set; }
        public DbSet<FamilyMemberHistory> FamilyMemberHistories { get; set; }
        public DbSet<UnifiedService> UnifiedServices { get; set; }
        public DbSet<ServiceCategory> ServiceCategories { get; set; }
        public DbSet<UnifiedRequest> UnifiedRequests { get; set; }
        public DbSet<RequestData> RequestDataEntries { get; set; }
        public DbSet<UnifiedRequestAttachment> UnifiedRequestAttachments { get; set; }
        public DbSet<RequestStatusHistory> RequestStatusHistories { get; set; }
        public DbSet<UnifiedRequestRemark> UnifiedRequestRemarks { get; set; }
        public DbSet<CitizenWelfareStatus> CitizenWelfareStatuses { get; set; }

        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<GnAvailability> GnAvailabilities { get; set; }
        public DbSet<Announcement> Announcements { get; set; }
        public DbSet<AnnouncementTargetCitizen> AnnouncementTargetCitizens { get; set; }
        public DbSet<PollOption> PollOptions { get; set; }
        public DbSet<PollVote> PollVotes { get; set; }
        public DbSet<SurveyResponse> SurveyResponses { get; set; }
        public DbSet<DocumentCategory> DocumentCategories { get; set; }
        public DbSet<GNDocument> GNDocuments { get; set; }
        public DbSet<GNDocumentLog> GNDocumentLogs { get; set; }
        public DbSet<DocumentVaultDocument> DocumentVaultDocuments { get; set; }
        public DbSet<DocumentVaultCategory> DocumentVaultCategories { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<ChatAttachment> ChatAttachments { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<TransferRequest> TransferRequests { get; set; }
        public DbSet<GnDivisionAssignment> GnDivisionAssignments { get; set; }
        
        // New Announcement Module DbSets
        public DbSet<GnAnnouncement> GnAnnouncements { get; set; }
        public DbSet<GnPollOption> GnPollOptions { get; set; }
        public DbSet<GnPollVote> GnPollVotes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure Unique Constraint for GnPollVote
            modelBuilder.Entity<GnPollVote>()
                .HasIndex(v => new { v.AnnouncementId, v.CitizenNic })
                .IsUnique();

            modelBuilder.Entity<DsDivision>()
                .HasIndex(x => x.Name)
                .IsUnique();

            modelBuilder.Entity<GnDivision>()
                .HasIndex(x => new { x.Name, x.DsDivisionId })
                .IsUnique();

            // Province and District Configuration
            modelBuilder.Entity<District>()
                .HasOne(x => x.Province)
                .WithMany(x => x.Districts)
                .HasForeignKey(x => x.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DsDivision>()
                .HasOne(x => x.District)
                .WithMany(x => x.DsDivisions)
                .HasForeignKey(x => x.DistrictId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DsDivision>()
                .HasOne(x => x.Province)
                .WithMany(x => x.DsDivisions)
                .HasForeignKey(x => x.ProvinceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed Data for Provinces
            modelBuilder.Entity<Province>().HasData(
                new Province { Id = 1, Name = "Northern" },
                new Province { Id = 2, Name = "Eastern" },
                new Province { Id = 3, Name = "North Western" },
                new Province { Id = 4, Name = "North Central" },
                new Province { Id = 5, Name = "Central" },
                new Province { Id = 6, Name = "Western" },
                new Province { Id = 7, Name = "Southern" },
                new Province { Id = 8, Name = "Uva" },
                new Province { Id = 9, Name = "Sabaragamuwa" }
            );

            // Seed Data for Districts
            modelBuilder.Entity<District>().HasData(
                // Northern Province
                new District { Id = 1, ProvinceId = 1, Name = "Jaffna" },
                new District { Id = 2, ProvinceId = 1, Name = "Kilinochchi" },
                new District { Id = 3, ProvinceId = 1, Name = "Mannar" },
                new District { Id = 4, ProvinceId = 1, Name = "Mullaitivu" },
                new District { Id = 5, ProvinceId = 1, Name = "Vavuniya" },
                
                // Eastern Province
                new District { Id = 6, ProvinceId = 2, Name = "Trincomalee" },
                new District { Id = 7, ProvinceId = 2, Name = "Batticaloa" },
                new District { Id = 8, ProvinceId = 2, Name = "Ampara" },
                
                // North Western Province
                new District { Id = 9, ProvinceId = 3, Name = "Kurunegala" },
                new District { Id = 10, ProvinceId = 3, Name = "Puttalam" },
                
                // North Central Province
                new District { Id = 11, ProvinceId = 4, Name = "Anuradhapura" },
                new District { Id = 12, ProvinceId = 4, Name = "Polonnaruwa" },
                
                // Central Province
                new District { Id = 13, ProvinceId = 5, Name = "Kandy" },
                new District { Id = 14, ProvinceId = 5, Name = "Matale" },
                new District { Id = 15, ProvinceId = 5, Name = "Nuwara Eliya" },
                
                // Western Province
                new District { Id = 16, ProvinceId = 6, Name = "Colombo" },
                new District { Id = 17, ProvinceId = 6, Name = "Gampaha" },
                new District { Id = 18, ProvinceId = 6, Name = "Kalutara" },
                
                // Southern Province
                new District { Id = 19, ProvinceId = 7, Name = "Galle" },
                new District { Id = 20, ProvinceId = 7, Name = "Matara" },
                new District { Id = 21, ProvinceId = 7, Name = "Hambantota" },
                
                // Uva Province
                new District { Id = 22, ProvinceId = 8, Name = "Badulla" },
                new District { Id = 23, ProvinceId = 8, Name = "Monaragala" },
                
                // Sabaragamuwa Province
                new District { Id = 24, ProvinceId = 9, Name = "Ratnapura" },
                new District { Id = 25, ProvinceId = 9, Name = "Kegalle" }
            );

            modelBuilder.Entity<Account>()
                .HasIndex(x => x.Email)
                .IsUnique()
                .HasFilter("[Email] IS NOT NULL");

            modelBuilder.Entity<GnOfficer>()
                .HasIndex(x => x.GnDivisionId)
                .IsUnique()
                .HasFilter("[GnDivisionId] IS NOT NULL");



            modelBuilder.Entity<DocumentCategory>()
                .HasIndex(x => x.CategoryName)
                .IsUnique();

            modelBuilder.Entity<AnnouncementTargetCitizen>()
                .HasIndex(x => new { x.AnnouncementId, x.CitizenNic })
                .IsUnique();

            modelBuilder.Entity<PollVote>()
                .HasIndex(x => new { x.AnnouncementId, x.CitizenNic })
                .IsUnique();

            modelBuilder.Entity<GnDivision>()
                .HasOne(x => x.DsDivision)
                .WithMany(x => x.GnDivisions)
                .HasForeignKey(x => x.DsDivisionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DsOfficer>()
                .HasOne(x => x.Account)
                .WithOne(x => x.DsOfficer)
                .HasForeignKey<DsOfficer>(x => x.Nic)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DsOfficer>()
                .HasOne(x => x.DsDivision)
                .WithMany(x => x.DsOfficers)
                .HasForeignKey(x => x.DsDivisionId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<GnOfficer>()
                .HasOne(x => x.Account)
                .WithOne(x => x.GnOfficer)
                .HasForeignKey<GnOfficer>(x => x.Nic)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GnOfficer>()
                .HasOne(x => x.GnDivision)
                .WithOne(x => x.GnOfficer)
                .HasForeignKey<GnOfficer>(x => x.GnDivisionId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<EgnServiceUser>()
                .HasOne(x => x.CitizenMaster)
                .WithOne(x => x.EgnServiceUser)
                .HasForeignKey<EgnServiceUser>(x => x.Nic)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<EgnServiceUser>()
                .HasOne(x => x.Account)
                .WithOne(x => x.EgnServiceUser)
                .HasForeignKey<EgnServiceUser>(x => x.Nic)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FamilyCard>()
                .HasOne(x => x.HeadOfHousehold)
                .WithMany(x => x.HeadedFamilyCards)
                .HasForeignKey(x => x.HeadOfHouseholdNic)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FamilyCard>()
                .HasOne(x => x.GnDivision)
                .WithMany(x => x.FamilyCards)
                .HasForeignKey(x => x.GnDivisionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CitizenMaster>()
                .HasOne(x => x.FamilyCard)
                .WithMany(x => x.Citizens)
                .HasForeignKey(x => x.FamilyCardNumber)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CitizenMaster>()
                .HasOne(x => x.GnDivision)
                .WithMany(x => x.Citizens)
                .HasForeignKey(x => x.GnDivisionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CitizenSpecialStatus>()
                .HasOne(x => x.CitizenMaster)
                .WithOne(x => x.SpecialStatus)
                .HasForeignKey<CitizenSpecialStatus>(x => x.CitizenNic)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Appointment>()
                .HasOne(x => x.Citizen)
                .WithMany(x => x.Appointments)
                .HasForeignKey(x => x.CitizenNic)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Appointment>()
                .HasOne(x => x.GnOfficer)
                .WithMany(x => x.Appointments)
                .HasForeignKey(x => x.GnOfficerNic)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GnAvailability>()
                .HasIndex(x => new { x.GnOfficerNic, x.AvailableDate })
                .IsUnique();

            modelBuilder.Entity<GnAvailability>()
                .HasOne(x => x.GnOfficer)
                .WithMany()
                .HasForeignKey(x => x.GnOfficerNic)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Announcement>()
                .HasOne(x => x.GnOfficer)
                .WithMany(x => x.Announcements)
                .HasForeignKey(x => x.GnOfficerNic)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AnnouncementTargetCitizen>()
                .HasOne(x => x.Announcement)
                .WithMany(x => x.TargetCitizens)
                .HasForeignKey(x => x.AnnouncementId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<AnnouncementTargetCitizen>()
                .HasOne(x => x.Citizen)
                .WithMany(x => x.AnnouncementTargets)
                .HasForeignKey(x => x.CitizenNic)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PollOption>()
                .HasOne(x => x.Announcement)
                .WithMany(x => x.PollOptions)
                .HasForeignKey(x => x.AnnouncementId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PollVote>()
                .HasOne(x => x.Announcement)
                .WithMany(x => x.PollVotes)
                .HasForeignKey(x => x.AnnouncementId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PollVote>()
                .HasOne(x => x.Citizen)
                .WithMany(x => x.PollVotes)
                .HasForeignKey(x => x.CitizenNic)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PollVote>()
                .HasOne(x => x.PollOption)
                .WithMany(x => x.PollVotes)
                .HasForeignKey(x => x.PollOptionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SurveyResponse>()
                .HasOne(x => x.Announcement)
                .WithMany(x => x.SurveyResponses)
                .HasForeignKey(x => x.AnnouncementId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SurveyResponse>()
                .HasOne(x => x.Citizen)
                .WithMany(x => x.SurveyResponses)
                .HasForeignKey(x => x.CitizenNic)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GNDocument>()
                .HasOne(x => x.Category)
                .WithMany(x => x.Documents)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GNDocument>()
                .HasOne(x => x.GnOfficer)
                .WithMany()
                .HasForeignKey(x => x.GnOfficerNic)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<GNDocument>()
                .HasOne(x => x.GnDivision)
                .WithMany()
                .HasForeignKey(x => x.DivisionId)
                .OnDelete(DeleteBehavior.Restrict);



            modelBuilder.Entity<GNDocumentLog>()
                .HasOne(x => x.PerformedBy)
                .WithMany()
                .HasForeignKey(x => x.PerformedByNic)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Conversation>()
                .HasOne(x => x.Citizen)
                .WithMany(x => x.Conversations)
                .HasForeignKey(x => x.CitizenNic)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Conversation>()
                .HasOne(x => x.GnOfficer)
                .WithMany(x => x.Conversations)
                .HasForeignKey(x => x.GnOfficerNic)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(x => x.Conversation)
                .WithMany(x => x.ChatMessages)
                .HasForeignKey(x => x.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ChatMessage>()
                .HasOne(x => x.Sender)
                .WithMany(x => x.ChatMessages)
                .HasForeignKey(x => x.SenderNic)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ChatAttachment>()
                .HasOne(x => x.Message)
                .WithMany(x => x.Attachments)
                .HasForeignKey(x => x.MessageId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Notification>()
                .HasOne(x => x.Recipient)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.RecipientNic)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TransferRequest>()
                .HasOne(x => x.GnOfficer)
                .WithMany(x => x.TransferRequests)
                .HasForeignKey(x => x.GnOfficerNic)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransferRequest>()
                .HasOne(x => x.CurrentDivision)
                .WithMany()
                .HasForeignKey(x => x.CurrentDivisionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransferRequest>()
                .HasOne(x => x.RequestedDivision)
                .WithMany()
                .HasForeignKey(x => x.RequestedDivisionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<TransferRequest>()
                .HasOne(x => x.ReviewedBy)
                .WithMany(x => x.ReviewedTransferRequests)
                .HasForeignKey(x => x.ReviewedByNic)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<GnDivisionAssignment>()
                .HasOne(x => x.GnOfficer)
                .WithMany()
                .HasForeignKey(x => x.GnOfficerNic)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<GnDivisionAssignment>()
                .HasOne(x => x.GnDivision)
                .WithMany()
                .HasForeignKey(x => x.GnDivisionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unified Request Engine Relationships
            modelBuilder.Entity<UnifiedService>()
                .HasOne(x => x.ParentHeading)
                .WithMany(x => x.Services)
                .HasForeignKey(x => x.ParentHeadingId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<ServiceFormSchema>()
                .HasOne(x => x.Category)
                .WithMany(x => x.FormSchemas)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<FamilyMemberHistory>()
                .HasOne(x => x.Citizen)
                .WithMany()
                .HasForeignKey(x => x.CitizenNic)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<FamilyMemberHistory>()
                .HasOne(x => x.FamilyCard)
                .WithMany()
                .HasForeignKey(x => x.FamilyCardNumber)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ServiceCategory>()
                .HasOne(x => x.Service)
                .WithMany(x => x.Categories)
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UnifiedRequest>()
                .HasOne(x => x.Citizen)
                .WithMany()
                .HasForeignKey(x => x.CitizenNic)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UnifiedRequest>()
                .HasOne(x => x.Service)
                .WithMany(x => x.Requests)
                .HasForeignKey(x => x.ServiceId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UnifiedRequest>()
                .HasOne(x => x.Category)
                .WithMany(x => x.Requests)
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UnifiedRequest>()
                .HasOne(x => x.AssignedOfficer)
                .WithMany()
                .HasForeignKey(x => x.AssignedGnOfficerNic)
                .OnDelete(DeleteBehavior.NoAction);

            modelBuilder.Entity<UnifiedRequest>()
                .HasOne(x => x.DynamicData)
                .WithOne(x => x.Request)
                .HasForeignKey<RequestData>(x => x.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UnifiedRequestAttachment>()
                .HasOne(x => x.Request)
                .WithMany(x => x.Attachments)
                .HasForeignKey(x => x.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UnifiedRequest>()
                .HasOne(x => x.StatusHistory)
                .WithOne(x => x.Request)
                .HasForeignKey<RequestStatusHistory>(x => x.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RequestStatusHistory>()
                .HasOne(x => x.ChangedByAccount)
                .WithMany()
                .HasForeignKey(x => x.ChangedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<UnifiedRequestRemark>()
                .HasOne(x => x.Request)
                .WithMany(x => x.Remarks)
                .HasForeignKey(x => x.RequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<UnifiedRequestRemark>()
                .HasOne(x => x.Officer)
                .WithMany()
                .HasForeignKey(x => x.OfficerNic)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CitizenWelfareStatus>()
                .HasOne(x => x.Citizen)
                .WithMany()
                .HasForeignKey(x => x.CitizenNic)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CitizenWelfareStatus>()
                .HasOne(x => x.VerifyingOfficer)
                .WithMany()
                .HasForeignKey(x => x.VerifiedByGnOfficer)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
