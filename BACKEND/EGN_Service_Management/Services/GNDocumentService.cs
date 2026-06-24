using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EGN_Service_Management.Services
{
    public class GNDocumentService : IGNDocumentService
    {
        private readonly IGNDocumentRepository _repository;
        private readonly IWebHostEnvironment _env;

        public GNDocumentService(IGNDocumentRepository repository, IWebHostEnvironment env)
        {
            _repository = repository;
            _env = env;
        }

        public async Task<IEnumerable<GNDocumentResponseDTO>> GetDocumentsAsync(string divisionId, string? status, int? categoryId)
        {
            var documents = await _repository.GetByDivisionIdAsync(divisionId, status, categoryId);
            return documents.Select(MapToDTO);
        }

        public async Task<GNDocumentResponseDTO?> GetDocumentByIdAsync(int id)
        {
            var document = await _repository.GetByIdAsync(id);
            if (document == null) return null;
            return MapToDTO(document);
        }

        public async Task<GNDocumentResponseDTO> CreateDocumentAsync(GNDocumentRequestDTO request, IFormFile file, string officerNic, string divisionId)
        {
            string fileName = "";
            string filePath = "";
            string fileType = "";

            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "documents");
                Directory.CreateDirectory(uploadsFolder);
                
                fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                var absolutePath = Path.Combine(uploadsFolder, fileName);
                
                using (var stream = new FileStream(absolutePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                filePath = $"/uploads/documents/{fileName}";
                fileType = Path.GetExtension(file.FileName).TrimStart('.');
            }

            var document = new GNDocument
            {
                GnOfficerNic = officerNic,
                DivisionId = divisionId,
                Title = request.Title,
                Description = request.Description,
                CategoryId = request.CategoryId,
                FileName = file?.FileName ?? "",
                FilePath = filePath,
                FileType = fileType,
                Status = request.PublishNow ? "Published" : "Draft",
                PublishDate = request.PublishNow ? DateTime.UtcNow : null,
                ExpiryDate = request.ExpiryDate,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(document);

            await _repository.AddLogAsync(new GNDocumentLog
            {
                DocumentId = document.DocumentId,
                Action = "Created" + (request.PublishNow ? " & Published" : ""),
                PerformedByNic = officerNic,
                ActionDate = DateTime.UtcNow
            });

            var createdDoc = await _repository.GetByIdAsync(document.DocumentId);
            return MapToDTO(createdDoc!);
        }

        public async Task<GNDocumentResponseDTO> UpdateDocumentAsync(int id, GNDocumentRequestDTO request, IFormFile? file, string officerNic)
        {
            var document = await _repository.GetByIdAsync(id);
            if (document == null) throw new Exception("Document not found");
            if (document.Status != "Draft") throw new Exception("Only draft documents can be updated");

            document.Title = request.Title;
            document.Description = request.Description;
            document.CategoryId = request.CategoryId;
            document.ExpiryDate = request.ExpiryDate;
            document.UpdatedAt = DateTime.UtcNow;

            if (file != null && file.Length > 0)
            {
                var uploadsFolder = Path.Combine(_env.WebRootPath ?? "wwwroot", "uploads", "documents");
                Directory.CreateDirectory(uploadsFolder);
                
                var fileName = Guid.NewGuid().ToString() + "_" + file.FileName;
                var absolutePath = Path.Combine(uploadsFolder, fileName);
                
                using (var stream = new FileStream(absolutePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                document.FileName = file.FileName;
                document.FilePath = $"/uploads/documents/{fileName}";
                document.FileType = Path.GetExtension(file.FileName).TrimStart('.');
            }

            if (request.PublishNow)
            {
                document.Status = "Published";
                document.PublishDate = DateTime.UtcNow;
            }

            await _repository.UpdateAsync(document);

            await _repository.AddLogAsync(new GNDocumentLog
            {
                DocumentId = document.DocumentId,
                Action = "Updated" + (request.PublishNow ? " & Published" : ""),
                PerformedByNic = officerNic,
                ActionDate = DateTime.UtcNow
            });

            var updatedDoc = await _repository.GetByIdAsync(document.DocumentId);
            return MapToDTO(updatedDoc!);
        }

        public async Task PublishDocumentAsync(int id, string officerNic)
        {
            var document = await _repository.GetByIdAsync(id);
            if (document == null) throw new Exception("Document not found");
            if (document.Status == "Published") throw new Exception("Document is already published");

            document.Status = "Published";
            document.PublishDate = DateTime.UtcNow;
            document.UpdatedAt = DateTime.UtcNow;

            await _repository.UpdateAsync(document);

            await _repository.AddLogAsync(new GNDocumentLog
            {
                DocumentId = document.DocumentId,
                Action = "Published",
                PerformedByNic = officerNic,
                ActionDate = DateTime.UtcNow
            });
        }

        public async Task DeleteDocumentAsync(int id, string officerNic)
        {
            var document = await _repository.GetByIdAsync(id);
            if (document == null) throw new Exception("Document not found");
            if (document.Status != "Draft" && document.Status != "Published") throw new Exception("Only draft or published documents can be deleted");

            await _repository.AddLogAsync(new GNDocumentLog
            {
                DocumentId = document.DocumentId,
                Action = "Deleted",
                PerformedByNic = officerNic,
                ActionDate = DateTime.UtcNow
            });

            await _repository.DeleteAsync(document);
        }

        private GNDocumentResponseDTO MapToDTO(GNDocument document)
        {
            return new GNDocumentResponseDTO
            {
                DocumentId = document.DocumentId,
                GnOfficerNic = document.GnOfficerNic,
                DivisionId = document.DivisionId,
                Title = document.Title,
                Description = document.Description,
                CategoryId = document.CategoryId,
                CategoryName = document.Category?.CategoryName ?? string.Empty,
                FileName = document.FileName,
                FilePath = document.FilePath,
                FileType = document.FileType,
                Status = document.Status,
                PublishDate = document.PublishDate,
                ExpiryDate = document.ExpiryDate,
                CreatedAt = document.CreatedAt,
                UpdatedAt = document.UpdatedAt
            };
        }
    }
}
