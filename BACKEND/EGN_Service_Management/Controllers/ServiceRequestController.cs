using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceRequestController : ControllerBase
    {
        private readonly IServiceRequestService _serviceRequestService;

        public ServiceRequestController(IServiceRequestService serviceRequestService)
        {
            _serviceRequestService = serviceRequestService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllServiceRequestRecords()
        {
            var records = await _serviceRequestService.GetAllAsync();
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetServiceRequestById(string id)
        {
            try
            {
                var record = await _serviceRequestService.GetByIdAsync(id);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "ServiceRequest not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateServiceRequest([FromBody] ServiceRequestRequestDTO requestDTO)
        {
            try
            {
                var record = await _serviceRequestService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetServiceRequestById), new { id = record.Id }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateServiceRequest(string id, [FromBody] ServiceRequestRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.Id = id;
                var record = await _serviceRequestService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "ServiceRequest not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteServiceRequest(string id)
        {
            try
            {
                await _serviceRequestService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "ServiceRequest not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static ServiceRequest MapToEntity(ServiceRequestRequestDTO requestDTO)
        {
            return new ServiceRequest
            {
                Id = requestDTO.Id,
                CitizenNic = requestDTO.CitizenNic,
                GnDivisionId = requestDTO.GnDivisionId,
                ServiceType = requestDTO.ServiceType,
                Category = requestDTO.Category,
                Description = requestDTO.Description,
                Status = requestDTO.Status,
                CurrentStep = requestDTO.CurrentStep,
                RejectionReason = requestDTO.RejectionReason,
                FinalDocumentUrl = requestDTO.FinalDocumentUrl,
                SubmittedDate = requestDTO.SubmittedDate,
                LastUpdated = requestDTO.LastUpdated,
            };
        }

        private static ServiceRequestResponseDTO MapToResponseDTO(ServiceRequest entity)
        {
            return new ServiceRequestResponseDTO
            {
                Id = entity.Id,
                CitizenNic = entity.CitizenNic,
                GnDivisionId = entity.GnDivisionId,
                ServiceType = entity.ServiceType,
                Category = entity.Category,
                Description = entity.Description,
                Status = entity.Status,
                CurrentStep = entity.CurrentStep,
                RejectionReason = entity.RejectionReason,
                FinalDocumentUrl = entity.FinalDocumentUrl,
                SubmittedDate = entity.SubmittedDate,
                LastUpdated = entity.LastUpdated,
            };
        }
    }
}
