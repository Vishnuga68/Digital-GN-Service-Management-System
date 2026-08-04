namespace EGN_Service_Management.DTO.ResponseDTO
{
    public class LoginResponseDTO
    {
        public string Nic { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Token { get; set; } = string.Empty;
        public bool IsFirstLogin { get; set; }
    }
}
