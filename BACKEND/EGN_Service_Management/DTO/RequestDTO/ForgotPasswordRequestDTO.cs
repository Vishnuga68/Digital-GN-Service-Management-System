using System.ComponentModel.DataAnnotations;

namespace EGN_Service_Management.DTO.RequestDTO
{
    public class ForgotPasswordRequestDTO
    {
        public string? Nic { get; set; }
        public string? Email { get; set; }
    }
}
