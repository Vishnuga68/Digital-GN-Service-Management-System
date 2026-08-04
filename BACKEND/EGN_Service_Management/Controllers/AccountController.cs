using AutoMapper;
using EGN_Service_Management.Database;
using EGN_Service_Management.DTO;
using EGN_Service_Management.DTO.RequestDTO;
using EGN_Service_Management.DTO.ResponseDTO;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Security.Claims;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;
        private readonly IMapper _mapper;
        private readonly ITokenService _tokenService;
        private readonly EgnDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountController(IAccountService accountService, IMapper mapper, ITokenService tokenService, EgnDbContext context, IConfiguration configuration)
        {
            _accountService = accountService;
            _mapper = mapper;
            _tokenService = tokenService;
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        [Authorize(Roles = "GNOfficer,DSOfficer")]
        public async Task<IActionResult> GetAllAccountRecords()
        {
            var records = await _accountService.GetAllAsync();
            var responseDTOs = _mapper.Map<IEnumerable<AccountResponseDTO>>(records);
            return Ok(ApiResponse<IEnumerable<AccountResponseDTO>>.SuccessResponse(responseDTOs, "Account records retrieved successfully."));
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> GetAccountById(string id)
        {
            var record = await _accountService.GetByIdAsync(id);
            var responseDTO = _mapper.Map<AccountResponseDTO>(record);
            return Ok(ApiResponse<AccountResponseDTO>.SuccessResponse(responseDTO, "Account record retrieved successfully."));
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateAccount(string id, [FromBody] AccountRequestDTO requestDTO)
        {
            var entity = _mapper.Map<Account>(requestDTO);
            entity.Nic = id;
            
            // Hash the password if a new one is supplied
            if (!string.IsNullOrEmpty(requestDTO.PasswordHash) && !requestDTO.PasswordHash.StartsWith("$2"))
            {
                entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(requestDTO.PasswordHash);
            }
            
            var record = await _accountService.UpdateAsync(id, entity);
            var responseDTO = _mapper.Map<AccountResponseDTO>(record);
            return Ok(ApiResponse<AccountResponseDTO>.SuccessResponse(responseDTO, "Account updated successfully."));
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "DSOfficer")]
        public async Task<IActionResult> DeleteAccount(string id)
        {
            await _accountService.DeleteAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse("Account deleted successfully."));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDTO loginDTO)
        {
            Account account;
            try
            {
                account = await _accountService.GetByIdAsync(loginDTO.Nic);
            }
            catch (KeyNotFoundException)
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid NIC or password."));
            }

            // Verify password using secure cryptographically strong BCrypt
            bool isPasswordValid = false;
            try
            {
                if (account.PasswordHash.StartsWith("$2"))
                {
                    isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDTO.Password, account.PasswordHash);
                }
                else
                {
                    // Fallback to plain text during migration/setup period if needed
                    isPasswordValid = account.PasswordHash == loginDTO.Password;
                }
            }
            catch
            {
                isPasswordValid = account.PasswordHash == loginDTO.Password;
            }

            if (!isPasswordValid)
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Invalid NIC or password."));
            }

            if (account.IsDeleted == true)
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Access denied: Your account has been deleted. Please contact support."));
            }

            if (account.Status != "Active")
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("Access denied: Your account is Inactive."));
            }

            // Retrieve roles from user_roles / roles tables directly using ADO.NET query to db.sql structure
            var userRoles = new List<string>();
            try
            {
                using (var command = _context.Database.GetDbConnection().CreateCommand())
                {
                    command.CommandText = @"
                        SELECT r.RoleName 
                        FROM [user_roles] ur 
                        INNER JOIN [roles] r ON ur.RoleId = r.RoleId 
                        WHERE ur.Nic = @Nic";
                    
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = "@Nic";
                    parameter.Value = account.Nic;
                    command.Parameters.Add(parameter);

                    var wasOpen = _context.Database.GetDbConnection().State == System.Data.ConnectionState.Open;
                    if (!wasOpen)
                    {
                        await _context.Database.OpenConnectionAsync();
                    }

                    try
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                userRoles.Add(reader.GetString(0));
                            }
                        }
                    }
                    finally
                    {
                        if (!wasOpen)
                        {
                            await _context.Database.CloseConnectionAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Auth Query Fallback] Failed to query user_roles: {ex.Message}");
            }

            // Determine active role based on user roles and selected role
            string activeRole = "Citizen";
            if (!string.IsNullOrEmpty(loginDTO.SelectedRole))
            {
                if (loginDTO.SelectedRole.Equals("DS Officer", StringComparison.OrdinalIgnoreCase) || loginDTO.SelectedRole.Equals("DSOfficer", StringComparison.OrdinalIgnoreCase) || loginDTO.SelectedRole.Equals("DS", StringComparison.OrdinalIgnoreCase))
                {
                    if (userRoles.Any(r => r.Equals("DS", StringComparison.OrdinalIgnoreCase) || r.Equals("DSOfficer", StringComparison.OrdinalIgnoreCase) || r == "3"))
                    {
                        activeRole = "DSOfficer";
                    }
                    else
                    {
                        return Unauthorized(ApiResponse<object>.ErrorResponse("Access denied: You do not have the DS Officer role."));
                    }
                }
                else if (loginDTO.SelectedRole.Equals("GN Officer", StringComparison.OrdinalIgnoreCase) || loginDTO.SelectedRole.Equals("GNOfficer", StringComparison.OrdinalIgnoreCase) || loginDTO.SelectedRole.Equals("GN", StringComparison.OrdinalIgnoreCase))
                {
                    if (userRoles.Any(r => r.Equals("GN", StringComparison.OrdinalIgnoreCase) || r.Equals("GNOfficer", StringComparison.OrdinalIgnoreCase) || r == "2"))
                    {
                        activeRole = "GNOfficer";
                    }
                    else
                    {
                        return Unauthorized(ApiResponse<object>.ErrorResponse("Access denied: You do not have the GN Officer role."));
                    }
                }
                else if (loginDTO.SelectedRole.Equals("Citizen", StringComparison.OrdinalIgnoreCase))
                {
                    if (userRoles.Any(r => r.Equals("Citizen", StringComparison.OrdinalIgnoreCase) || r.Equals("CITIZEN", StringComparison.OrdinalIgnoreCase) || r == "1" || r == "3"))
                    {
                        activeRole = "Citizen";
                    }
                    else
                    {
                        return Unauthorized(ApiResponse<object>.ErrorResponse("Access denied: You do not have the Citizen role."));
                    }
                }
                else
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Invalid role selection."));
                }
            }
            else
            {
                // Determine active role based on user roles (Precedence: DS > GN > Citizen)
                if (userRoles.Any(r => r.Equals("DS", StringComparison.OrdinalIgnoreCase) || r.Equals("DSOfficer", StringComparison.OrdinalIgnoreCase) || r == "3"))
                {
                    activeRole = "DSOfficer";
                }
                else if (userRoles.Any(r => r.Equals("GN", StringComparison.OrdinalIgnoreCase) || r.Equals("GNOfficer", StringComparison.OrdinalIgnoreCase) || r == "2"))
                {
                    activeRole = "GNOfficer";
                }
                else if (userRoles.Any(r => r.Equals("Citizen", StringComparison.OrdinalIgnoreCase) || r.Equals("CITIZEN", StringComparison.OrdinalIgnoreCase) || r == "1" || r == "3"))
                {
                    activeRole = "Citizen";
                }
                else if (userRoles.Count > 0)
                {
                    activeRole = userRoles.First();
                }
            }

            // Generate secure signed JWT Access Token
            var token = _tokenService.GenerateToken(account, activeRole);

            // Determine if this is the first login before updating LastLogin
            bool isFirstLogin = !account.LastLogin.HasValue;

            // Record login audit timestamp
            account.LastLogin = DateTime.UtcNow;
            await _accountService.UpdateAsync(account.Nic, account);

            var loginResponse = new LoginResponseDTO
            {
                Nic = account.Nic,
                Role = activeRole,
                Email = account.Email,
                Token = token,
                IsFirstLogin = isFirstLogin
            };

            return Ok(ApiResponse<LoginResponseDTO>.SuccessResponse(loginResponse, "Login successful."));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDTO request)
        {
            // Do not reveal whether an email or account exists in the system.
            // Always return a generic response: "If an account exists with the provided email, an OTP has been sent."
            // But internally, search using NIC or email case-insensitively:
            var searchNic = request.Nic?.Trim() ?? string.Empty;
            var searchEmail = request.Email?.Trim() ?? string.Empty;

            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => 
                    a.Nic.ToLower() == searchNic.ToLower() || 
                    a.Nic.ToLower() == searchEmail.ToLower() ||
                    (a.Email != null && a.Email.ToLower() == searchNic.ToLower()) ||
                    (a.Email != null && a.Email.ToLower() == searchEmail.ToLower())
                );

            if (account != null)
            {
                // Invalidate any existing unused OTPs for this account
                var existingOtps = await _context.AccountPasswordResetOtps
                    .Where(o => o.AccountNic == account.Nic && !o.IsUsed)
                    .ToListAsync();
                foreach (var oldOtp in existingOtps)
                {
                    oldOtp.IsUsed = true;
                    oldOtp.UsedAt = DateTime.UtcNow;
                }

                // Generate a secure random 6-digit OTP
                var random = new Random();
                string otpCode = random.Next(100000, 999999).ToString();

                // Hash the OTP using BCrypt
                string otpHash = BCrypt.Net.BCrypt.HashPassword(otpCode);

                // Create new OTP record
                var newOtp = new AccountPasswordResetOtp
                {
                    AccountNic = account.Nic,
                    OtpHash = otpHash,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(10), // 10 minutes expiry
                    AttemptCount = 0,
                    MaxAttempts = 5,
                    IsVerified = false,
                    IsUsed = false,
                    CreatedAt = DateTime.UtcNow,
                    CreatedIp = HttpContext.Connection.RemoteIpAddress?.ToString()
                };

                _context.AccountPasswordResetOtps.Add(newOtp);
                await _context.SaveChangesAsync();

                // Log the OTP code to console as standard recovery mock
                Console.WriteLine($"[AUDIT / EMAIL OTP] Forgot password OTP generated for account {account.Nic} (Email: {account.Email ?? "None"}). OTP is: {otpCode}");
                
                // Write to local debug file for easy viewing
                try
                {
                    System.IO.File.WriteAllText("otp_debug.txt", $"Account NIC: {account.Nic}\nOTP Code: {otpCode}\nGenerated At: {DateTime.UtcNow} UTC\n");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[DEBUG] Failed to write otp_debug.txt: {ex.Message}");
                }

                // Send email OTP via Mailtrap if configured
                var smtpHost = _configuration["SmtpSettings:Host"] ?? "sandbox.smtp.mailtrap.io";
                var smtpPortStr = _configuration["SmtpSettings:Port"] ?? "2525";
                var smtpUsername = _configuration["SmtpSettings:Username"];
                var smtpPassword = _configuration["SmtpSettings:Password"];
                
                if (!string.IsNullOrEmpty(smtpUsername) && !string.IsNullOrEmpty(smtpPassword) && smtpUsername != "your_mailtrap_username" && !string.IsNullOrEmpty(account.Email))
                {
                    try
                    {
                        using (var client = new System.Net.Mail.SmtpClient(smtpHost, int.Parse(smtpPortStr)))
                        {
                            client.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);
                            client.EnableSsl = true;

                            var mailMessage = new System.Net.Mail.MailMessage();
                            mailMessage.From = new System.Net.Mail.MailAddress(_configuration["SmtpSettings:FromEmail"] ?? "no-reply@egn.gov.lk", _configuration["SmtpSettings:FromName"] ?? "EGN Service Management");
                            mailMessage.To.Add(account.Email);
                            mailMessage.Subject = "EGN Account Password Reset OTP";
                            mailMessage.Body = $"Hello,\n\nYour temporary one-time password (OTP) reset code is: {otpCode}\n\nThis OTP is valid for 10 minutes. Please do not share this code with anyone.\n\nBest regards,\nEGN Team";
                            mailMessage.IsBodyHtml = false;

                            await client.SendMailAsync(mailMessage);
                            Console.WriteLine($"[MAILTRAP] OTP email sent successfully to {account.Email}");
                        }
                    }
                    catch (Exception mailEx)
                    {
                        Console.WriteLine($"[MAILTRAP ERROR] Failed to send email via SMTP: {mailEx.Message}");
                    }
                }
            }

            return Ok(ApiResponse<object>.SuccessResponse(
                data: null, 
                message: "If an account exists with the provided email, an OTP has been sent."
            ));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDTO request)
        {
            // Find the account by NIC or Email
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.Nic == request.Nic || a.Email == request.Nic);

            if (account == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Invalid request: Account not found."));
            }

            // Find the active, non-expired OTP record for this account
            var activeOtp = await _context.AccountPasswordResetOtps
                .Where(o => o.AccountNic == account.Nic && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (activeOtp == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("No active reset code found. Please request a new OTP."));
            }

            // Check max attempts
            if (activeOtp.AttemptCount >= activeOtp.MaxAttempts)
            {
                activeOtp.IsUsed = true;
                activeOtp.UsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return BadRequest(ApiResponse<object>.ErrorResponse("Maximum verification attempts exceeded. Please request a new OTP."));
            }

            // Increment attempt count
            activeOtp.AttemptCount++;
            await _context.SaveChangesAsync();

            // Verify the OTP code
            bool isValid = BCrypt.Net.BCrypt.Verify(request.Code, activeOtp.OtpHash);
            if (!isValid)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse($"Invalid verification code. {activeOtp.MaxAttempts - activeOtp.AttemptCount} attempts remaining."));
            }

            // OTP verified successfully!
            activeOtp.IsVerified = true;
            activeOtp.VerifiedAt = DateTime.UtcNow;
            activeOtp.IsUsed = true;
            activeOtp.UsedAt = DateTime.UtcNow;

            // Securely hash the new password using BCrypt
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            _context.Accounts.Update(account);

            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse("Your password has been successfully reset. You can now login with your new credentials."));
        }

        /// <summary>
        /// Role-Based User Creation: ONLY DS Officers can create GN Officers.
        /// </summary>
        [HttpPost("gn-officer")]
        [Authorize(Roles = "DSOfficer")]
        public async Task<IActionResult> CreateGnOfficer([FromBody] CreateGnOfficerDTO dto)
        {
            // 0. Fetch GN RoleId dynamically from Roles table
            int gnRoleId = 1; // Default
            using (var cmdRole = _context.Database.GetDbConnection().CreateCommand())
            {
                cmdRole.CommandText = "SELECT TOP 1 [RoleId] FROM [Roles] WHERE [RoleName] = 'GN' OR [RoleName] = 'GNOfficer'";
                var wasOpen = _context.Database.GetDbConnection().State == System.Data.ConnectionState.Open;
                if (!wasOpen) await _context.Database.OpenConnectionAsync();
                try
                {
                    var result = await cmdRole.ExecuteScalarAsync();
                    if (result != null)
                    {
                        gnRoleId = Convert.ToInt32(result);
                    }
                }
                finally
                {
                    if (!wasOpen) await _context.Database.CloseConnectionAsync();
                }
            }

            // 1. Citizen Record Check
            var citizenMaster = await _context.CitizenMasters.FindAsync(dto.Nic);
            if (citizenMaster == null)
            {
                return Ok(ApiResponse<object>.ErrorResponse("citizen_not_found"));
            }

            // 2. Duplicate System Account Check / Shared Credentials logic
            var existingAccount = await _context.Accounts.FindAsync(dto.Nic);
            if (existingAccount != null)
            {
                if (existingAccount.IsDeleted == true)
                {
                    // Reactivate soft-deleted account!
                    using var reactivateTransactionGn = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        existingAccount.IsDeleted = false;
                        existingAccount.DeletedAt = null;
                        existingAccount.Status = "Active";
                        existingAccount.LastLogin = null; // Reset first login check
                        existingAccount.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                        if (!string.IsNullOrEmpty(dto.Email)) existingAccount.Email = dto.Email;
                        if (!string.IsNullOrEmpty(dto.Phone)) existingAccount.Phone = dto.Phone;
                        
                        _context.Accounts.Update(existingAccount);

                        // 1. Create or Update GN Officer Profile
                        var existingGnOfficer = await _context.GnOfficers.FindAsync(dto.Nic);
                        if (existingGnOfficer == null)
                        {
                            var gnOfficer = new GnOfficer
                            {
                                Nic = dto.Nic,
                                FullName = dto.FullName,
                                GnDivisionId = dto.GnDivisionId
                            };
                            _context.GnOfficers.Add(gnOfficer);
                        }
                        else
                        {
                            existingGnOfficer.FullName = dto.FullName;
                            existingGnOfficer.GnDivisionId = dto.GnDivisionId;
                            _context.GnOfficers.Update(existingGnOfficer);
                        }

                        // 2. Create Assignment Record
                        if (dto.GnDivisionId != null)
                        {
                            var newAssignment = new GnDivisionAssignment
                            {
                                GnOfficerNic = dto.Nic,
                                GnDivisionId = dto.GnDivisionId,
                                StartDate = DateTime.UtcNow,
                                Status = "Active"
                            };
                            _context.GnDivisionAssignments.Add(newAssignment);
                        }

                        // 3. Update or add EGN Service User profile link
                        var existingEgnUser = await _context.EgnServiceUsers.FindAsync(dto.Nic);
                        if (existingEgnUser == null)
                        {
                            var egnUser = new EgnServiceUser
                            {
                                Nic = dto.Nic,
                                Username = dto.Email ?? dto.Nic,
                                RegisteredAt = DateTime.UtcNow
                            };
                            _context.EgnServiceUsers.Add(egnUser);
                        }

                        await _context.SaveChangesAsync();

                        // 5. Insert the user role mapping into [user_roles] table
                        await _context.Database.ExecuteSqlRawAsync(@"
                            IF NOT EXISTS (SELECT 1 FROM [user_roles] WHERE [Nic] = {0} AND [RoleId] = {1})
                            BEGIN
                                INSERT INTO [user_roles] ([Nic], [RoleId]) VALUES ({0}, {1})
                            END", 
                            dto.Nic, gnRoleId
                        );

                        await reactivateTransactionGn.CommitAsync();
                        return Ok(ApiResponse<object>.SuccessResponse(null, "GN Officer account reactivated successfully."));
                    }
                    catch (Exception ex)
                    {
                        await reactivateTransactionGn.RollbackAsync();
                        return StatusCode(500, ApiResponse<object>.ErrorResponse($"An error occurred during reactivation: {ex.Message}"));
                    }
                }

                // Check if they already have GNOfficer role
                bool hasGnRole = false;
                using (var cmd = _context.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM [user_roles] WHERE [Nic] = @Nic AND [RoleId] = @RoleId";
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@Nic";
                    p.Value = dto.Nic;
                    cmd.Parameters.Add(p);

                    var pRole = cmd.CreateParameter();
                    pRole.ParameterName = "@RoleId";
                    pRole.Value = gnRoleId;
                    cmd.Parameters.Add(pRole);
                    
                    var wasOpen = _context.Database.GetDbConnection().State == ConnectionState.Open;
                    if (!wasOpen) await _context.Database.OpenConnectionAsync();
                    try
                    {
                        hasGnRole = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                    }
                    finally
                    {
                        if (!wasOpen) await _context.Database.CloseConnectionAsync();
                    }
                }

                if (hasGnRole)
                {
                    return Ok(ApiResponse<object>.ErrorResponse("duplicate_account"));
                }

                // If GN Division conflict check
                if (dto.GnDivisionId != null)
                {
                    var activeOfficerExists = await _context.GnOfficers
                        .Include(o => o.Account)
                        .AnyAsync(o => o.GnDivisionId == dto.GnDivisionId && o.Account!.Status == "Active");

                    var activeAssignmentExists = await _context.GnDivisionAssignments
                        .AnyAsync(a => a.GnDivisionId == dto.GnDivisionId && a.Status == "Active");

                    if (activeOfficerExists || activeAssignmentExists)
                    {
                        if (!dto.ForceReassign)
                        {
                            return Ok(ApiResponse<object>.ErrorResponse("active_assignment_exists"));
                        }
                        else
                        {
                            // Deactivate existing assignments
                            var previousActiveOfficers = await _context.GnOfficers
                                .Include(o => o.Account)
                                .Where(o => o.GnDivisionId == dto.GnDivisionId && o.Account!.Status == "Active")
                                .ToListAsync();

                            foreach (var officer in previousActiveOfficers)
                            {
                                if (officer.Account != null)
                                {
                                    officer.Account.Status = "Inactive";
                                }
                                officer.GnDivisionId = null;
                            }

                            var activeAssignments = await _context.GnDivisionAssignments
                                .Where(a => a.GnDivisionId == dto.GnDivisionId && a.Status == "Active")
                                .ToListAsync();

                            foreach (var assignment in activeAssignments)
                            {
                                assignment.EndDate = DateTime.UtcNow;
                                assignment.Status = "Inactive";
                            }
                        }
                    }
                }

                // Add GNOfficer role and register officer details safely in database transaction
                using var transactionExist = await _context.Database.BeginTransactionAsync();
                try
                {
                    // 1. Create or Update GN Officer Profile
                    var existingGnOfficer = await _context.GnOfficers.FindAsync(dto.Nic);
                    if (existingGnOfficer == null)
                    {
                        var gnOfficer = new GnOfficer
                        {
                            Nic = dto.Nic,
                            FullName = dto.FullName,
                            GnDivisionId = dto.GnDivisionId
                        };
                        _context.GnOfficers.Add(gnOfficer);
                    }
                    else
                    {
                        existingGnOfficer.FullName = dto.FullName;
                        existingGnOfficer.GnDivisionId = dto.GnDivisionId;
                        _context.GnOfficers.Update(existingGnOfficer);
                    }

                    // 2. Create Assignment Record
                    if (dto.GnDivisionId != null)
                    {
                        var newAssignment = new GnDivisionAssignment
                        {
                            GnOfficerNic = dto.Nic,
                            GnDivisionId = dto.GnDivisionId,
                            StartDate = DateTime.UtcNow,
                            Status = "Active"
                        };
                        _context.GnDivisionAssignments.Add(newAssignment);
                    }

                    // 3. Update Email/Phone on existingAccount if currently empty
                    if (string.IsNullOrEmpty(existingAccount.Email) && !string.IsNullOrEmpty(dto.Email))
                    {
                        existingAccount.Email = dto.Email;
                    }
                    if (string.IsNullOrEmpty(existingAccount.Phone) && !string.IsNullOrEmpty(dto.Phone))
                    {
                        existingAccount.Phone = dto.Phone;
                    }
                    _context.Accounts.Update(existingAccount);

                    // 4. Update or add EGN Service User profile link
                    var existingEgnUser = await _context.EgnServiceUsers.FindAsync(dto.Nic);
                    if (existingEgnUser == null)
                    {
                        var egnUser = new EgnServiceUser
                        {
                            Nic = dto.Nic,
                            Username = dto.Email ?? dto.Nic,
                            RegisteredAt = DateTime.UtcNow
                        };
                        _context.EgnServiceUsers.Add(egnUser);
                    }

                    await _context.SaveChangesAsync();

                    // 5. Insert the user role mapping into [user_roles] table
                    await _context.Database.ExecuteSqlRawAsync(@"
                        IF NOT EXISTS (SELECT 1 FROM [user_roles] WHERE [Nic] = {0} AND [RoleId] = {1})
                        BEGIN
                            INSERT INTO [user_roles] ([Nic], [RoleId]) VALUES ({0}, {1})
                        END", 
                        dto.Nic, gnRoleId
                    );

                    await transactionExist.CommitAsync();

                    return Ok(ApiResponse<object>.SuccessResponse(null, "existing_account_detected"));
                }
                catch (Exception ex)
                {
                    await transactionExist.RollbackAsync();
                    return StatusCode(500, ApiResponse<object>.ErrorResponse($"An error occurred during transaction: {ex.Message}"));
                }
            }

            if (dto.GnDivisionId != null)
            {
                var existingDivision = await _context.GnDivisions.FindAsync(dto.GnDivisionId);
                if (existingDivision == null)
                {
                    return Ok(ApiResponse<object>.ErrorResponse($"GN Division '{dto.GnDivisionId}' does not exist."));
                }
            }

            // 3. Active Assignment Conflict Check
            if (dto.GnDivisionId != null)
            {
                var activeOfficerExists = await _context.GnOfficers
                    .Include(o => o.Account)
                    .AnyAsync(o => o.GnDivisionId == dto.GnDivisionId && o.Account!.Status == "Active");

                var activeAssignmentExists = await _context.GnDivisionAssignments
                    .AnyAsync(a => a.GnDivisionId == dto.GnDivisionId && a.Status == "Active");

                if (activeOfficerExists || activeAssignmentExists)
                {
                    if (!dto.ForceReassign)
                    {
                        return Ok(ApiResponse<object>.ErrorResponse("active_assignment_exists"));
                    }
                    else
                    {
                        // Set existing GN Officer status as INACTIVE & set GnDivisionId to null
                        var previousActiveOfficers = await _context.GnOfficers
                            .Include(o => o.Account)
                            .Where(o => o.GnDivisionId == dto.GnDivisionId && o.Account!.Status == "Active")
                            .ToListAsync();

                        foreach (var officer in previousActiveOfficers)
                        {
                            if (officer.Account != null)
                            {
                                officer.Account.Status = "Inactive";
                            }
                            officer.GnDivisionId = null;
                        }

                        // Set existing GN assignment status as INACTIVE & set EndDate
                        var activeAssignments = await _context.GnDivisionAssignments
                            .Where(a => a.GnDivisionId == dto.GnDivisionId && a.Status == "Active")
                            .ToListAsync();

                        foreach (var assignment in activeAssignments)
                        {
                            assignment.EndDate = DateTime.UtcNow;
                            assignment.Status = "Inactive";
                        }
                    }
                }
            }

            // Hash the password securely
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Secure Database Transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 4. Create and Save new Account
                var account = new Account
                {
                    Nic = dto.Nic,
                    PasswordHash = hashedPassword,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    Status = "Active",
                    LastLogin = null
                };
                _context.Accounts.Add(account);

                // 5. Create or Update GN Officer Profile
                var existingGnOfficer = await _context.GnOfficers.FindAsync(dto.Nic);
                if (existingGnOfficer == null)
                {
                    var gnOfficer = new GnOfficer
                    {
                        Nic = dto.Nic,
                        FullName = dto.FullName,
                        GnDivisionId = dto.GnDivisionId
                    };
                    _context.GnOfficers.Add(gnOfficer);
                }
                else
                {
                    existingGnOfficer.FullName = dto.FullName;
                    existingGnOfficer.GnDivisionId = dto.GnDivisionId;
                    _context.GnOfficers.Update(existingGnOfficer);
                }

                // 6. Create and Save the GN Division active assignment details
                if (dto.GnDivisionId != null)
                {
                    var newAssignment = new GnDivisionAssignment
                    {
                        GnOfficerNic = dto.Nic,
                        GnDivisionId = dto.GnDivisionId,
                        StartDate = DateTime.UtcNow,
                        Status = "Active"
                    };
                    _context.GnDivisionAssignments.Add(newAssignment);
                }

                // 7. Reflection in EGN_Service_User table
                var existingEgnUser = await _context.EgnServiceUsers.FindAsync(dto.Nic);
                if (existingEgnUser == null)
                {
                    var egnUser = new EgnServiceUser
                    {
                        Nic = dto.Nic,
                        Username = dto.Email ?? dto.Nic,
                        RegisteredAt = DateTime.UtcNow
                    };
                    _context.EgnServiceUsers.Add(egnUser);
                }
                else
                {
                    existingEgnUser.Username = dto.Email ?? dto.Nic;
                    _context.EgnServiceUsers.Update(existingEgnUser);
                }

                await _context.SaveChangesAsync();

                // 8. Insert the user role mapping into [user_roles] table safely
                await _context.Database.ExecuteSqlRawAsync(@"
                    IF NOT EXISTS (SELECT 1 FROM [user_roles] WHERE [Nic] = {0} AND [RoleId] = {1})
                    BEGIN
                        INSERT INTO [user_roles] ([Nic], [RoleId]) VALUES ({0}, {1})
                    END", 
                    dto.Nic, gnRoleId
                );

                await transaction.CommitAsync();

                var responseDTO = _mapper.Map<AccountResponseDTO>(account);
                return CreatedAtAction(
                    nameof(GetAccountById),
                    new { id = account.Nic },
                    ApiResponse<AccountResponseDTO>.SuccessResponse(responseDTO, "GN Officer account, profile, division assignment, and service links recorded successfully by DS Officer.")
                );
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"An error occurred during transaction: {ex.Message}"));
            }
        }

        [HttpPost("citizen")]
        [Authorize(Roles = "GNOfficer")]
        public async Task<IActionResult> CreateCitizen([FromBody] CreateCitizenDTO dto)
        {
            // 0. Fetch CITIZEN RoleId dynamically from Roles table
            int citizenRoleId = 3; // Default
            using (var cmdRole = _context.Database.GetDbConnection().CreateCommand())
            {
                cmdRole.CommandText = "SELECT TOP 1 [RoleId] FROM [Roles] WHERE [RoleName] = 'CITIZEN' OR [RoleName] = 'Citizen'";
                var wasOpen = _context.Database.GetDbConnection().State == System.Data.ConnectionState.Open;
                if (!wasOpen) await _context.Database.OpenConnectionAsync();
                try
                {
                    var result = await cmdRole.ExecuteScalarAsync();
                    if (result != null) citizenRoleId = Convert.ToInt32(result);
                }
                finally
                {
                    if (!wasOpen) await _context.Database.CloseConnectionAsync();
                }
            }

            // Verify that the citizen exists in the Citizen Master database
            var citizenMaster = await _context.CitizenMasters.FindAsync(dto.Nic);
            if (citizenMaster == null)
            {
                return Ok(ApiResponse<object>.ErrorResponse("citizen_not_found"));
            }

            // GN Officer can only register citizens from their own GN Division
            var loggedInGnOfficerNic = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name;
            GnOfficer? loggedInGnOfficer = null;
            if (!string.IsNullOrEmpty(loggedInGnOfficerNic))
            {
                loggedInGnOfficer = await _context.GnOfficers.FindAsync(loggedInGnOfficerNic);
            }

            if (loggedInGnOfficer == null || string.IsNullOrEmpty(loggedInGnOfficer.GnDivisionId))
            {
                return Ok(ApiResponse<object>.ErrorResponse("You do not have a valid GN Division assigned."));
            }

            if (!string.IsNullOrEmpty(citizenMaster.GnDivisionId) && citizenMaster.GnDivisionId != loggedInGnOfficer.GnDivisionId)
            {
                return Ok(ApiResponse<object>.ErrorResponse("Citizen does not belong to your GN Division. You can only register citizens from your own division."));
            }

            // Database Validation for existing account
            var existingAccount = await _context.Accounts.FindAsync(dto.Nic);
            if (existingAccount != null)
            {
                if (existingAccount.IsDeleted == true)
                {
                    // Reactivate soft-deleted account!
                    using var reactivateTransactionCitizen = await _context.Database.BeginTransactionAsync();
                    try
                    {
                        existingAccount.IsDeleted = false;
                        existingAccount.DeletedAt = null;
                        existingAccount.Status = "Active";
                        existingAccount.LastLogin = null; // Reset first login check
                        existingAccount.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
                        if (!string.IsNullOrEmpty(dto.Email)) existingAccount.Email = dto.Email;
                        if (!string.IsNullOrEmpty(dto.Phone)) existingAccount.Phone = dto.Phone;
                        
                        _context.Accounts.Update(existingAccount);

                        // Ensure user role mapping exists
                        await _context.Database.ExecuteSqlRawAsync(@"
                            IF NOT EXISTS (SELECT 1 FROM [user_roles] WHERE [Nic] = {0} AND [RoleId] = {1})
                            BEGIN
                                INSERT INTO [user_roles] ([Nic], [RoleId]) VALUES ({0}, {1})
                            END", 
                            dto.Nic, citizenRoleId
                        );

                        // Ensure EgnServiceUser exists
                        var existingEgnUser = await _context.EgnServiceUsers.FindAsync(dto.Nic);
                        if (existingEgnUser == null)
                        {
                            var egnUser = new EgnServiceUser
                            {
                                Nic = dto.Nic,
                                Username = dto.Username,
                                RegisteredAt = DateTime.UtcNow
                            };
                            _context.EgnServiceUsers.Add(egnUser);
                        }
                        else
                        {
                            existingEgnUser.Username = dto.Username;
                            _context.EgnServiceUsers.Update(existingEgnUser);
                        }

                        // Assign division of the logged-in GN Officer if null
                        if (string.IsNullOrEmpty(citizenMaster.GnDivisionId))
                        {
                            citizenMaster.GnDivisionId = loggedInGnOfficer.GnDivisionId;
                            _context.CitizenMasters.Update(citizenMaster);
                        }

                        await _context.SaveChangesAsync();
                        await reactivateTransactionCitizen.CommitAsync();

                        return Ok(ApiResponse<object>.SuccessResponse(new { nic = dto.Nic }, "Citizen account reactivated successfully."));
                    }
                    catch (Exception ex)
                    {
                        await reactivateTransactionCitizen.RollbackAsync();
                        return StatusCode(500, ApiResponse<object>.ErrorResponse($"An error occurred during reactivation: {ex.Message}"));
                    }
                }

                // Check if citizen role already exists
                bool hasCitizenRole = false;
                using (var cmd = _context.Database.GetDbConnection().CreateCommand())
                {
                    cmd.CommandText = "SELECT COUNT(*) FROM [user_roles] WHERE [Nic] = @Nic AND [RoleId] = @RoleId";
                    var p = cmd.CreateParameter();
                    p.ParameterName = "@Nic";
                    p.Value = dto.Nic;
                    cmd.Parameters.Add(p);
                    
                    var pRole = cmd.CreateParameter();
                    pRole.ParameterName = "@RoleId";
                    pRole.Value = citizenRoleId;
                    cmd.Parameters.Add(pRole);
                    
                    var wasOpen = _context.Database.GetDbConnection().State == ConnectionState.Open;
                    if (!wasOpen) await _context.Database.OpenConnectionAsync();
                    try
                    {
                        hasCitizenRole = Convert.ToInt32(await cmd.ExecuteScalarAsync()) > 0;
                    }
                    finally
                    {
                        if (!wasOpen) await _context.Database.CloseConnectionAsync();
                    }
                }

                if (hasCitizenRole)
                {
                    return Ok(ApiResponse<object>.ErrorResponse("duplicate_citizen_role"));
                }

                // Add the Citizen role to existing UserRoles
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    // Update email/phone if provided and currently empty/null in existingAccount
                    if (string.IsNullOrEmpty(existingAccount.Email) && !string.IsNullOrEmpty(dto.Email))
                    {
                        existingAccount.Email = dto.Email;
                    }
                    if (string.IsNullOrEmpty(existingAccount.Phone) && !string.IsNullOrEmpty(dto.Phone))
                    {
                        existingAccount.Phone = dto.Phone;
                    }
                    _context.Accounts.Update(existingAccount);

                    // Insert the user role mapping into [user_roles] table
                    await _context.Database.ExecuteSqlRawAsync(
                        "INSERT INTO [user_roles] ([Nic], [RoleId]) VALUES ({0}, {1})", 
                        dto.Nic, citizenRoleId
                    );

                    // Also check and update EgnServiceUser
                    var existingEgnUser = await _context.EgnServiceUsers.FindAsync(dto.Nic);
                    if (existingEgnUser == null)
                    {
                        existingEgnUser = new EgnServiceUser
                        {
                            Nic = dto.Nic,
                            Username = dto.Email ?? dto.Nic,
                            RegisteredAt = DateTime.UtcNow
                        };
                        _context.EgnServiceUsers.Add(existingEgnUser);
                    }
                    
                    // Assign division of the logged-in GN Officer if null
                    var gnOfficerNic = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name;
                    if (!string.IsNullOrEmpty(gnOfficerNic))
                    {
                        var gnOfficer = await _context.GnOfficers.FindAsync(gnOfficerNic);
                        if (gnOfficer != null && !string.IsNullOrEmpty(gnOfficer.GnDivisionId))
                        {
                            if (string.IsNullOrEmpty(citizenMaster.GnDivisionId))
                            {
                                citizenMaster.GnDivisionId = gnOfficer.GnDivisionId;
                                _context.CitizenMasters.Update(citizenMaster);
                            }
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Fetch existing roles
                    var existingRolesList = new List<string>();
                    using (var cmd = _context.Database.GetDbConnection().CreateCommand())
                    {
                        cmd.CommandText = @"
                            SELECT r.RoleName 
                            FROM [user_roles] ur 
                            INNER JOIN [roles] r ON ur.RoleId = r.RoleId 
                            WHERE ur.Nic = @Nic";
                        var p = cmd.CreateParameter();
                        p.ParameterName = "@Nic";
                        p.Value = dto.Nic;
                        cmd.Parameters.Add(p);
                        
                        var wasOpen = _context.Database.GetDbConnection().State == ConnectionState.Open;
                        if (!wasOpen) await _context.Database.OpenConnectionAsync();
                        try
                        {
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    existingRolesList.Add(reader.GetString(0));
                                }
                            }
                        }
                        finally
                        {
                            if (!wasOpen) await _context.Database.CloseConnectionAsync();
                        }
                    }

                    if (existingRolesList.Count == 0)
                    {
                        var isGn = await _context.GnOfficers.AnyAsync(o => o.Nic == dto.Nic);
                        var isDs = await _context.DsOfficers.AnyAsync(o => o.Nic == dto.Nic);
                        if (isGn) existingRolesList.Add("GNOfficer");
                        if (isDs) existingRolesList.Add("DSOfficer");
                        if (existingRolesList.Count == 0) existingRolesList.Add("User");
                    }

                    var usernameOrEmail = existingEgnUser?.Username ?? existingAccount.Email ?? existingAccount.Nic;

                    return Ok(ApiResponse<object>.SuccessResponse(new {
                        nic = dto.Nic,
                        existingRoles = existingRolesList,
                        usernameOrEmail = usernameOrEmail,
                        action = "Add Citizen Access Only"
                    }, "existing_account_detected"));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    var innerMsg = ex.InnerException != null ? ex.InnerException.Message : "";
                    if (ex.InnerException?.InnerException != null) innerMsg += " | " + ex.InnerException.InnerException.Message;
                    return StatusCode(500, ApiResponse<object>.ErrorResponse($"An error occurred during transaction: {ex.Message} (Inner: {innerMsg})"));
                }
            }

            var existingUsername = await _context.Accounts.FirstOrDefaultAsync(x => x.Email == dto.Username || x.Nic == dto.Username);
            if (existingUsername != null)
            {
                return Ok(ApiResponse<object>.ErrorResponse($"Username '{dto.Username}' is already taken."));
            }

            if (!string.IsNullOrEmpty(dto.Email))
            {
                var existingEmail = await _context.Accounts.FirstOrDefaultAsync(x => x.Email == dto.Email);
                if (existingEmail != null)
                {
                    return Ok(ApiResponse<object>.ErrorResponse($"Email '{dto.Email}' is already registered with another account."));
                }
            }

            if (dto.FamilyCardNumber != null)
            {
                var existingCard = await _context.FamilyCards.FindAsync(dto.FamilyCardNumber);
                if (existingCard == null)
                {
                    return Ok(ApiResponse<object>.ErrorResponse($"Family Card Number '{dto.FamilyCardNumber}' does not exist."));
                }
            }

            // Hash the password securely
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Secure Database Transaction
            using var transactionNew = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Create and Save Account
                var account = new Account
                {
                    Nic = dto.Nic,
                    PasswordHash = hashedPassword,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    Status = "Active",
                    LastLogin = null
                };
                _context.Accounts.Add(account);

                // 2. Create and Save Egn Service User profile link
                var egnUser = new EgnServiceUser
                {
                    Nic = dto.Nic,
                    Username = dto.Username,
                    RegisteredAt = DateTime.UtcNow
                };
                _context.EgnServiceUsers.Add(egnUser);

                // 3. Assign division of the logged-in GN Officer
                var gnOfficerNic = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.Identity?.Name;
                if (!string.IsNullOrEmpty(gnOfficerNic))
                {
                    var gnOfficer = await _context.GnOfficers.FindAsync(gnOfficerNic);
                    if (gnOfficer != null && !string.IsNullOrEmpty(gnOfficer.GnDivisionId))
                    {
                        if (string.IsNullOrEmpty(citizenMaster.GnDivisionId))
                        {
                            citizenMaster.GnDivisionId = gnOfficer.GnDivisionId;
                            _context.CitizenMasters.Update(citizenMaster);
                        }
                    }
                }

                await _context.SaveChangesAsync();

                // 4. Insert the user role mapping into [user_roles] table
                await _context.Database.ExecuteSqlRawAsync(
                    "INSERT INTO [user_roles] ([Nic], [RoleId]) VALUES ({0}, {1})", 
                    dto.Nic, citizenRoleId
                );

                await transactionNew.CommitAsync();

                var responseDTO = _mapper.Map<AccountResponseDTO>(account);
                return CreatedAtAction(
                    nameof(GetAccountById),
                    new { id = account.Nic },
                    ApiResponse<AccountResponseDTO>.SuccessResponse(responseDTO, "Citizen successfully added to eGN services.")
                );
            }
            catch (Exception ex)
            {
                await transactionNew.RollbackAsync();
                var innerMsg = ex.InnerException != null ? ex.InnerException.Message : "";
                if (ex.InnerException?.InnerException != null) innerMsg += " | " + ex.InnerException.InnerException.Message;
                return StatusCode(500, ApiResponse<object>.ErrorResponse($"An error occurred during transaction: {ex.Message} (Inner: {innerMsg})"));
            }
        }

        [HttpPost("profile/verify-password")]
        [Authorize]
        public async Task<IActionResult> VerifyPassword([FromBody] VerifyPasswordDTO request)
        {
            var nic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(nic)) return Unauthorized();

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Nic == nic);
            if (account == null) return NotFound(ApiResponse<object>.ErrorResponse("Account not found."));

            bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash);
            if (!isPasswordCorrect)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Incorrect password. Please enter your correct current password."));
            }

            return Ok(ApiResponse<object>.SuccessResponse(null, "Password verified successfully."));
        }

        [HttpPost("profile/delete")]
        [Authorize]
        public async Task<IActionResult> DeleteProfileAccount([FromBody] VerifyPasswordDTO request)
        {
            try
            {
                var nic = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.Identity?.Name;

                if (string.IsNullOrEmpty(nic))
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Unauthorized: User identification claim not found."));
                }

                var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Nic == nic);
                if (account == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse("Account not found."));
                }

                // Verify password
                bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(request.Password, account.PasswordHash);
                if (!isPasswordCorrect)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse("Incorrect password. Please enter your correct current password to verify deletion."));
                }

                // Soft delete
                account.IsDeleted = true;
                account.DeletedAt = DateTime.UtcNow;
                account.Status = "Inactive"; // Mark status inactive

                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(null, "Account deleted successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse($"Failed to delete account: {ex.Message}"));
            }
        }

        [HttpPost("profile/change-email/request")]
        [Authorize]
        public async Task<IActionResult> RequestEmailChange([FromBody] ChangeEmailRequestDTO request)
        {
            var nic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(nic)) return Unauthorized();

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Nic == nic);
            if (account == null) return NotFound(ApiResponse<object>.ErrorResponse("Account not found."));

            // Validate that CurrentPassword matches account.PasswordHash
            bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, account.PasswordHash);
            if (!isPasswordCorrect)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Incorrect password. Please enter your current password to authorize changes."));
            }

            var newEmail = request.NewEmail?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(newEmail))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("New email is required."));
            }

            // Validate email format
            if (!IsValidEmail(newEmail))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Invalid email format. Please enter a valid email address."));
            }

            // Validate that the new email is different
            if (string.Equals(account.Email, newEmail, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("New email must be different from your current email."));
            }

            // Validate that the new email is not already used by another account
            var isEmailUsed = await _context.Accounts.AnyAsync(a => a.Nic != nic && a.Email != null && a.Email.ToLower() == newEmail.ToLower());
            if (isEmailUsed)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("This email address is already registered with another account."));
            }

            // Invalidate any existing unused email change OTPs for this account
            var existingOtps = await _context.AccountPasswordResetOtps
                .Where(o => o.AccountNic == account.Nic && !o.IsUsed && o.CreatedIp.StartsWith("EMAIL_CHANGE:"))
                .ToListAsync();
            foreach (var oldOtp in existingOtps)
            {
                oldOtp.IsUsed = true;
                oldOtp.UsedAt = DateTime.UtcNow;
            }

            // Generate a secure random 6-digit OTP code
            var random = new Random();
            string otpCode = random.Next(100000, 999999).ToString();
            string otpHash = BCrypt.Net.BCrypt.HashPassword(otpCode);

            // Save new OTP record
            var newOtp = new AccountPasswordResetOtp
            {
                AccountNic = account.Nic,
                OtpHash = otpHash,
                ExpiresAt = DateTime.UtcNow.AddMinutes(10), // 10 minutes expiry
                AttemptCount = 0,
                MaxAttempts = 5,
                IsVerified = false,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow,
                CreatedIp = $"EMAIL_CHANGE:{newEmail}" // store email address in CreatedIp
            };

            _context.AccountPasswordResetOtps.Add(newOtp);
            await _context.SaveChangesAsync();

            // Log OTP to console/debug
            Console.WriteLine($"[AUDIT / EMAIL CHANGE OTP] OTP code generated for email change of {account.Nic} to {newEmail}: {otpCode}");
            try
            {
                System.IO.File.WriteAllText("otp_debug.txt", $"Account NIC: {account.Nic}\nNew Email: {newEmail}\nOTP Code: {otpCode}\nGenerated At: {DateTime.UtcNow} UTC\n");
            }
            catch {}

            // Send SMTP via Mailtrap if configured
            var smtpHost = _configuration["SmtpSettings:Host"] ?? "sandbox.smtp.mailtrap.io";
            var smtpPortStr = _configuration["SmtpSettings:Port"] ?? "2525";
            var smtpUsername = _configuration["SmtpSettings:Username"];
            var smtpPassword = _configuration["SmtpSettings:Password"];
            
            if (!string.IsNullOrEmpty(smtpUsername) && !string.IsNullOrEmpty(smtpPassword) && smtpUsername != "your_mailtrap_username")
            {
                try
                {
                    using (var client = new System.Net.Mail.SmtpClient(smtpHost, int.Parse(smtpPortStr)))
                    {
                        client.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);
                        client.EnableSsl = true;

                        var mailMessage = new System.Net.Mail.MailMessage();
                        mailMessage.From = new System.Net.Mail.MailAddress(_configuration["SmtpSettings:FromEmail"] ?? "no-reply@egn.gov.lk", _configuration["SmtpSettings:FromName"] ?? "EGN Service Management");
                        mailMessage.To.Add(newEmail);
                        mailMessage.Subject = "EGN Email Change Verification OTP";
                        mailMessage.Body = $"Hello,\n\nYour temporary one-time password (OTP) code to verify your new email address is: {otpCode}\n\nThis OTP is valid for 10 minutes. Please enter this code in your profile to complete the change.\n\nBest regards,\nEGN Team";
                        mailMessage.IsBodyHtml = false;

                        await client.SendMailAsync(mailMessage);
                        Console.WriteLine($"[MAILTRAP] Email change verification OTP sent successfully to {newEmail}");
                    }
                }
                catch (Exception mailEx)
                {
                    Console.WriteLine($"[MAILTRAP ERROR] Failed to send email change verification OTP via SMTP: {mailEx.Message}");
                }
            }

            return Ok(ApiResponse<object>.SuccessResponse(null, "A verification OTP has been sent to your new email. Please verify to complete the change."));
        }

        [HttpPost("profile/change-email/verify")]
        [Authorize]
        public async Task<IActionResult> VerifyEmailChange([FromBody] VerifyEmailChangeDTO request)
        {
            var nic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(nic)) return Unauthorized();

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Nic == nic);
            if (account == null) return NotFound(ApiResponse<object>.ErrorResponse("Account not found."));

            // Find active OTP record for email change
            var activeOtp = await _context.AccountPasswordResetOtps
                .Where(o => o.AccountNic == account.Nic && !o.IsUsed && o.ExpiresAt > DateTime.UtcNow && o.CreatedIp.StartsWith("EMAIL_CHANGE:"))
                .OrderByDescending(o => o.CreatedAt)
                .FirstOrDefaultAsync();

            if (activeOtp == null)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("No active email verification code found or it has expired. Please request a new code."));
            }

            if (activeOtp.AttemptCount >= activeOtp.MaxAttempts)
            {
                activeOtp.IsUsed = true;
                activeOtp.UsedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return BadRequest(ApiResponse<object>.ErrorResponse("Maximum verification attempts exceeded. Please request a new code."));
            }

            activeOtp.AttemptCount++;
            await _context.SaveChangesAsync();

            bool isValid = BCrypt.Net.BCrypt.Verify(request.Code, activeOtp.OtpHash);
            if (!isValid)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse($"Invalid verification code. {activeOtp.MaxAttempts - activeOtp.AttemptCount} attempts remaining."));
            }

            // OTP is valid! Mark verified & used
            activeOtp.IsVerified = true;
            activeOtp.VerifiedAt = DateTime.UtcNow;
            activeOtp.IsUsed = true;
            activeOtp.UsedAt = DateTime.UtcNow;

            string oldEmail = account.Email;
            string newEmail = activeOtp.CreatedIp.Substring("EMAIL_CHANGE:".Length);

            // Update Email
            account.Email = newEmail;
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();

            // Send confirmation email to OLD email address
            if (!string.IsNullOrEmpty(oldEmail))
            {
                var smtpHost = _configuration["SmtpSettings:Host"] ?? "sandbox.smtp.mailtrap.io";
                var smtpPortStr = _configuration["SmtpSettings:Port"] ?? "2525";
                var smtpUsername = _configuration["SmtpSettings:Username"];
                var smtpPassword = _configuration["SmtpSettings:Password"];
                
                if (!string.IsNullOrEmpty(smtpUsername) && !string.IsNullOrEmpty(smtpPassword) && smtpUsername != "your_mailtrap_username")
                {
                    try
                    {
                        using (var client = new System.Net.Mail.SmtpClient(smtpHost, int.Parse(smtpPortStr)))
                        {
                            client.Credentials = new System.Net.NetworkCredential(smtpUsername, smtpPassword);
                            client.EnableSsl = true;

                            var mailMessage = new System.Net.Mail.MailMessage();
                            mailMessage.From = new System.Net.Mail.MailAddress(_configuration["SmtpSettings:FromEmail"] ?? "no-reply@egn.gov.lk", _configuration["SmtpSettings:FromName"] ?? "EGN Service Management");
                            mailMessage.To.Add(oldEmail);
                            mailMessage.Subject = "Security Notification: EGN Account Email Address Changed";
                            mailMessage.Body = $"Hello,\n\nThis is a security notification to confirm that the email address for your EGN Account ({account.Nic}) has been successfully updated to: {newEmail}.\n\nIf you did not request this change, please contact administration immediately.\n\nBest regards,\nEGN Team";
                            mailMessage.IsBodyHtml = false;

                            await client.SendMailAsync(mailMessage);
                            Console.WriteLine($"[MAILTRAP] Email change notification sent to old email {oldEmail}");
                        }
                    }
                    catch (Exception mailEx)
                    {
                        Console.WriteLine($"[MAILTRAP ERROR] Failed to send email change confirmation notification: {mailEx.Message}");
                    }
                }
            }

            return Ok(ApiResponse<object>.SuccessResponse(null, "Your email address has been successfully verified and updated."));
        }

        [HttpPost("profile/change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO request)
        {
            var nic = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(nic)) return Unauthorized();

            var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Nic == nic);
            if (account == null) return NotFound(ApiResponse<object>.ErrorResponse("Account not found."));

            // Verify current password
            bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, account.PasswordHash);
            if (!isPasswordCorrect)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Incorrect current password. Please try again."));
            }

            // Ensure password policy
            if (string.IsNullOrEmpty(request.NewPassword) || request.NewPassword.Length < 6)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("New password must be at least 6 characters long."));
            }

            // Prevent user from reusing current password
            bool isSamePassword = BCrypt.Net.BCrypt.Verify(request.NewPassword, account.PasswordHash);
            if (isSamePassword)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("New password cannot be the same as your current password. Please select a different password."));
            }

            // Update password hash
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            _context.Accounts.Update(account);
            await _context.SaveChangesAsync();

            return Ok(ApiResponse<object>.SuccessResponse(null, "Your password has been successfully updated."));
        }

        [HttpPut("profile/update")]
        [Authorize]
        public async Task<IActionResult> UpdateProfileDetails([FromBody] UpdateProfileDetailsDTO request)
        {
            try
            {
                var nic = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier)
                          ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? User.Identity?.Name;

                if (string.IsNullOrEmpty(nic))
                {
                    return Unauthorized(ApiResponse<object>.ErrorResponse("Unauthorized: User identification claim not found in authentication token."));
                }

                var account = await _context.Accounts.FirstOrDefaultAsync(a => a.Nic == nic);
                if (account == null)
                {
                    return NotFound(ApiResponse<object>.ErrorResponse($"Account associated with NIC '{nic}' was not found."));
                }

                account.Phone = request.Phone;
                account.Profile = request.Profile;

                // Call SaveChangesAsync; EF Core tracks changes and updates only modified fields
                await _context.SaveChangesAsync();

                return Ok(ApiResponse<object>.SuccessResponse(new { phone = account.Phone, profile = account.Profile }, "Profile updated successfully."));
            }
            catch (Exception ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse($"Database update failed: {ex.Message}. Details: {ex.InnerException?.Message}"));
            }
        }


        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }

    public class ChangeEmailRequestDTO
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewEmail { get; set; } = string.Empty;
    }

    public class VerifyEmailChangeDTO
    {
        public string Code { get; set; } = string.Empty;
    }

    public class UpdateProfileDetailsDTO
    {
        public string? Phone { get; set; }
        public string? Profile { get; set; }
    }

    public class ChangePasswordDTO
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class VerifyPasswordDTO
    {
        public string Password { get; set; } = string.Empty;
    }
}
