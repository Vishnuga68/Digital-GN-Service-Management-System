using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AnnouncementTargetCitizenController : ControllerBase
    {
        private readonly IAnnouncementTargetCitizenService _announcementTargetCitizenService;

        public AnnouncementTargetCitizenController(IAnnouncementTargetCitizenService announcementTargetCitizenService)
        {
            _announcementTargetCitizenService = announcementTargetCitizenService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAnnouncementTargetCitizenRecords()
        {
            var records = await _announcementTargetCitizenService.GetAllAsync();
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetAnnouncementTargetCitizenById(int id)
        {
            try
            {
                var record = await _announcementTargetCitizenService.GetByIdAsync(id);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "AnnouncementTargetCitizen not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateAnnouncementTargetCitizen([FromBody] AnnouncementTargetCitizenRequestDTO requestDTO)
        {
            try
            {
                var record = await _announcementTargetCitizenService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetAnnouncementTargetCitizenById), new { id = record.Id }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAnnouncementTargetCitizen(int id, [FromBody] AnnouncementTargetCitizenRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.Id = id;
                var record = await _announcementTargetCitizenService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "AnnouncementTargetCitizen not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAnnouncementTargetCitizen(int id)
        {
            try
            {
                await _announcementTargetCitizenService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "AnnouncementTargetCitizen not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static AnnouncementTargetCitizen MapToEntity(AnnouncementTargetCitizenRequestDTO requestDTO)
        {
            return new AnnouncementTargetCitizen
            {
                Id = requestDTO.Id,
                AnnouncementId = requestDTO.AnnouncementId,
                CitizenNic = requestDTO.CitizenNic,
            };
        }

        private static AnnouncementTargetCitizenResponseDTO MapToResponseDTO(AnnouncementTargetCitizen entity)
        {
            return new AnnouncementTargetCitizenResponseDTO
            {
                Id = entity.Id,
                AnnouncementId = entity.AnnouncementId,
                CitizenNic = entity.CitizenNic,
            };
        }
    }
}
