using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IRepositories;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace EGN_Service_Management.Services
{
    public class DocumentVaultService : IDocumentVaultService
    {
        private readonly IDocumentVaultRepository _repository;
        private readonly IWebHostEnvironment _environment;

        public DocumentVaultService(IDocumentVaultRepository repository, IWebHostEnvironment environment)
        {
            _repository = repository;
            _environment = environment;
        }

        public async Task<IEnumerable<DocumentVaultResponseDTO>> GetDocumentsAsync(string nic, string roleId)
        {
            var documents = await _repository.GetDocumentsAsync(nic, roleId);

            return documents.Select(d => new DocumentVaultResponseDTO
            {
                VaultDocumentId = d.VaultDocumentId,
                NIC = d.NIC,
                RoleId = d.RoleId,
                CategoryName = d.CategoryName,
                DocumentName = d.DocumentName,
                OriginalFileName = d.OriginalFileName,
                FilePath = d.FilePath,
                FileType = d.FileType,
                Description = d.Description,
                UploadedAt = d.UploadedAt
            });
        }

        public async Task<DocumentVaultResponseDTO> UploadDocumentAsync(DocumentVaultRequestDTO requestDto, string nic, string roleId, int uploadedBy)
        {
            var file = requestDto.File;
            if (file == null || file.Length == 0)
                throw new ArgumentException("Invalid file");

            var ext = Path.GetExtension(file.FileName).ToLower();
            var allowedExts = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx", ".xls", ".xlsx" };
            if (!allowedExts.Contains(ext))
                throw new ArgumentException("Unsupported file type");

            var uploadDir = Path.Combine(_environment.WebRootPath ?? "wwwroot", "Vaults", nic, roleId);
            if (!Directory.Exists(uploadDir))
            {
                Directory.CreateDirectory(uploadDir);
            }

            var uniqueFileName = $"{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadDir, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var relativePath = $"/Vaults/{nic}/{roleId}/{uniqueFileName}";

            var document = new DocumentVaultDocument
            {
                NIC = nic,
                RoleId = roleId,
                CategoryName = requestDto.CategoryName,
                DocumentName = requestDto.DocumentName,
                OriginalFileName = file.FileName,
                FilePath = relativePath,
                FileType = ext.TrimStart('.'),
                Description = requestDto.Description,
                IsActive = true,
                UploadedAt = DateTime.UtcNow,
                UploadedBy = uploadedBy
            };

            await _repository.AddDocumentAsync(document);

            return new DocumentVaultResponseDTO
            {
                VaultDocumentId = document.VaultDocumentId,
                NIC = document.NIC,
                RoleId = document.RoleId,
                CategoryName = document.CategoryName,
                DocumentName = document.DocumentName,
                OriginalFileName = document.OriginalFileName,
                FilePath = document.FilePath,
                FileType = document.FileType,
                Description = document.Description,
                UploadedAt = document.UploadedAt
            };
        }

        public async Task<bool> DeleteDocumentAsync(int documentId, string nic, string roleId, int deletedBy)
        {
            var document = await _repository.GetDocumentByIdAsync(documentId, nic, roleId);
            if (document == null)
                return false;

            await _repository.RemoveDocumentAsync(document);
            return true;
        }
    }
}
