using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EGN_Service_Management.Entity;

namespace EGN_Service_Management.IServices
{
    public interface IAppointmentService
    {
        // For GN Module
        Task<IEnumerable<GnAvailability>> GetGnAvailabilityAsync(string gnOfficerNic, DateTime startDate, DateTime endDate);
        Task<GnAvailability> SetGnAvailabilityAsync(GnAvailability availability);
        Task DeleteGnAvailabilityAsync(int availabilityId);
        
        Task<IEnumerable<object>> GetGnAppointmentsAsync(string gnOfficerNic, DateTime startDate, DateTime endDate);
        Task<Appointment> UpdateAppointmentStatusAsync(int appointmentId, string status, string? cancellationReason);

        // For Citizen Module
        Task<IEnumerable<dynamic>> GetAvailableSlotsAsync(string gnOfficerNic, DateTime targetDate);
        Task<Appointment> BookAppointmentAsync(Appointment appointment);
        Task<IEnumerable<object>> GetCitizenAppointmentsAsync(string citizenNic);
        Task<Appointment> CancelCitizenAppointmentAsync(int appointmentId, string citizenNic, string cancellationReason);
    }
}
