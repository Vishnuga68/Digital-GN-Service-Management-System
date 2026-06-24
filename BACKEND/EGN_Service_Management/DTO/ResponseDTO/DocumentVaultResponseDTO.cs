using System;

namespace EGN_Service_Management.DTO.ResponseDTO
{
    public class DocumentVaultResponseDTO
    {
        public int VaultDocumentId { get; set; }
        public string NIC { get; set; }
        public string RoleId { get; set; }
        public string CategoryName { get; set; }
        public string DocumentName { get; set; }
        public string OriginalFileName { get; set; }
        public string FilePath { get; set; }
        public string FileType { get; set; }
        public string Description { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}
