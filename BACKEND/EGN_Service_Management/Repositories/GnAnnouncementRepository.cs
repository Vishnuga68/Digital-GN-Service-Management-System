using EGN_Service_Management.Database;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using Microsoft.EntityFrameworkCore;

namespace EGN_Service_Management.Repositories
{
    public class GnAnnouncementRepository : IGnAnnouncementRepository
    {
        private readonly EgnDbContext _context;

        public GnAnnouncementRepository(EgnDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<GnAnnouncement>> GetAnnouncementsByDivisionAsync(string divisionId, bool publishedOnly = false)
        {
            var query = _context.GnAnnouncements
                .Include(a => a.GNDocument)
                .Include(a => a.GnOfficer)
                .Include(a => a.PollOptions)
                    .ThenInclude(po => po.PollVotes)
                .Where(a => a.GnDivisionId == divisionId);

            if (publishedOnly)
            {
                query = query.Where(a => a.PublishDate != null);
            }

            return await query.OrderByDescending(a => a.CreatedAt).ToListAsync();
        }

        public async Task<GnAnnouncement?> GetByIdAsync(int id)
        {
            return await _context.GnAnnouncements
                .Include(a => a.GNDocument)
                .Include(a => a.PollOptions)
                .ThenInclude(po => po.PollVotes)
                .Include(a => a.PollVotes)
                .FirstOrDefaultAsync(a => a.AnnouncementId == id);
        }

        public async Task<GnAnnouncement> CreateAsync(GnAnnouncement announcement)
        {
            await _context.GnAnnouncements.AddAsync(announcement);
            await _context.SaveChangesAsync();
            return announcement;
        }

        public async Task<GnAnnouncement> UpdateAsync(GnAnnouncement announcement)
        {
            _context.GnAnnouncements.Update(announcement);
            await _context.SaveChangesAsync();
            return announcement;
        }

        public async Task DeleteAsync(GnAnnouncement announcement)
        {
            _context.GnAnnouncements.Remove(announcement);
            await _context.SaveChangesAsync();
        }

        public async Task<GnPollOption> CreatePollOptionAsync(GnPollOption option)
        {
            await _context.GnPollOptions.AddAsync(option);
            await _context.SaveChangesAsync();
            return option;
        }

        public async Task DeletePollOptionsAsync(int announcementId)
        {
            var options = await _context.GnPollOptions.Where(po => po.AnnouncementId == announcementId).ToListAsync();
            _context.GnPollOptions.RemoveRange(options);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> HasCitizenVotedAsync(int announcementId, string citizenNic)
        {
            return await _context.GnPollVotes.AnyAsync(v => v.AnnouncementId == announcementId && v.CitizenNic == citizenNic);
        }

        public async Task<GnPollVote> SubmitVoteAsync(GnPollVote vote)
        {
            await _context.GnPollVotes.AddAsync(vote);
            await _context.SaveChangesAsync();
            return vote;
        }

        public async Task<IEnumerable<GNDocument>> GetPublishedDocumentsByGnOfficerAsync(string gnOfficerNic)
        {
            return await _context.GNDocuments
                .Where(d => d.GnOfficerNic == gnOfficerNic && d.Status == "Published")
                .ToListAsync();
        }

        public async Task<IEnumerable<GNDocument>> GetPublishedDocumentsByDivisionAsync(string divisionId)
        {
            return await _context.GNDocuments
                .Where(d => d.DivisionId == divisionId && d.Status == "Published")
                .OrderBy(d => d.Title)
                .ToListAsync();
        }

        public async Task<string> GetFullNameByNicAsync(string nic)
        {
            if (string.IsNullOrEmpty(nic)) return nic;
            nic = nic.Trim();

            var officer = await _context.GnOfficers.FirstOrDefaultAsync(o => o.Nic == nic);
            if (officer != null && !string.IsNullOrEmpty(officer.FullName))
            {
                return officer.FullName;
            }

            var citizen = await _context.CitizenMasters.FirstOrDefaultAsync(c => c.Nic == nic);
            if (citizen != null && !string.IsNullOrEmpty(citizen.FullName))
            {
                return citizen.FullName;
            }

            return nic; // Fallback to NIC if not found
        }
    }
}
