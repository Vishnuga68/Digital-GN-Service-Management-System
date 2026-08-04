using EGN_Service_Management.DTO;
using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using EGN_Service_Management.Database;
using Microsoft.EntityFrameworkCore;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CitizenMasterController : ControllerBase
    {
        private readonly ICitizenMasterService _citizenMasterService;
        private readonly EgnDbContext _context;

        public CitizenMasterController(ICitizenMasterService citizenMasterService, EgnDbContext context)
        {
            _citizenMasterService = citizenMasterService;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCitizenMasterRecords()
        {
            var records = await _citizenMasterService.GetAllAsync();
            return Ok(records.Select(MapToResponseDTO));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCitizenMasterById(string id)
        {
            try
            {
                var record = await _citizenMasterService.GetByIdAsync(id);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "CitizenMaster not found." });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateCitizenMaster([FromBody] CitizenMasterRequestDTO requestDTO)
        {
            try
            {
                var record = await _citizenMasterService.CreateAsync(MapToEntity(requestDTO));
                return CreatedAtAction(nameof(GetCitizenMasterById), new { id = record.Nic }, MapToResponseDTO(record));
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCitizenMaster(string id, [FromBody] CitizenMasterRequestDTO requestDTO)
        {
            try
            {
                var entity = MapToEntity(requestDTO);
                entity.Nic = id;
                var record = await _citizenMasterService.UpdateAsync(id, entity);
                return Ok(MapToResponseDTO(record));
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "CitizenMaster not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCitizenMaster(string id)
        {
            try
            {
                await _citizenMasterService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound(new { message = "CitizenMaster not found." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("division")]
        [Authorize(Roles = "GNOfficer,DSOfficer")]
        public async Task<IActionResult> GetCitizensByDivision([FromQuery] string divisionId)
        {
            var dbConnection = _context.Database.GetDbConnection();
            var wasOpen = dbConnection.State == ConnectionState.Open;
            if (!wasOpen) await _context.Database.OpenConnectionAsync();

            try
            {
                var citizens = new List<object>();
                using (var command = dbConnection.CreateCommand())
                {
                    command.CommandText = @"
                        SELECT 
                            cm.[Nic], cm.[FullName], cm.[Dob], cm.[Gender], cm.[MaritalStatus], 
                            cm.[Religion], cm.[Occupation], CASE WHEN cm.[Nic] = fc.[HeadOfHouseholdNic] THEN 'Head' ELSE 'Member' END AS RelationshipToHead, cm.[LifeStatus], cm.[GnDivisionId],
                            cm.[PastFamilyCardNumber], fc.[FamilyCardNumber], fc.[Address], fc.[Status] AS FamilyCardStatus,
                            (SELECT COUNT(*) FROM [EGN_Service_Users] esu WHERE esu.[Nic] = cm.[Nic]) AS HasEgnAccount,
                            a.[Email], a.[Phone]
                        FROM [Citizen_Master] cm
                        INNER JOIN [Voter_Registry] vr ON cm.[Nic] = vr.[CitizenNic]
                        LEFT JOIN [family_cards] fc ON cm.[FamilyCardNumber] = fc.[FamilyCardNumber]
                        LEFT JOIN [accounts] a ON cm.[Nic] = a.[Nic]
                        WHERE LOWER(cm.[GnDivisionId]) = LOWER(@GnDivId)";

                    var param = command.CreateParameter();
                    param.ParameterName = "@GnDivId";
                    param.Value = divisionId;
                    command.Parameters.Add(param);

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            citizens.Add(new
                             {
                                 nic = reader["Nic"].ToString(),
                                 fullName = reader["FullName"].ToString(),
                                 dob = reader["Dob"] != DBNull.Value ? reader["Dob"].ToString() : null,
                                 gender = reader["Gender"].ToString(),
                                 maritalStatus = reader["MaritalStatus"].ToString(),
                                 religion = reader["Religion"] != DBNull.Value ? reader["Religion"].ToString() : null,
                                 occupation = reader["Occupation"] != DBNull.Value ? reader["Occupation"].ToString() : null,
                                 relationshipToHead = reader["RelationshipToHead"] != DBNull.Value ? reader["RelationshipToHead"].ToString() : null,
                                 lifeStatus = reader["LifeStatus"].ToString(),
                                 gnDivisionId = reader["GnDivisionId"] != DBNull.Value ? reader["GnDivisionId"].ToString() : null,
                                 pastFamilyCardNumber = reader["PastFamilyCardNumber"] != DBNull.Value ? reader["PastFamilyCardNumber"].ToString() : null,
                                 familyCardNumber = reader["FamilyCardNumber"] != DBNull.Value ? reader["FamilyCardNumber"].ToString() : null,
                                 familyCardStatus = reader["FamilyCardStatus"] != DBNull.Value ? reader["FamilyCardStatus"].ToString() : null,
                                 address = reader["Address"] != DBNull.Value ? reader["Address"].ToString() : null,
                                 hasEgnAccount = Convert.ToInt32(reader["HasEgnAccount"]) > 0,
                                 email = reader["Email"] != DBNull.Value ? reader["Email"].ToString() : null,
                                 phone = reader["Phone"] != DBNull.Value ? reader["Phone"].ToString() : null
                             });
                        }
                    }
                }

                return Ok(ApiResponse<List<object>>.SuccessResponse(citizens, "Citizens retrieved successfully for division."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Error getting citizens by division: {ex.Message}"));
            }
            finally
            {
                if (!wasOpen) await _context.Database.CloseConnectionAsync();
            }
        }

        [HttpGet("division/stats")]
        [Authorize(Roles = "GNOfficer,DSOfficer")]
        public async Task<IActionResult> GetDivisionStats([FromQuery] string divisionId)
        {
            try
            {
                var totalCitizens = await _context.CitizenMasters
                    .Where(cm => cm.GnDivisionId == divisionId)
                    .Where(cm => _context.VoterRegistries.Any(vr => vr.CitizenNic == cm.Nic))
                    .CountAsync();
                var totalFamilies = await _context.FamilyCards.CountAsync(fc => fc.GnDivisionId == divisionId);

                return Ok(ApiResponse<object>.SuccessResponse(new
                {
                    totalCitizens,
                    totalFamilies
                }, "Division stats retrieved successfully."));
            }
            catch (Exception ex)
            {
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"Error getting stats: {ex.Message}"));
            }
        }

        private static CitizenMaster MapToEntity(CitizenMasterRequestDTO requestDTO)
        {
            return new CitizenMaster
            {
                Nic = requestDTO.Nic,
                FullName = requestDTO.FullName,
                Dob = requestDTO.Dob,
                Gender = requestDTO.Gender,
                MaritalStatus = requestDTO.MaritalStatus,
                Religion = requestDTO.Religion,
                Occupation = requestDTO.Occupation,
                FamilyCardNumber = requestDTO.FamilyCardNumber,
                PastFamilyCardNumber = requestDTO.PastFamilyCardNumber,
                LifeStatus = requestDTO.LifeStatus,
                MonthlyIncome = requestDTO.MonthlyIncome
            };
        }

        private static CitizenMasterResponseDTO MapToResponseDTO(CitizenMaster entity)
        {
            return new CitizenMasterResponseDTO
            {
                Nic = entity.Nic,
                FullName = entity.FullName,
                Dob = entity.Dob,
                Gender = entity.Gender,
                MaritalStatus = entity.MaritalStatus,
                Religion = entity.Religion,
                Occupation = entity.Occupation,
                FamilyCardNumber = entity.FamilyCardNumber,
                PastFamilyCardNumber = entity.PastFamilyCardNumber,
                LifeStatus = entity.LifeStatus,
                Address = entity.FamilyCard?.Address,
                GnDivisionId = entity.GnDivisionId,
                MonthlyIncome = entity.MonthlyIncome
            };
        }
    }
}
