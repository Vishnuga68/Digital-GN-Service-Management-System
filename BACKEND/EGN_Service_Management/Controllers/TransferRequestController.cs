using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransferRequestController : ControllerBase
    {
        private readonly ITransferRequestService _transferRequestService;

        public TransferRequestController(ITransferRequestService transferRequestService)
        {
            _transferRequestService = transferRequestService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTransferRequestRecords()
        {
            var records = await _transferRequestService.GetAllAsync();
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTransferRequestById(string id)
        {
            try
            {
                var record = await _transferRequestService.GetByIdAsync(id);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "TransferRequest not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransferRequest([FromBody] TransferRequestRequestDTO requestDTO)
        {
            try
            {
                var record = await _transferRequestService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetTransferRequestById), new { id = record.Id }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransferRequest(string id, [FromBody] TransferRequestRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.Id = id;
                var record = await _transferRequestService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "TransferRequest not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransferRequest(string id)
        {
            try
            {
                await _transferRequestService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "TransferRequest not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static TransferRequest MapToEntity(TransferRequestRequestDTO requestDTO)
        {
            return new TransferRequest
            {
                Id = requestDTO.Id,
                GnOfficerNic = requestDTO.GnOfficerNic,
                CurrentDivisionId = requestDTO.CurrentDivisionId,
                RequestedDivisionId = requestDTO.RequestedDivisionId,
                Reason = requestDTO.Reason,
                Status = requestDTO.Status,
                DocumentName = requestDTO.DocumentName,
                DocumentUrl = requestDTO.DocumentUrl,
                SubmittedDate = requestDTO.SubmittedDate,
                ReviewedByNic = requestDTO.ReviewedByNic,
                ReviewedDate = requestDTO.ReviewedDate,
            };
        }

        private static TransferRequestResponseDTO MapToResponseDTO(TransferRequest entity)
        {
            return new TransferRequestResponseDTO
            {
                Id = entity.Id,
                GnOfficerNic = entity.GnOfficerNic,
                CurrentDivisionId = entity.CurrentDivisionId,
                RequestedDivisionId = entity.RequestedDivisionId,
                Reason = entity.Reason,
                Status = entity.Status,
                DocumentName = entity.DocumentName,
                DocumentUrl = entity.DocumentUrl,
                SubmittedDate = entity.SubmittedDate,
                ReviewedByNic = entity.ReviewedByNic,
                ReviewedDate = entity.ReviewedDate,
            };
        }
    }
}
