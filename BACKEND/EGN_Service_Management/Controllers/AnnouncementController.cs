using AutoMapper;
using EGN_Service_Management.DTO;
using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Secure all actions by default
    public class AnnouncementController : ControllerBase
    {
        private readonly IAnnouncementService _announcementService;
        private readonly IMapper _mapper;

        public AnnouncementController(IAnnouncementService announcementService, IMapper mapper)
        {
            _announcementService = announcementService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAnnouncementRecords()
        {
            var records = await _announcementService.GetAllAsync();
            var responseDTOs = _mapper.Map<IEnumerable<AnnouncementResponseDTO>>(records);
            return Ok(ApiResponse<IEnumerable<AnnouncementResponseDTO>>.SuccessResponse(responseDTOs, "Announcements retrieved successfully."));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAnnouncementById(string id)
        {
            var record = await _announcementService.GetByIdAsync(id);
            var responseDTO = _mapper.Map<AnnouncementResponseDTO>(record);
            return Ok(ApiResponse<AnnouncementResponseDTO>.SuccessResponse(responseDTO, "Announcement retrieved successfully."));
        }

        [HttpPost]
        [Authorize(Roles = "GNOfficer,DSOfficer")]
        public async Task<IActionResult> CreateAnnouncement([FromBody] AnnouncementRequestDTO requestDTO)
        {
            var entity = _mapper.Map<Announcement>(requestDTO);
            var record = await _announcementService.CreateAsync(entity);
            var responseDTO = _mapper.Map<AnnouncementResponseDTO>(record);
            
            return CreatedAtAction(
                nameof(GetAnnouncementById), 
                new { id = record.Id }, 
                ApiResponse<AnnouncementResponseDTO>.SuccessResponse(responseDTO, "Announcement created successfully.")
            );
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "GNOfficer,DSOfficer")]
        public async Task<IActionResult> UpdateAnnouncement(string id, [FromBody] AnnouncementRequestDTO requestDTO)
        {
            var entity = _mapper.Map<Announcement>(requestDTO);
            entity.Id = id;
            
            var record = await _announcementService.UpdateAsync(id, entity);
            var responseDTO = _mapper.Map<AnnouncementResponseDTO>(record);
            return Ok(ApiResponse<AnnouncementResponseDTO>.SuccessResponse(responseDTO, "Announcement updated successfully."));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "GNOfficer,DSOfficer")]
        public async Task<IActionResult> DeleteAnnouncement(string id)
        {
            await _announcementService.DeleteAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse("Announcement deleted successfully."));
        }
    }
}
