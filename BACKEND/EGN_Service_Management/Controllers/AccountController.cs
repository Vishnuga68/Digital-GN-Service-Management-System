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

        public AccountController(IAccountService accountService, IMapper mapper, ITokenService tokenService, EgnDbContext context)
        {
            _accountService = accountService;
            _mapper = mapper;
            _tokenService = tokenService;
            _context = context;
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

            // Record login audit timestamp
            account.LastLogin = DateTime.UtcNow;
            await _accountService.UpdateAsync(account.Nic, account);

            var loginResponse = new LoginResponseDTO
            {
                Nic = account.Nic,
                Role = activeRole,
                Email = account.Email,
                Token = token
            };

            return Ok(ApiResponse<LoginResponseDTO>.SuccessResponse(loginResponse, "Login successful."));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDTO request)
        {
            Account account;
            try
            {
                account = await _accountService.GetByIdAsync(request.Nic);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("No account found with this NIC."));
            }

            if (account.Email == null || !account.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("The email does not match our records for this account."));
            }

            // Generate a mock 6-digit verification code
            string mockOtpCode = "123456";
            
            // Console Logging for local debugging so they can instantly test ResetPassword
            Console.WriteLine($"[AUDIT / EMAIL MOCK] Password recovery requested for NIC: {request.Nic}. Temporary recovery OTP is: {mockOtpCode}");

            return Ok(ApiResponse<object>.SuccessResponse(
                data: new { testOtpCode = mockOtpCode }, 
                message: "A password recovery code has been generated. For local testing, use code '123456'."
            ));
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDTO request)
        {
            if (request.Code != "123456")
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Invalid or expired verification code."));
            }

            Account account;
            try
            {
                account = await _accountService.GetByIdAsync(request.Nic);
            }
            catch (KeyNotFoundException)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("Account not found."));
            }

            // Securely hash the new password using BCrypt
            account.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _accountService.UpdateAsync(account.Nic, account);

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
    }
}
