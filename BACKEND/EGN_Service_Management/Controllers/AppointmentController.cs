using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using EGN_Service_Management.Entity;
using EGN_Service_Management.IServices;

namespace EGN_Service_Management.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentController : ControllerBase
    {
        private readonly IAppointmentService _appointmentService;
        private readonly INotificationService _notificationService;
        private readonly EGN_Service_Management.Database.EgnDbContext _context;

        public AppointmentController(
            IAppointmentService appointmentService,
            INotificationService notificationService,
            EGN_Service_Management.Database.EgnDbContext context)
        {
            _appointmentService = appointmentService;
            _notificationService = notificationService;
            _context = context;
        }

        // GN ENDPOINTS
        [HttpGet("gn/availability/{gnOfficerNic}")]
        public async Task<IActionResult> GetGnAvailability(string gnOfficerNic, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var availabilities = await _appointmentService.GetGnAvailabilityAsync(gnOfficerNic, startDate, endDate);
                return Ok(availabilities);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("gn/availability")]
        public async Task<IActionResult> SetGnAvailability([FromBody] GnAvailability availability)
        {
            try
            {
                var result = await _appointmentService.SetGnAvailabilityAsync(availability);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("gn/availability/{id}")]
        public async Task<IActionResult> DeleteGnAvailability(int id)
        {
            try
            {
                await _appointmentService.DeleteGnAvailabilityAsync(id);
                return Ok(new { Message = "Availability removed successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("gn/appointments/{gnOfficerNic}")]
        public async Task<IActionResult> GetGnAppointments(string gnOfficerNic, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var appointments = await _appointmentService.GetGnAppointmentsAsync(gnOfficerNic, startDate, endDate);
                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("gn/appointments/{id}/status")]
        public async Task<IActionResult> UpdateAppointmentStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var result = await _appointmentService.UpdateAppointmentStatusAsync(id, request.Status, request.CancellationReason);

                var appointment = await _context.Appointments.FindAsync(id);
                if (appointment != null)
                {
                    await _notificationService.SendNotificationAsync(
                        recipientNic: appointment.CitizenNic,
                        recipientRole: "Citizen",
                        type: "appointment",
                        title: $"Appointment {request.Status}",
                        message: $"Your appointment on {appointment.AppointmentDate:yyyy-MM-dd} has been {request.Status.ToLower()}.",
                        relatedEntityType: "Appointment",
                        relatedEntityId: appointment.AppointmentId.ToString(),
                        actionUrl: "/citizen/appointments",
                        priority: "Medium",
                        createdBy: appointment.GnOfficerNic
                    );
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // CITIZEN ENDPOINTS
        [HttpGet("citizen/slots/{gnOfficerNic}")]
        public async Task<IActionResult> GetAvailableSlots(string gnOfficerNic, [FromQuery] DateTime targetDate)
        {
            try
            {
                var slots = await _appointmentService.GetAvailableSlotsAsync(gnOfficerNic, targetDate);
                return Ok(slots);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("citizen/book")]
        public async Task<IActionResult> BookAppointment([FromBody] Appointment appointment)
        {
            try
            {
                var result = await _appointmentService.BookAppointmentAsync(appointment);

                await _notificationService.SendNotificationAsync(
                    recipientNic: appointment.GnOfficerNic,
                    recipientRole: "GNOfficer",
                    type: "appointment",
                    title: "New Appointment Booked",
                    message: $"Citizen {appointment.CitizenNic} has booked an appointment for {appointment.AppointmentDate:yyyy-MM-dd}.",
                    relatedEntityType: "Appointment",
                    relatedEntityId: appointment.AppointmentId.ToString(),
                    actionUrl: "/gn-officer/appointments",
                    priority: "Medium",
                    createdBy: appointment.CitizenNic
                );

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("citizen/appointments/{citizenNic}")]
        public async Task<IActionResult> GetCitizenAppointments(string citizenNic)
        {
            try
            {
                var appointments = await _appointmentService.GetCitizenAppointmentsAsync(citizenNic);
                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("citizen/appointments/{id}/cancel")]
        public async Task<IActionResult> CancelCitizenAppointment(int id, [FromBody] CancelRequest request)
        {
            try
            {
                var result = await _appointmentService.CancelCitizenAppointmentAsync(id, request.CitizenNic, request.CancellationReason);

                var appointment = await _context.Appointments.FindAsync(id);
                if (appointment != null)
                {
                    await _notificationService.SendNotificationAsync(
                        recipientNic: appointment.GnOfficerNic,
                        recipientRole: "GNOfficer",
                        type: "appointment",
                        title: "Appointment Cancelled",
                        message: $"Citizen {request.CitizenNic} has cancelled their appointment on {appointment.AppointmentDate:yyyy-MM-dd}.",
                        relatedEntityType: "Appointment",
                        relatedEntityId: appointment.AppointmentId.ToString(),
                        actionUrl: "/gn-officer/appointments",
                        priority: "Medium",
                        createdBy: request.CitizenNic
                    );
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // --- NEW SIMPLIFIED GN APPOINTMENT ENDPOINTS ---
        [HttpGet("/api/appointments/gn/list")]
        public async Task<IActionResult> GetGnAppointmentsList([FromQuery] string gnOfficerNic, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            try
            {
                var appointments = await _appointmentService.GetGnAppointmentsAsync(gnOfficerNic, startDate, endDate);
                return Ok(appointments);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("/api/appointments/cancel")]
        public async Task<IActionResult> CancelGnAppointment([FromBody] GnCancelRequest request)
        {
            try
            {
                var result = await _appointmentService.UpdateAppointmentStatusAsync(request.AppointmentId, "CANCELLED_BY_GN", request.CancellationReason);

                var appointment = await _context.Appointments.FindAsync(request.AppointmentId);
                if (appointment != null)
                {
                    await _notificationService.SendNotificationAsync(
                        recipientNic: appointment.CitizenNic,
                        recipientRole: "Citizen",
                        type: "appointment",
                        title: "Appointment Cancelled",
                        message: $"Your appointment on {appointment.AppointmentDate:yyyy-MM-dd} has been cancelled by the GN Officer.",
                        relatedEntityType: "Appointment",
                        relatedEntityId: appointment.AppointmentId.ToString(),
                        actionUrl: "/citizen/appointments",
                        priority: "High",
                        createdBy: appointment.GnOfficerNic
                    );
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("/api/appointments/reschedule")]
        public async Task<IActionResult> RescheduleGnAppointment([FromBody] RescheduleRequest request)
        {
            try
            {
                var oldAppt = await _context.Appointments.FindAsync(request.AppointmentId);
                if (oldAppt == null) throw new Exception("Appointment not found");

                // Update existing appointment
                oldAppt.AppointmentDate = request.NewDate;
                oldAppt.StartTime = request.NewStartTime;
                oldAppt.EndTime = request.NewEndTime;
                oldAppt.Status = "RESCHEDULED";
                oldAppt.UpdatedAt = DateTime.UtcNow;

                _context.Appointments.Update(oldAppt);
                await _context.SaveChangesAsync();

                await _notificationService.SendNotificationAsync(
                    recipientNic: oldAppt.CitizenNic,
                    recipientRole: "Citizen",
                    type: "appointment",
                    title: "Appointment Rescheduled",
                    message: $"Your appointment has been rescheduled to {oldAppt.AppointmentDate:yyyy-MM-dd} at {oldAppt.StartTime}.",
                    relatedEntityType: "Appointment",
                    relatedEntityId: oldAppt.AppointmentId.ToString(),
                    actionUrl: "/citizen/appointments",
                    priority: "High",
                    createdBy: oldAppt.GnOfficerNic
                );

                return Ok(oldAppt);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // --- NEW SIMPLIFIED GN AVAILABILITY ENDPOINTS ---
        [HttpGet("/api/availability/month")]
        public async Task<IActionResult> GetAvailabilityMonth([FromQuery] string gnOfficerNic, [FromQuery] DateTime startDate, [FromQuery] DateTime endDate)
        {
            return await GetGnAvailability(gnOfficerNic, startDate, endDate);
        }

        [HttpGet("/api/availability/day")]
        public async Task<IActionResult> GetAvailabilityDay([FromQuery] string gnOfficerNic, [FromQuery] DateTime date)
        {
            return await GetGnAvailability(gnOfficerNic, date, date);
        }

        [HttpPost("/api/availability/save")]
        public async Task<IActionResult> SaveAvailabilityV2([FromBody] GnAvailability availability)
        {
            return await SetGnAvailability(availability);
        }

        [HttpPut("/api/availability/update")]
        public async Task<IActionResult> UpdateAvailabilityV2([FromBody] GnAvailability availability)
        {
            return await SetGnAvailability(availability);
        }
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? CancellationReason { get; set; }
    }

    public class CancelRequest
    {
        public string CitizenNic { get; set; } = string.Empty;
        public string CancellationReason { get; set; } = string.Empty;
    }

    public class GnCancelRequest
    {
        public int AppointmentId { get; set; }
        public string CancellationReason { get; set; } = string.Empty;
    }

    public class RescheduleRequest
    {
        public int AppointmentId { get; set; }
        public DateOnly NewDate { get; set; }
        public TimeSpan NewStartTime { get; set; }
        public TimeSpan NewEndTime { get; set; }
    }
}
