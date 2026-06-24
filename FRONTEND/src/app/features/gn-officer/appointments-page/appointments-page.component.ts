import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppointmentService } from '../../../core/services/appointment.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-appointments-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './appointments-page.component.html',
  styleUrl: './appointments-page.component.scss'
})
export class AppointmentsPageComponent implements OnInit, OnDestroy {
  allAppointments: any[] = [];
  gnOfficerNic: string = '';
  filterStatus: string = 'ALL';
  
  // Alert Modal
  showAlertModal: boolean = false;
  alertMessage: string = '';
  alertType: 'success' | 'error' = 'success';

  // Cancel Modal
  showCancelModal: boolean = false;
  appointmentIdToCancel: number | null = null;
  cancelReasonType: string = '';
  cancellationReason: string = '';

  // View Cancelled Modal
  showViewModal: boolean = false;
  selectedAppointment: any = null;

  // Reschedule Modal
  showRescheduleModal: boolean = false;
  appointmentIdToReschedule: number | null = null;
  rescheduleDate: string = '';
  rescheduleStartTime: string = '';
  rescheduleEndTime: string = '';
  availableSlots: any[] = [];
  selectedSlotId: string | null = null;
  todayDate: string = '';
  maxDate: string = '';
  noSlotsReasonMsg: string = '';
  
  private destroy$ = new Subject<void>();

  constructor(private appointmentService: AppointmentService) {}

