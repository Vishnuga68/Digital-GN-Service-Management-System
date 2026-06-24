using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using EGN_Service_Management.Database;
using EGN_Service_Management.DTO;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FamilyCardController : ControllerBase
    {
        private readonly IFamilyCardService _familyCardService;
        private readonly EgnDbContext _context;

        public FamilyCardController(IFamilyCardService familyCardService, EgnDbContext context)
        {
            _familyCardService = familyCardService;
            _context = context;
        }

        [HttpGet("{familyCardNumber}/members")]
        public async Task<IActionResult> GetFamilyMembers(string familyCardNumber)
        {
            try
            {
                var familyCard = await _context.FamilyCards.FirstOrDefaultAsync(fc => fc.FamilyCardNumber == familyCardNumber);
                if (familyCard == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Family Card not found."));
                }

                var members = await _context.CitizenMasters
                    .Where(c => c.FamilyCardNumber == familyCardNumber)
                    .Select(c => new
                    {
                        Nic = c.Nic,
                        FullName = c.FullName,
                        Dob = c.Dob,
                        Gender = c.Gender,
                        MaritalStatus = c.MaritalStatus,
                        Religion = c.Religion,
                        Occupation = c.Occupation,
                        RelationshipToHead = c.Nic == familyCard.HeadOfHouseholdNic ? "Head" : "Member",
                        LifeStatus = c.LifeStatus,
                        GnDivisionId = c.GnDivisionId
                    })
                    .ToListAsync();

                return Ok(ApiResponse<List<object>>.SuccessResponse(members.Cast<object>().ToList(), "Family members retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Error fetching family members: {ex.Message}"));
            }
        }

        [HttpGet("history/members")]
        public async Task<IActionResult> GetFamilyHistory([FromQuery] string? pastCard)
        {
            try
            {
                if (string.IsNullOrEmpty(pastCard))
                {
                    return Ok(ApiResponse<List<object>>.SuccessResponse(new List<object>(), "Past family card not found."));
                }

                var query = _context.CitizenMasters.Where(c => c.FamilyCardNumber == pastCard || c.PastFamilyCardNumber == pastCard);

                var members = await query
                    .Select(c => new
                    {
                        Nic = c.Nic,
                        FullName = c.FullName,
                        Dob = c.Dob,
                        Gender = c.Gender,
                        MaritalStatus = c.MaritalStatus,
                        Religion = c.Religion,
                        Occupation = c.Occupation,
                        RelationshipToHead = "Member", // Head is difficult to determine for history, fallback to Member
                        LifeStatus = c.LifeStatus,
                        GnDivisionId = c.GnDivisionId,
                        FamilyCardNumber = c.FamilyCardNumber,
                        PastFamilyCardNumber = c.PastFamilyCardNumber
                    })
                    .ToListAsync();

                if (!members.Any())
                {
                    return Ok(ApiResponse<List<object>>.SuccessResponse(new List<object>(), "Past family card not found."));
                }

                return Ok(ApiResponse<List<object>>.SuccessResponse(members.Cast<object>().ToList(), "Family history retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Error fetching family history: {ex.Message}"));
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllFamilyCardRecords()
        {
            var records = await _familyCardService.GetAllAsync();
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetFamilyCardById(string id)
        {
            try
            {
                var record = await _familyCardService.GetByIdAsync(id);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "FamilyCard not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateFamilyCard([FromBody] FamilyCardRequestDTO requestDTO)
        {
            try
            {
                var record = await _familyCardService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetFamilyCardById), new { id = record.FamilyCardNumber }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFamilyCard(string id, [FromBody] FamilyCardRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.FamilyCardNumber = id;
                var record = await _familyCardService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "FamilyCard not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFamilyCard(string id)
        {
            try
            {
                await _familyCardService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "FamilyCard not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        private static FamilyCard MapToEntity(FamilyCardRequestDTO requestDTO)
        {
            return new FamilyCard
            {
                FamilyCardNumber = requestDTO.FamilyCardNumber,
                HeadOfHouseholdNic = requestDTO.HeadOfHouseholdNic,
                GnDivisionId = requestDTO.GnDivisionId,
                Address = requestDTO.Address,
                RegisteredDate = requestDTO.RegisteredDate,
                Status = requestDTO.Status,
            };
        }

        private static FamilyCardResponseDTO MapToResponseDTO(FamilyCard entity)
        {
            return new FamilyCardResponseDTO
            {
                FamilyCardNumber = entity.FamilyCardNumber,
                HeadOfHouseholdNic = entity.HeadOfHouseholdNic,
                GnDivisionId = entity.GnDivisionId,
                Address = entity.Address,
                RegisteredDate = entity.RegisteredDate,
                Status = entity.Status,
            };
        }
    }
}
