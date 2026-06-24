using EGN_Service_Management.Database;
using EGN_Service_Management.DTO;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace EGN_Service_Management.Middleware
{
    public class GnActiveAssignmentMiddleware
    {
        private readonly RequestDelegate _next;

        public GnActiveAssignmentMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, EgnDbContext dbContext)
        {
            if (context.User.Identity?.IsAuthenticated == true)
            {
                var roleClaim = context.User.FindFirst(ClaimTypes.Role)?.Value;
                if (roleClaim == "GNOfficer")
                {
                    var nicClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    if (!string.IsNullOrEmpty(nicClaim))
                    {
                        var account = await dbContext.Accounts
                            .Include(a => a.GnOfficer)
                            .FirstOrDefaultAsync(a => a.Nic == nicClaim);

                        if (account == null || account.Status != "Active" || account.GnOfficer == null || string.IsNullOrEmpty(account.GnOfficer.GnDivisionId))
                        {
                            context.Response.StatusCode = StatusCodes.Status403Forbidden;
                            context.Response.ContentType = "application/json";

                            var responseEnvelope = ApiResponse<object>.ErrorResponse("Access denied: You do not have an active Grama Niladhari assignment.");
                            var options = new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                            };
                            var jsonString = JsonSerializer.Serialize(responseEnvelope, options);
                            await context.Response.WriteAsync(jsonString);
                            return;
                        }
                    }
                }
            }

            await _next(context);
        }
    }
}
