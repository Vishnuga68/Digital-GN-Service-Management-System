using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;
using EGN_Service_Management.Database;

namespace EGN_Service_Management.Services
{
    public class AppointmentService : IAppointmentService
    {
        private readonly EgnDbContext _context;

        public AppointmentService(EgnDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<GnAvailability>> GetGnAvailabilityAsync(string gnOfficerNic, DateTime startDate, DateTime endDate)
        {
            var start = DateOnly.FromDateTime(startDate);
            var end = DateOnly.FromDateTime(endDate);
            return await _context.GnAvailabilities
                .Where(a => a.GnOfficerNic == gnOfficerNic && a.AvailableDate >= start && a.AvailableDate <= end)
                .OrderBy(a => a.AvailableDate)
                .ToListAsync();
        }

        public async Task<GnAvailability> SetGnAvailabilityAsync(GnAvailability availability)
        {
            if (availability.AvailableDate.DayOfWeek == DayOfWeek.Sunday)
            {
                throw new Exception("Cannot set availability on this day");
            }

            // Check if date is ICS holiday is handled by frontend.

            var existing = await _context.GnAvailabilities
                .FirstOrDefaultAsync(a => a.GnOfficerNic == availability.GnOfficerNic && a.AvailableDate == availability.AvailableDate);

            if (existing != null)
            {
                existing.Status = availability.Status;
                existing.StartTime = availability.StartTime;
                existing.EndTime = availability.EndTime;
                existing.Notes = availability.Notes;
                existing.UpdatedAt = DateTime.UtcNow;
                _context.GnAvailabilities.Update(existing);
            }
            else
            {
                availability.CreatedAt = DateTime.UtcNow;
                availability.UpdatedAt = DateTime.UtcNow;
                _context.GnAvailabilities.Add(availability);
            }

            await _context.SaveChangesAsync();
            return existing ?? availability;
        }

        public async Task DeleteGnAvailabilityAsync(int availabilityId)
        {
            var existing = await _context.GnAvailabilities.FindAsync(availabilityId);
            if (existing != null)
            {
                _context.GnAvailabilities.Remove(existing);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<object>> GetGnAppointmentsAsync(string gnOfficerNic, DateTime startDate, DateTime endDate)
        {
            var start = DateOnly.FromDateTime(startDate);
            var end = DateOnly.FromDateTime(endDate);
            var appointments = await (from a in _context.Appointments
                                      join c in _context.CitizenMasters on a.CitizenNic equals c.Nic into cGroup
                                      from c in cGroup.DefaultIfEmpty()
                                      where a.GnOfficerNic == gnOfficerNic && a.AppointmentDate >= start && a.AppointmentDate <= end
                                      orderby a.AppointmentDate, a.StartTime
                                      select new
                                      {
                                          AppointmentId = a.AppointmentId,
                                          CitizenName = c != null ? c.FullName : "Unknown Citizen",
                                          CitizenNic = a.CitizenNic,
                                          ServiceType = a.ServiceType,
                                          AppointmentDate = a.AppointmentDate.ToString("yyyy-MM-dd"),
                                          StartTime = a.StartTime.ToString(@"hh\:mm"),
                                          EndTime = a.EndTime.ToString(@"hh\:mm"),
                                          Status = a.Status,
                                          CancellationReason = a.CancellationReason,
                                          UpdatedAt = a.UpdatedAt
                                      }).ToListAsync();
            return appointments;
        }

        public async Task<Appointment> UpdateAppointmentStatusAsync(int appointmentId, string status, string? cancellationReason)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null) throw new Exception("Appointment not found");

            appointment.Status = status;
            if (status.Contains("CANCELLED", StringComparison.OrdinalIgnoreCase) || status.Contains("Cancelled", StringComparison.OrdinalIgnoreCase) || status == "Rejected")
            {
                appointment.CancellationReason = cancellationReason;
            }
            appointment.UpdatedAt = DateTime.UtcNow;
            
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();
            return appointment;
        }

        public async Task<IEnumerable<dynamic>> GetAvailableSlotsAsync(string gnOfficerNic, DateTime targetDate)
        {
            // Check if Sunday
            if (targetDate.DayOfWeek == DayOfWeek.Sunday)
            {
                return new List<dynamic>();
            }

            var target = DateOnly.FromDateTime(targetDate);
            
            // Holidays checking is handled by frontend

            var availability = await _context.GnAvailabilities
                .FirstOrDefaultAsync(a => a.GnOfficerNic == gnOfficerNic && a.AvailableDate == target);

            TimeSpan startTime;
            TimeSpan endTime;

            if (availability != null && availability.Status == "GN_OFFICE" && availability.StartTime.HasValue && availability.EndTime.HasValue)
            {
                startTime = availability.StartTime.Value;
                endTime = availability.EndTime.Value;
            }
            else
            {
                // Else: return empty list (No explicit GN_OFFICE availability)
                return new List<dynamic>();
            }

            // Generate slots every 30 minutes
            var slots = new List<dynamic>();
            var current = startTime;

            var existingAppointments = await _context.Appointments
                .Where(a => a.GnOfficerNic == gnOfficerNic && a.AppointmentDate == target && a.Status != "Cancelled" && a.Status != "Rejected" && a.Status != "CANCELLED_BY_GN")
                .ToListAsync();

            while (current < endTime)
            {
                var slotEnd = current.Add(TimeSpan.FromMinutes(30));
                if (slotEnd > endTime) break; 

                var isBooked = existingAppointments.Any(a => 
                    (a.StartTime < slotEnd && a.EndTime > current));

                slots.Add(new
                {
                    StartTime = current.ToString(@"hh\:mm"),
                    EndTime = slotEnd.ToString(@"hh\:mm"),
                    IsBooked = isBooked
                });

                current = slotEnd;
            }

            return slots;
        }

        public async Task<Appointment> BookAppointmentAsync(Appointment appointment)
        {
            if (appointment.AppointmentDate.DayOfWeek == DayOfWeek.Sunday)
                throw new Exception("Cannot book appointments on Sunday");

            // ICS Holiday check handled by frontend
            
            // Verify availability
            var availability = await _context.GnAvailabilities
                .FirstOrDefaultAsync(a => a.GnOfficerNic == appointment.GnOfficerNic && a.AvailableDate == appointment.AppointmentDate);

            if (availability != null && availability.Status != "GN_OFFICE") 
                throw new Exception("GN Officer is not available for office appointments on this date");
            
            // Overlapping check
            var overlapping = await _context.Appointments
                .AnyAsync(a => a.GnOfficerNic == appointment.GnOfficerNic && a.AppointmentDate == appointment.AppointmentDate 
                               && a.Status != "Cancelled" && a.Status != "Rejected" && a.Status != "CANCELLED_BY_GN"
                               && a.StartTime < appointment.EndTime && a.EndTime > appointment.StartTime);
                               
            if (overlapping) throw new Exception("This time slot is already booked");

            appointment.Status = "PENDING";
            appointment.CreatedAt = DateTime.UtcNow;
            appointment.UpdatedAt = DateTime.UtcNow;
            
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            return appointment;
        }

        public async Task<IEnumerable<object>> GetCitizenAppointmentsAsync(string citizenNic)
        {
            var appointments = await _context.Appointments
                .Where(a => a.CitizenNic == citizenNic)
                .OrderByDescending(a => a.AppointmentDate).ThenBy(a => a.StartTime)
                .Select(a => new
                {
                    AppointmentId = a.AppointmentId,
                    GnOfficerNic = a.GnOfficerNic, // Front-end might need this for something
                    ServiceType = a.ServiceType,
                    AppointmentDate = a.AppointmentDate.ToString("yyyy-MM-dd"),
                    StartTime = a.StartTime.ToString(@"hh\:mm"),
                    EndTime = a.EndTime.ToString(@"hh\:mm"),
                    Status = a.Status,
                    CancellationReason = a.CancellationReason
                })
                .ToListAsync();
            return appointments;
        }

        public async Task<Appointment> CancelCitizenAppointmentAsync(int appointmentId, string citizenNic, string cancellationReason)
        {
            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment == null || appointment.CitizenNic != citizenNic) throw new Exception("Appointment not found");

            appointment.Status = "CANCELLED_BY_CITIZEN";
            appointment.CancellationReason = cancellationReason;
            appointment.UpdatedAt = DateTime.UtcNow;

            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();
            return appointment;
        }
    }
}
