using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "GNOfficer,GnOfficer,Citizen")]
    public class DocumentVaultController : ControllerBase
    {
        private readonly IDocumentVaultService _vaultService;
        private readonly EGN_Service_Management.Database.EgnDbContext _context;

        public DocumentVaultController(IDocumentVaultService vaultService, EGN_Service_Management.Database.EgnDbContext context)
        {
            _vaultService = vaultService;
            _context = context;
        }

        private string GetNicFromToken()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        }

        private string GetRoleFromToken()
        {
            return User.FindFirst(ClaimTypes.Role)?.Value ?? "";
        }

        private async Task<string> GetActualRoleIdAsync(string roleName)
        {
            // Fallback default roles
            string defaultRoleId = (roleName == "GnOfficer" || roleName == "GNOfficer") ? "2" : "3";
            string roleQueryParam = (roleName == "GnOfficer" || roleName == "GNOfficer") ? "GN" : "CITIZEN";

            using (var cmd = _context.Database.GetDbConnection().CreateCommand())
            {
                cmd.CommandText = "SELECT TOP 1 [RoleId] FROM [Roles] WHERE [RoleName] = @RoleName OR [RoleName] = @RoleNameFallback";
                
                var p1 = cmd.CreateParameter();
                p1.ParameterName = "@RoleName";
                p1.Value = roleQueryParam;
                cmd.Parameters.Add(p1);

                var p2 = cmd.CreateParameter();
                p2.ParameterName = "@RoleNameFallback";
                p2.Value = roleName;
                cmd.Parameters.Add(p2);

                var wasOpen = _context.Database.GetDbConnection().State == System.Data.ConnectionState.Open;
                if (!wasOpen) await _context.Database.OpenConnectionAsync();
                try
                {
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null)
                    {
                        return result.ToString() ?? defaultRoleId;
                    }
                }
                finally
                {
                    if (!wasOpen) await _context.Database.CloseConnectionAsync();
                }
            }
            return defaultRoleId;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<DocumentVaultResponseDTO>>> GetMyDocuments()
        {
            var nic = GetNicFromToken();
            var role = GetRoleFromToken();

            if (string.IsNullOrEmpty(nic) || string.IsNullOrEmpty(role))
                return Unauthorized("Invalid token claims.");

            var roleId = await GetActualRoleIdAsync(role);

            var documents = await _vaultService.GetDocumentsAsync(nic, roleId);
            return Ok(documents);
        }

        [HttpPost("upload")]
        public async Task<ActionResult<DocumentVaultResponseDTO>> UploadDocument([FromForm] DocumentVaultRequestDTO request)
        {
            var nic = GetNicFromToken();
            var role = GetRoleFromToken();

            if (string.IsNullOrEmpty(nic) || string.IsNullOrEmpty(role))
                return Unauthorized("Invalid token claims.");

            var roleId = await GetActualRoleIdAsync(role);
            int uploadedById = 0; 

            try
            {
                var uploadedDoc = await _vaultService.UploadDocumentAsync(request, nic, roleId, uploadedById);
                return Ok(uploadedDoc);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(int id)
        {
            var nic = GetNicFromToken();
            var role = GetRoleFromToken();

            if (string.IsNullOrEmpty(nic) || string.IsNullOrEmpty(role))
                return Unauthorized("Invalid token claims.");

            var roleId = await GetActualRoleIdAsync(role);
            int deletedById = 0;

            var success = await _vaultService.DeleteDocumentAsync(id, nic, roleId, deletedById);

            if (!success)
                return NotFound("Document not found or you don't have permission to delete it.");

            return NoContent();
        }
    }
}
