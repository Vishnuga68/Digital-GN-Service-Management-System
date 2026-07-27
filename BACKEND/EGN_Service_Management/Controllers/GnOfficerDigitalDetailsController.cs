using EGN_Service_Management.Database;
using EGN_Service_Management.Entity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Security.Claims;

namespace EGN_Service_Management.Controllers
{
    [Route("api/gn-officer")]
    [ApiController]
    public class GnOfficerDigitalDetailsController : ControllerBase
    {
        private readonly EgnDbContext _context;
        private readonly IWebHostEnvironment _env;

        public GnOfficerDigitalDetailsController(EgnDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        [HttpGet("digital-details/{nic}")]
        [Authorize(Roles = "GNOfficer,DSOfficer")]
        public async Task<IActionResult> GetDigitalDetails(string nic)
        {
            var callerNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var isDs = User.IsInRole("DSOfficer");

            // A GN Officer must not be able to use/view another officer's signature or stamp.
            if (!isDs && callerNic != nic)
            {
                return Forbid("You are not authorized to view this officer's digital details.");
            }

            var details = await _context.GNOfficerDigitalDetails.FirstOrDefaultAsync(d => d.GNOfficerNIC == nic);
            if (details == null)
            {
                return Ok(new
                {
                    nic = nic,
                    rubberStampPath = (string?)null,
                    signaturePath = (string?)null
                });
            }

            return Ok(new
            {
                nic = details.GNOfficerNIC,
                rubberStampPath = GetBase64DataUrl(details.RubberStampPath, "gn-stamps"),
                signaturePath = GetBase64DataUrl(details.SignaturePath, "gn-signatures")
            });
        }

        [HttpPut("update-rubber-stamp")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> UpdateRubberStamp([FromForm] string? GNOfficerNIC, IFormFile RubberStampImage)
        {
            // Always read NIC from JWT claims for security
            var callerNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(callerNic)) return Unauthorized();

            if (RubberStampImage == null || RubberStampImage.Length == 0) return BadRequest("Image is required.");

            // Validation
            if (RubberStampImage.Length > 500 * 1024) return BadRequest("Maximum file size is 500KB.");
            
            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg" };
            var extension = Path.GetExtension(RubberStampImage.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension)) return BadRequest("Only PNG, JPG and JPEG images are allowed.");

            var allowedMimeTypes = new[] { "image/png", "image/jpeg" };
            if (!allowedMimeTypes.Contains(RubberStampImage.ContentType.ToLowerInvariant())) return BadRequest("Invalid image format.");

            // Ensure secure directory exists (outside wwwroot)
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "SecureUploads", "gn-stamps");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            // Save file securely
            var uniqueFileName = $"GN_{callerNic}_stamp_{DateTime.UtcNow.Ticks}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await RubberStampImage.CopyToAsync(fileStream);
            }

            var dbPath = $"/secure-uploads/gn-stamps/{uniqueFileName}";

            // Update DB
            var details = await _context.GNOfficerDigitalDetails.FirstOrDefaultAsync(d => d.GNOfficerNIC == callerNic);
            if (details == null)
            {
                details = new GNOfficerDigitalDetails
                {
                    GNOfficerNIC = callerNic,
                    RubberStampPath = dbPath,
                    CreatedBy = User.Identity?.Name ?? callerNic
                };
                _context.GNOfficerDigitalDetails.Add(details);
            }
            else
            {
                details.RubberStampPath = dbPath;
                details.UpdatedDate = DateTime.UtcNow;
                details.UpdatedBy = User.Identity?.Name ?? callerNic;
                _context.GNOfficerDigitalDetails.Update(details);
            }

            await _context.SaveChangesAsync();

            var base64DataUrl = GetBase64DataUrl(dbPath, "gn-stamps");
            return Ok(new { message = "Rubber stamp updated successfully", path = base64DataUrl });
        }

        [HttpPost("update-rubber-stamp")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> UpdateRubberStampPost([FromForm] string? GNOfficerNIC, IFormFile RubberStampImage)
        {
            return await UpdateRubberStamp(GNOfficerNIC, RubberStampImage);
        }

        [HttpPut("update-signature")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> UpdateSignature([FromForm] string? GNOfficerNIC, IFormFile SignatureImage)
        {
            // Always read NIC from JWT claims for security
            var callerNic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(callerNic)) return Unauthorized();

            if (SignatureImage == null || SignatureImage.Length == 0) return BadRequest("Image is required.");

            // Validation
            if (SignatureImage.Length > 500 * 1024) return BadRequest("Maximum file size is 500KB.");
            
            var allowedExtensions = new[] { ".png", ".jpg", ".jpeg" };
            var extension = Path.GetExtension(SignatureImage.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension)) return BadRequest("Only PNG, JPG and JPEG images are allowed.");

            var allowedMimeTypes = new[] { "image/png", "image/jpeg" };
            if (!allowedMimeTypes.Contains(SignatureImage.ContentType.ToLowerInvariant())) return BadRequest("Invalid image format.");

            // Ensure secure directory exists (outside wwwroot)
            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "SecureUploads", "gn-signatures");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            // Save file securely
            var uniqueFileName = $"GN_{callerNic}_signature_{DateTime.UtcNow.Ticks}{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await SignatureImage.CopyToAsync(fileStream);
            }

            var dbPath = $"/secure-uploads/gn-signatures/{uniqueFileName}";

            // Update DB
            var details = await _context.GNOfficerDigitalDetails.FirstOrDefaultAsync(d => d.GNOfficerNIC == callerNic);
            if (details == null)
            {
                details = new GNOfficerDigitalDetails
                {
                    GNOfficerNIC = callerNic,
                    SignaturePath = dbPath,
                    CreatedBy = User.Identity?.Name ?? callerNic
                };
                _context.GNOfficerDigitalDetails.Add(details);
            }
            else
            {
                details.SignaturePath = dbPath;
                details.UpdatedDate = DateTime.UtcNow;
                details.UpdatedBy = User.Identity?.Name ?? callerNic;
                _context.GNOfficerDigitalDetails.Update(details);
            }

            await _context.SaveChangesAsync();

            var base64DataUrl = GetBase64DataUrl(dbPath, "gn-signatures");
            return Ok(new { message = "Digital signature updated successfully", path = base64DataUrl });
        }

        [HttpPost("update-signature")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> UpdateSignaturePost([FromForm] string? GNOfficerNIC, IFormFile SignatureImage)
        {
            return await UpdateSignature(GNOfficerNIC, SignatureImage);
        }

        private string? GetBase64DataUrl(string? relativePath, string category)
        {
            if (string.IsNullOrEmpty(relativePath)) return null;

            // Resolve file path securely
            var fileName = Path.GetFileName(relativePath);
            var securePath = Path.Combine(Directory.GetCurrentDirectory(), "SecureUploads", category, fileName);
            
            if (!System.IO.File.Exists(securePath))
            {
                // Fallback to wwwroot if it was saved there previously
                securePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", relativePath.TrimStart('/'));
            }

            if (System.IO.File.Exists(securePath))
            {
                var bytes = System.IO.File.ReadAllBytes(securePath);
                var extension = Path.GetExtension(securePath).ToLowerInvariant();
                var mimeType = extension == ".png" ? "image/png" : "image/jpeg";
                return $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
            }

            return null;
        }
    }
}
