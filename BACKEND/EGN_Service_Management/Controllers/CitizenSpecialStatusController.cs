using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EGN_Service_Management.Database;
using EGN_Service_Management.Entity;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CitizenSpecialStatusController : ControllerBase
    {
        private readonly EgnDbContext _context;

        public CitizenSpecialStatusController(EgnDbContext context)
        {
            _context = context;
        }

        [HttpGet("{nic}")]
        public async Task<IActionResult> GetWelfareAndSuggestions(string nic)
        {
            var citizen = await _context.CitizenMasters
                .FirstOrDefaultAsync(c => c.Nic == nic);

            if (citizen == null)
            {
                return NotFound(new { success = false, message = "Citizen not found" });
            }

            var today = DateOnly.FromDateTime(DateTime.Today);
            int age = today.Year - citizen.Dob.Year;
            if (citizen.Dob > today.AddYears(-age)) age--;

            var welfare = await _context.CitizenWelfareStatuses
                .FirstOrDefaultAsync(w => w.CitizenNic == nic && w.IsActive);

            var suggestions = new List<object>();
            var confirmedWelfare = new List<object>();

            // 1. Confirmed Welfare Mapping
            if (welfare != null)
            {
                if (welfare.IsAswesuma)
                {
                    confirmedWelfare.Add(new
                    {
                        programName = "Aswesuma Welfare Benefit Scheme",
                        statusText = "Currently Receiving",
                        lastVerifiedDate = welfare.LastVerifiedDate?.ToString("yyyy-MM-dd"),
                        verifiedBy = welfare.VerifiedByGnOfficer
                    });
                }
                if (welfare.IsPublicAssistanceEligible)
                {
                    confirmedWelfare.Add(new
                    {
                        programName = "Public Assistance Programme",
                        statusText = "Currently Receiving",
                        lastVerifiedDate = welfare.LastVerifiedDate?.ToString("yyyy-MM-dd"),
                        verifiedBy = welfare.VerifiedByGnOfficer
                    });
                }
                if (welfare.IsElderlyEligible)
                {
                    confirmedWelfare.Add(new
                    {
                        programName = "Elderly Allowance",
                        statusText = "Confirmed",
                        lastVerifiedDate = welfare.LastVerifiedDate?.ToString("yyyy-MM-dd"),
                        verifiedBy = welfare.VerifiedByGnOfficer
                    });
                }
                if (welfare.IsDisabilityEligible)
                {
                    confirmedWelfare.Add(new
                    {
                        programName = "Disability Allowance",
                        statusText = "Currently Receiving",
                        lastVerifiedDate = welfare.LastVerifiedDate?.ToString("yyyy-MM-dd"),
                        verifiedBy = welfare.VerifiedByGnOfficer
                    });
                }
                if (welfare.IsKidneyPatient)
                {
                    confirmedWelfare.Add(new
                    {
                        programName = "Kidney Disease Allowance",
                        statusText = "Currently Receiving",
                        lastVerifiedDate = welfare.LastVerifiedDate?.ToString("yyyy-MM-dd"),
                        verifiedBy = welfare.VerifiedByGnOfficer
                    });
                }
                if (welfare.IsPregnantMotherEligible)
                {
                    confirmedWelfare.Add(new
                    {
                        programName = "Pregnant Mother (Poshana Malla)",
                        statusText = "Currently Receiving",
                        lastVerifiedDate = welfare.LastVerifiedDate?.ToString("yyyy-MM-dd"),
                        verifiedBy = welfare.VerifiedByGnOfficer
                    });
                }
                if (welfare.IsHousingAssistanceEligible)
                {
                    confirmedWelfare.Add(new
                    {
                        programName = "Housing Assistance Programmes",
                        statusText = "Currently Receiving",
                        lastVerifiedDate = welfare.LastVerifiedDate?.ToString("yyyy-MM-dd"),
                        verifiedBy = welfare.VerifiedByGnOfficer
                    });
                }
            }

            // 2. Suggestions Mapping (only when citizen is Alive)
            if (citizen.LifeStatus.Equals("Alive", StringComparison.OrdinalIgnoreCase))
            {
                // A. Voter Registration Suggestion
                // Conditions: age >= 18, LifeStatus is Alive, and not registered in voter registration registry.
                // Since there is no voter table, we treat isRegisteredVoter as false (always suggest if >= 18).
                bool isRegisteredVoter = false; 
                if (age >= 18 && !isRegisteredVoter)
                {
                    suggestions.Add(new
                    {
                        type = "Voter",
                        title = "Eligible for Voter Registration Review",
                        reason = $"Voter Registration Review – Citizen is {age} years old and not registered."
                    });
                }

                // B. Elderly Allowance Suggestion
                // Conditions: age >= 60, not already receiving confirmed Elderly Allowance.
                bool alreadyReceivingElderly = welfare != null && welfare.IsElderlyEligible;
                if (age >= 60 && !alreadyReceivingElderly)
                {
                    suggestions.Add(new
                    {
                        type = "Elderly",
                        title = "Eligible for Elderly Allowance Review",
                        reason = $"Elderly Allowance Review – Citizen is {age} years old."
                    });
                }

                // C. Low-Income Family Suggestion
                // Conditions: is official head of household, total family monthly income < 30000.
                if (!string.IsNullOrEmpty(citizen.FamilyCardNumber))
                {
                    var familyCard = await _context.FamilyCards
                        .FirstOrDefaultAsync(f => f.FamilyCardNumber == citizen.FamilyCardNumber);

                    if (familyCard != null && string.Equals(familyCard.HeadOfHouseholdNic.Trim(), citizen.Nic.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        // Calculate total income for alive family members
                        var familyMembersIncome = await _context.CitizenMasters
                            .Where(c => c.FamilyCardNumber == citizen.FamilyCardNumber && c.LifeStatus == "Alive")
                            .Select(c => c.MonthlyIncome ?? 0)
                            .ToListAsync();

                        decimal totalFamilyIncome = familyMembersIncome.Sum();

                        if (totalFamilyIncome < 30000)
                        {
                            bool alreadyReceivingWelfare = welfare != null && (welfare.IsAswesuma || welfare.IsPublicAssistanceEligible);
                            suggestions.Add(new
                            {
                                type = "LowIncome",
                                title = "Eligible for Low-Income Assistance Review",
                                reason = alreadyReceivingWelfare 
                                    ? "Low-Income Family Review (Household already receiving welfare support)"
                                    : "Low-Income Family Review",
                                details = $"Total recorded family monthly income: Rs. {totalFamilyIncome:N2}"
                            });
                        }
                    }
                }
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    suggestions,
                    confirmedWelfare
                }
            });
        }
    }
}
