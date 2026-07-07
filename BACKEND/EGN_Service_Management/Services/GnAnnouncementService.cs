using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Services
{
    public class GnAnnouncementService : IGnAnnouncementService
    {
        private readonly IGnAnnouncementRepository _repository;

        public GnAnnouncementService(IGnAnnouncementRepository repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<GnAnnouncementResponseDTO>> GetGnAnnouncementsAsync(string divisionId)
        {
            var announcements = await _repository.GetAnnouncementsByDivisionAsync(divisionId, publishedOnly: false);
            var result = new List<GnAnnouncementResponseDTO>();
            foreach(var a in announcements) { result.Add(await MapToResponseDTOAsync(a)); }
            return result;
        }

        public async Task<IEnumerable<GnAnnouncementResponseDTO>> GetCitizenAnnouncementsAsync(string divisionId, string? citizenNic = null)
        {
            var announcements = await _repository.GetAnnouncementsByDivisionAsync(divisionId, publishedOnly: true);
            var result = new List<GnAnnouncementResponseDTO>();
            foreach(var a in announcements) { result.Add(await MapToResponseDTOAsync(a, citizenNic)); }
            return result;
        }

        public async Task<GnAnnouncementResponseDTO> GetByIdAsync(int id, bool isCitizen, string? citizenNic = null)
        {
            var announcement = await _repository.GetByIdAsync(id);
            if (announcement == null)
            {
                throw new KeyNotFoundException("Announcement not found.");
            }

            if (isCitizen)
            {
                var now = DateTime.UtcNow;
                if (!announcement.PublishDate.HasValue || announcement.ExpiryDate < now)
                {
                    throw new UnauthorizedAccessException("This announcement is not available.");
                }
            }

            return await MapToResponseDTOAsync(announcement, citizenNic);
        }

        public async Task<GnAnnouncementResponseDTO> CreateAsync(GnAnnouncementRequestDTO request, string gnOfficerNic, string divisionId)
        {
            var announcement = new GnAnnouncement
            {
                GnOfficerNic = gnOfficerNic,
                GnDivisionId = divisionId,
                AnnouncementType = request.AnnouncementType,
                Title = request.Title,
                Description = request.Description,
                DocumentId = request.DocumentId,
                ExternalSurveyUrl = request.ExternalSurveyUrl,
                PollQuestion = request.PollQuestion,
                PublishDate = request.PublishDate,
                ExpiryDate = request.ExpiryDate,
                CreatedBy = gnOfficerNic,
                ModifiedBy = gnOfficerNic
            };

            await _repository.CreateAsync(announcement);

            if (request.AnnouncementType == "Poll" && request.PollOptions != null)
            {
                foreach (var optionText in request.PollOptions)
                {
                    await _repository.CreatePollOptionAsync(new GnPollOption
                    {
                        AnnouncementId = announcement.AnnouncementId,
                        OptionText = optionText
                    });
                }
            }

            // Retrieve again to get options loaded
            var created = await _repository.GetByIdAsync(announcement.AnnouncementId);
            return await MapToResponseDTOAsync(created!);
        }

        public async Task<GnAnnouncementResponseDTO> UpdateAsync(int id, GnAnnouncementRequestDTO request, string gnOfficerNic)
        {
            var announcement = await _repository.GetByIdAsync(id);
            if (announcement == null || announcement.GnOfficerNic != gnOfficerNic)
            {
                throw new KeyNotFoundException("Announcement not found or unauthorized.");
            }

            announcement.AnnouncementType = request.AnnouncementType;
            announcement.Title = request.Title;
            announcement.Description = request.Description;
            announcement.DocumentId = request.DocumentId;
            announcement.ExternalSurveyUrl = request.ExternalSurveyUrl;
            announcement.PollQuestion = request.PollQuestion;
            announcement.PublishDate = request.PublishDate;
            announcement.ExpiryDate = request.ExpiryDate;
            announcement.ModifiedBy = gnOfficerNic;

            await _repository.UpdateAsync(announcement);

            if (request.AnnouncementType == "Poll" && request.PollOptions != null)
            {
                await _repository.DeletePollOptionsAsync(announcement.AnnouncementId);
                foreach (var optionText in request.PollOptions)
                {
                    await _repository.CreatePollOptionAsync(new GnPollOption
                    {
                        AnnouncementId = announcement.AnnouncementId,
                        OptionText = optionText
                    });
                }
            }

            var updated = await _repository.GetByIdAsync(announcement.AnnouncementId);
            return await MapToResponseDTOAsync(updated!);
        }

        public async Task DeleteAsync(int id, string gnOfficerNic)
        {
            var announcement = await _repository.GetByIdAsync(id);
            if (announcement == null || announcement.GnOfficerNic != gnOfficerNic)
            {
                throw new KeyNotFoundException("Announcement not found or unauthorized.");
            }

            await _repository.DeletePollOptionsAsync(id);
            await _repository.DeleteAsync(announcement);
        }

        public async Task SubmitVoteAsync(int announcementId, int optionId, string citizenNic)
        {
            var announcement = await _repository.GetByIdAsync(announcementId);
            if (announcement == null || announcement.AnnouncementType != "Poll")
            {
                throw new KeyNotFoundException("Poll not found.");
            }

            var now = DateTime.UtcNow;
            if (!announcement.PublishDate.HasValue || announcement.ExpiryDate < now)
            {
                throw new InvalidOperationException("Poll voting is closed or not published.");
            }

            var hasVoted = await _repository.HasCitizenVotedAsync(announcementId, citizenNic);
            if (hasVoted)
            {
                throw new InvalidOperationException("Citizen has already voted.");
            }

            var vote = new GnPollVote
            {
                AnnouncementId = announcementId,
                OptionId = optionId,
                CitizenNic = citizenNic
            };

            await _repository.SubmitVoteAsync(vote);
        }

        public async Task<IEnumerable<GNDocument>> GetPublishedDocumentsAsync(string gnOfficerNic)
        {
            return await _repository.GetPublishedDocumentsByGnOfficerAsync(gnOfficerNic);
        }

        public async Task<IEnumerable<GNDocument>> GetPublishedDocumentsByDivisionAsync(string divisionId)
        {
            return await _repository.GetPublishedDocumentsByDivisionAsync(divisionId);
        }

        private async Task<GnAnnouncementResponseDTO> MapToResponseDTOAsync(GnAnnouncement a, string? citizenNic = null)
        {
            string status = "Draft";
            var now = DateTime.UtcNow;
            if (a.PublishDate.HasValue)
            {
                if (a.ExpiryDate >= now)
                    status = "Published";
                else
                    status = "Expired";
            }

            string createdByName = !string.IsNullOrEmpty(a.CreatedBy) ? await _repository.GetFullNameByNicAsync(a.CreatedBy) : 
                                   (a.GnOfficer != null && !string.IsNullOrEmpty(a.GnOfficer.FullName) ? a.GnOfficer.FullName : await _repository.GetFullNameByNicAsync(a.GnOfficerNic));
            
            string modifiedByName = !string.IsNullOrEmpty(a.ModifiedBy) ? await _repository.GetFullNameByNicAsync(a.ModifiedBy) : 
                                    (a.GnOfficer != null && !string.IsNullOrEmpty(a.GnOfficer.FullName) ? a.GnOfficer.FullName : await _repository.GetFullNameByNicAsync(a.GnOfficerNic));

            bool hasVoted = false;
            int? userVotedOptionId = null;

            if (!string.IsNullOrEmpty(citizenNic) && a.PollOptions != null)
            {
                var userVote = a.PollOptions.SelectMany(po => po.PollVotes ?? new List<GnPollVote>())
                                            .FirstOrDefault(v => v.CitizenNic == citizenNic);
                if (userVote != null)
                {
                    hasVoted = true;
                    userVotedOptionId = userVote.OptionId;
                }
            }

            return new GnAnnouncementResponseDTO
            {
                AnnouncementId = a.AnnouncementId,
                GnOfficerNic = a.GnOfficerNic,
                GnDivisionId = a.GnDivisionId,
                AnnouncementType = a.AnnouncementType,
                Title = a.Title,
                Description = a.Description,
                DocumentId = a.DocumentId,
                DocumentName = a.GNDocument?.Title,
                DocumentUrl = a.GNDocument?.FilePath,
                ExternalSurveyUrl = a.ExternalSurveyUrl,
                PollQuestion = a.PollQuestion,
                PublishDate = a.PublishDate,
                ExpiryDate = a.ExpiryDate,
                CreatedAt = a.CreatedAt,
                CreatedByName = createdByName,
                ModifiedByName = modifiedByName,
                Status = status,
                HasVoted = hasVoted,
                UserVotedOptionId = userVotedOptionId,
                PollOptions = a.PollOptions.Select(po => new GnPollOptionResponseDTO
                {
                    OptionId = po.OptionId,
                    OptionText = po.OptionText,
                    Votes = po.PollVotes?.Count ?? 0
                }).ToList()
            };
        }
    }
}