  ngOnInit(): void {
    this.gnOfficerNic = sessionStorage.getItem('userNic') || '';
    const today = new Date();
    this.todayDate = today.toISOString().split('T')[0];
    
    // Calculate max date (exactly 1 month from today)
    const max = new Date();
    max.setMonth(max.getMonth() + 1);
    this.maxDate = max.toISOString().split('T')[0];

    this.loadAppointments();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadAppointments() {
    // Load a broad range for list
    const start = new Date();
    start.setMonth(start.getMonth() - 1); // 1 month ago
    const end = new Date();
    end.setMonth(end.getMonth() + 3); // next 3 months

    this.appointmentService.getGnAppointments(this.gnOfficerNic, start, end)
      .pipe(takeUntil(this.destroy$))
      .subscribe(apps => {
        this.allAppointments = apps.map(app => ({
          ...app,
          id: app.appointmentId || app.id,
          time: `${app.startTime} - ${app.endTime}`,
          service: app.serviceType || app.service,
          citizenName: app.citizenName, // Now accurately populated from backend
          citizenNic: app.citizenNic,
          date: app.appointmentDate || app.date
        }));
      });
  }

  get filteredAppointments() {
    if (this.filterStatus === 'ALL') {
      return this.allAppointments;
    }
    if (this.filterStatus === 'CANCELLED') {
      return this.allAppointments.filter(app => app.status === 'CANCELLED_BY_CITIZEN' || app.status === 'CANCELLED_BY_GN');
    }
    return this.allAppointments.filter(app => app.status === this.filterStatus);
  }

  setFilter(status: string) {
    this.filterStatus = status;
  }

  markAsFinished(id: number) {
    this.appointmentService.updateGnAppointmentStatus(id, 'COMPLETED').subscribe({
      next: () => {
        this.triggerAlert('Appointment marked as COMPLETED.', 'success');
        this.loadAppointments();
      },
      error: (err) => {
        this.triggerAlert(err.error?.Message || 'Failed to complete appointment.', 'error');
      }
    });
  }

  // --- Modals ---
  triggerAlert(message: string, type: 'success' | 'error' = 'success') {
    this.alertMessage = message;
    this.alertType = type;
    this.showAlertModal = true;
  }
  closeAlert() { this.showAlertModal = false; }

  // Cancel Logic
  openCancelModal(id: number) {
    this.appointmentIdToCancel = id;
    this.cancelReasonType = '';
    this.cancellationReason = '';
    this.showCancelModal = true;
  }
  closeCancelModal() {
    this.showCancelModal = false;
    this.appointmentIdToCancel = null;
    this.cancelReasonType = '';
    this.cancellationReason = '';
  }
  confirmCancel() {
    let finalReason = this.cancelReasonType;
    if (this.cancelReasonType === 'Other') {
      finalReason = this.cancellationReason.trim();
    }
    
    if (this.appointmentIdToCancel && finalReason) {
      this.appointmentService.cancelGnAppointment(this.appointmentIdToCancel, finalReason).subscribe({
        next: () => {
          this.triggerAlert('Appointment cancelled.', 'success');
          this.loadAppointments();
          this.closeCancelModal();
        },
        error: (err) => {
          this.triggerAlert(err.error?.Message || 'Failed to cancel.', 'error');
        }
      });
    } else {
       this.triggerAlert('Cancellation reason is required.', 'error');
    }
  }

  // Reschedule Logic
  openRescheduleModal(id: number) {
    this.appointmentIdToReschedule = id;
    this.rescheduleDate = '';
    this.rescheduleStartTime = '';
    this.rescheduleEndTime = '';
    this.availableSlots = [];
    this.selectedSlotId = null;
    this.showRescheduleModal = true;
  }
  
  onRescheduleDateChange() {
    this.availableSlots = [];
    this.selectedSlotId = null;
    this.rescheduleStartTime = '';
    this.rescheduleEndTime = '';
    this.noSlotsReasonMsg = '';
    
    if (this.rescheduleDate && this.gnOfficerNic) {
      const dateObj = new Date(this.rescheduleDate);
      
      // 1. Check ICS Holidays first
      const isHoliday = this.appointmentService.isHoliday(dateObj);
      if (isHoliday) {
        this.noSlotsReasonMsg = 'This is a government holiday';
        return; 
      }

      // 2. Check Weekends
      const dayOfWeek = dateObj.getDay();
      if (dayOfWeek === 0 || dayOfWeek === 6) {
         this.noSlotsReasonMsg = 'Weekend holiday, no appointments available';
         return;
      }

      // Check availability status for reason messages
      this.appointmentService.getGnAvailabilityDay(this.gnOfficerNic, dateObj).subscribe({
        next: (avails) => {
          let reasonFound = false;
          if (avails && avails.length > 0) {
            const status = avails[0].status || avails[0].Status;
            if (status === 'DIVISIONAL_OFFICE') this.noSlotsReasonMsg = 'You are on duty at DS Office on this date';
            else if (status === 'LEAVE') this.noSlotsReasonMsg = 'GN is on leave on this date';
            else if (status === 'FIELD_VISIT') this.noSlotsReasonMsg = 'GN is on a field visit on this date';
            else if (status === 'HOLIDAY') this.noSlotsReasonMsg = 'This is a government holiday';
            else if (status === 'WEEKEND_HOLIDAY') this.noSlotsReasonMsg = 'Weekend holiday, no appointments available';
            
            if (status && status !== 'GN_OFFICE') reasonFound = true;
          }
          
          this.appointmentService.getAvailableSlots(this.gnOfficerNic, dateObj).subscribe({
            next: (slots: any[]) => {
              this.availableSlots = slots.map(s => {
                const st = (s.startTime || '').substring(0, 5);
                const et = (s.endTime || '').substring(0, 5);
                return {
                    id: s.startTime,
                    startTime: st,
                    endTime: et,
                    status: s.isBooked ? 'booked' : 'available'
                };
              });
              
              if (this.availableSlots.length === 0 && !reasonFound) {
                 this.noSlotsReasonMsg = 'All slots are fully booked for this date';
              } else if (this.availableSlots.length > 0) {
                 this.noSlotsReasonMsg = ''; // clear if slots actually available
              }
            },
            error: () => {
              this.triggerAlert('Failed to load slots for selected date.', 'error');
            }
          });
        }
      });
    }
  }
  
  selectSlot(slot: any) {
    if (slot.status === 'available') {
      this.selectedSlotId = slot.id;
      this.rescheduleStartTime = slot.startTime;
      this.rescheduleEndTime = slot.endTime;
    }
  }

  closeRescheduleModal() {
    this.showRescheduleModal = false;
    this.appointmentIdToReschedule = null;
    this.availableSlots = [];
    this.selectedSlotId = null;
  }
  confirmReschedule() {
    if (this.appointmentIdToReschedule && this.rescheduleDate && this.rescheduleStartTime && this.rescheduleEndTime) {
      const dateObj = new Date(this.rescheduleDate);
      this.appointmentService.rescheduleGnAppointment(
        this.appointmentIdToReschedule, 
        dateObj, 
        this.rescheduleStartTime, 
        this.rescheduleEndTime
      ).subscribe({
        next: () => {
          this.triggerAlert('Appointment rescheduled.', 'success');
          this.loadAppointments();
          this.closeRescheduleModal();
        },
        error: (err) => {
          this.triggerAlert(err.error?.Message || 'Failed to reschedule.', 'error');
        }
      });
    } else {
       this.triggerAlert('All fields are required.', 'error');
    }
  }
}
