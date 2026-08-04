using EGN_Service_Management.Database;
using EGN_Service_Management.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EGN_Service_Management.Services
{
    public class GnAvailabilityGeneratorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GnAvailabilityGeneratorService> _logger;

        public GnAvailabilityGeneratorService(IServiceProvider serviceProvider, ILogger<GnAvailabilityGeneratorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("GN Availability Generator Service is starting.");

            // Run generation at startup
            await GenerateAvailabilityAsync();

            // Run generation daily
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Delay for 24 hours, checking for cancellation every second
                    await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                    await GenerateAvailabilityAsync();
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in GnAvailabilityGeneratorService task loop.");
                }
            }

            _logger.LogInformation("GN Availability Generator Service is stopping.");
        }

        private async Task GenerateAvailabilityAsync()
        {
            _logger.LogInformation("Starting GN Availability monthly generation check...");

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<EgnDbContext>();

                try
                {
                    // Find Sri Lanka timezone
                    TimeZoneInfo slZone;
                    try {
                        slZone = TimeZoneInfo.FindSystemTimeZoneById("Sri Lanka Standard Time");
                    } catch {
                        slZone = TimeZoneInfo.CreateCustomTimeZone("SLST", TimeSpan.FromHours(5.5), "Sri Lanka Standard Time", "Sri Lanka Standard Time");
                    }

                    var slNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, slZone);
                    var today = DateOnly.FromDateTime(slNow);
                    var oneMonthAhead = today.AddMonths(1);

                    // Load Sri Lanka public holidays from frontend ICS assets
                    var holidays = new HashSet<DateOnly>();
                    string path = Path.Combine(AppContext.BaseDirectory, "srilanka-holidays-2026.ics");
                    if (!File.Exists(path))
                    {
                        path = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "FRONTEND", "src", "assets", "srilanka-holidays-2026.ics");
                    }
                    if (!File.Exists(path))
                    {
                        path = Path.Combine(Directory.GetCurrentDirectory(), "FRONTEND", "src", "assets", "srilanka-holidays-2026.ics");
                    }

                    if (File.Exists(path))
                    {
                        _logger.LogInformation($"Loading holidays from ICS file at {path}");
                        var lines = File.ReadAllLines(path);
                        foreach (var line in lines)
                        {
                            if (line.StartsWith("DTSTART;VALUE=DATE:"))
                            {
                                var datePart = line.Substring("DTSTART;VALUE=DATE:".Length).Trim();
                                if (datePart.Length >= 8 && int.TryParse(datePart.Substring(0, 4), out int year) &&
                                    int.TryParse(datePart.Substring(4, 2), out int month) &&
                                    int.TryParse(datePart.Substring(6, 2), out int day))
                                {
                                    holidays.Add(new DateOnly(year, month, day));
                                }
                            }
                            else if (line.StartsWith("DTSTART:"))
                            {
                                var datePart = line.Substring("DTSTART:".Length).Trim();
                                if (datePart.Length >= 8 && int.TryParse(datePart.Substring(0, 4), out int year) &&
                                    int.TryParse(datePart.Substring(4, 2), out int month) &&
                                    int.TryParse(datePart.Substring(6, 2), out int day))
                                {
                                    holidays.Add(new DateOnly(year, month, day));
                                }
                            }
                        }
                        _logger.LogInformation($"Parsed {holidays.Count} holidays from ICS file.");
                    }
                    else
                    {
                        _logger.LogWarning("Sri Lanka holidays ICS file could not be found. Defaulting to empty holidays list.");
                    }

                    // Retrieve all active GN Officers (linked to account with Status = "Active")
                    var activeGnNics = await context.Accounts
                        .Where(a => a.Status == "Active" && a.GnOfficer != null)
                        .Select(a => a.Nic)
                        .ToListAsync();

                    _logger.LogInformation($"Found {activeGnNics.Count} active GN Officers.");

                    var weeklySchedule = new Dictionary<DayOfWeek, string>
                    {
                        { DayOfWeek.Monday, "DIVISIONAL_OFFICE" },
                        { DayOfWeek.Tuesday, "GN_OFFICE" },
                        { DayOfWeek.Wednesday, "FIELD_VISIT" },
                        { DayOfWeek.Thursday, "GN_OFFICE" },
                        { DayOfWeek.Friday, "FIELD_VISIT" },
                        { DayOfWeek.Saturday, "GN_OFFICE" },
                        { DayOfWeek.Sunday, "WEEKEND_HOLIDAY" }
                    };

                    int generatedCount = 0;

                    foreach (var nic in activeGnNics)
                    {
                        // Check existing availabilities in range [today, oneMonthAhead] to prevent duplicate key violations or overwriting
                        var existingDates = await context.GnAvailabilities
                            .Where(a => a.GnOfficerNic == nic && a.AvailableDate >= today && a.AvailableDate <= oneMonthAhead)
                            .Select(a => a.AvailableDate)
                            .ToListAsync();

                        var existingDatesSet = new HashSet<DateOnly>(existingDates);

                        for (var date = today; date <= oneMonthAhead; date = date.AddDays(1))
                        {
                            if (!existingDatesSet.Contains(date))
                            {
                                // Determine the status and times based on weekly schedule and public holidays
                                string status;
                                TimeSpan? startTime = null;
                                TimeSpan? endTime = null;

                                if (holidays.Contains(date))
                                {
                                    status = "HOLIDAY";
                                }
                                else if (date.DayOfWeek == DayOfWeek.Sunday)
                                {
                                    status = "WEEKEND_HOLIDAY";
                                }
                                else
                                {
                                    status = weeklySchedule[date.DayOfWeek];
                                    startTime = new TimeSpan(8, 30, 0); // 08:30 AM
                                    endTime = new TimeSpan(16, 15, 0);  // 04:15 PM
                                }

                                var newAvail = new GnAvailability
                                {
                                    GnOfficerNic = nic,
                                    AvailableDate = date,
                                    Status = status,
                                    StartTime = startTime,
                                    EndTime = endTime,
                                    Notes = "Automatically generated availability.",
                                    CreatedAt = DateTime.UtcNow,
                                    UpdatedAt = DateTime.UtcNow
                                };

                                context.GnAvailabilities.Add(newAvail);
                                generatedCount++;
                            }
                        }
                    }

                    if (generatedCount > 0)
                    {
                        await context.SaveChangesAsync();
                        _logger.LogInformation($"Successfully generated {generatedCount} availability records for active GN Officers.");
                    }
                    else
                    {
                        _logger.LogInformation("Availability is already up-to-date. No records generated.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during GN availability generation task.");
                }
            }
        }
    }
}
