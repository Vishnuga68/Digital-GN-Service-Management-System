using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SurveyResponseController : ControllerBase
    {
        private readonly ISurveyResponseService _surveyResponseService;

        public SurveyResponseController(ISurveyResponseService surveyResponseService)
        {
            _surveyResponseService = surveyResponseService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSurveyResponseRecords()
        {
            var records = await _surveyResponseService.GetAllAsync();
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSurveyResponseById(int id)
        {
            try
            {
                var record = await _surveyResponseService.GetByIdAsync(id);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "SurveyResponse not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateSurveyResponse([FromBody] SurveyResponseRequestDTO requestDTO)
        {
            try
            {
                var record = await _surveyResponseService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetSurveyResponseById), new { id = record.Id }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSurveyResponse(int id, [FromBody] SurveyResponseRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.Id = id;
                var record = await _surveyResponseService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "SurveyResponse not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSurveyResponse(int id)
        {
            try
            {
                await _surveyResponseService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "SurveyResponse not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static SurveyResponse MapToEntity(SurveyResponseRequestDTO requestDTO)
        {
            return new SurveyResponse
            {
                Id = requestDTO.Id,
                AnnouncementId = requestDTO.AnnouncementId,
                CitizenNic = requestDTO.CitizenNic,
                ResponseText = requestDTO.ResponseText,
                SubmittedAt = requestDTO.SubmittedAt,
            };
        }

        private static SurveyResponseResponseDTO MapToResponseDTO(SurveyResponse entity)
        {
            return new SurveyResponseResponseDTO
            {
                Id = entity.Id,
                AnnouncementId = entity.AnnouncementId,
                CitizenNic = entity.CitizenNic,
                ResponseText = entity.ResponseText,
                SubmittedAt = entity.SubmittedAt,
            };
        }
    }
}
