using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface ITokenService
    {
        string GenerateToken(Account account, string activeRole);
    }
}
