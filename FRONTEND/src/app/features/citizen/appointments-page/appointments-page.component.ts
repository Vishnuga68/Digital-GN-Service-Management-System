import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AppointmentService, TimeSlot, Holiday } from '../../../core/services/appointment.service';
import { Subject, interval } from 'rxjs';
import { takeUntil } from 'rxjs/operators';
import { HttpClient } from '@angular/common/http';

import { FullCalendarModule, FullCalendarComponent } from '@fullcalendar/angular';
import { CalendarOptions, EventInput } from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';
import interactionPlugin from '@fullcalendar/interaction';

@Component({
  selector: 'app-appointments-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule, FullCalendarModule],
  templateUrl: './appointments-page.component.html',
  styleUrl: './appointments-page.component.scss'
})
export class AppointmentsPageComponent implements OnInit, OnDestroy {
  @ViewChild('calendar') calendarComponent!: FullCalendarComponent;

  selectedDate: Date = new Date();
  slots: TimeSlot[] = [];
  userAppointments: any[] = [];
  selectedSlotId: string | null = null;
  selectedService: string = '';
  selectedSlotTime: string = '';
  
  isHoliday: boolean = false;
  holidayDetail: Holiday | undefined;
  
  showCancelModal: boolean = false;
  appointmentIdToCancel: string | null = null;
  
  // Alert Modal State
  showAlertModal: boolean = false;
  alertMessage: string = '';
  alertType: 'success' | 'error' = 'success';
  
  services: any[] = [];

  // Popover State
  showPopover: boolean = false;
  popoverContent: string = '';
  popoverLabel: string = '';
  popoverType: 'holiday' | 'appointment' = 'holiday';
  popoverPosition = { top: '0px', left: '0px' };

  private citizenNic: string = '';
  private gnOfficerNic: string = '';

  calendarOptions: CalendarOptions = {
    initialView: 'dayGridMonth',
    plugins: [dayGridPlugin, interactionPlugin],
    headerToolbar: {
      left: 'prev',
      center: 'title',
      right: 'next'
    },
    height: 'auto',
    contentHeight: 'auto',
    fixedWeekCount: false,
    themeSystem: 'standard',
    selectable: true,
    unselectAuto: false,
    selectMirror: true,
    events: [],
    
    dayCellClassNames: (arg: any) => {
      const classes = [];
      const date = arg.date;
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      
      const oneMonthLater = new Date();
      oneMonthLater.setMonth(today.getMonth() + 1);

      const isHoliday = this.appointmentService.isHoliday(date);
      if (isHoliday) classes.push('fc-holiday-cell');
      
      if (date < today || date > oneMonthLater || date.getDay() === 0) {
        classes.push('fc-date-restricted');
      }

      if (this.selectedDate && date.toDateString() === this.selectedDate.toDateString()) {
        classes.push('fc-day-selected');
      }

      const hasAppointment = this.userAppointments.some(app => {
        const appDate = new Date(app.appointmentDate);
        return appDate.toDateString() === date.toDateString();
      });
      if (hasAppointment) {
        classes.push('fc-day-appointment');
      }
      
      return classes;
    },

    dayCellContent: (arg: any) => {
      return { html: `<div class="fc-daygrid-day-number">${arg.dayNumberText}</div>` };
    },

    selectAllow: (selectInfo: any) => {
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      const oneMonthLater = new Date();
      oneMonthLater.setMonth(today.getMonth() + 1);
      
      const isOutRange = selectInfo.start < today || selectInfo.start > oneMonthLater;
      return !this.appointmentService.isHoliday(selectInfo.start) && !isOutRange && selectInfo.start.getDay() !== 0;
    },

    dateClick: (arg: any) => {
      const date = arg.date;
      const holiday = this.appointmentService.isHoliday(date);
      const app = this.userAppointments.find(a => new Date(a.appointmentDate).toDateString() === date.toDateString());
      
      // Allow selecting any date so the right panel shows the correct restriction message
      this.onDateSelect(date);
      
      if (holiday || app) {
        const rect = arg.dayEl.getBoundingClientRect();
        this.popoverPosition = {
          top: `${rect.top + window.scrollY - 5}px`, 
          left: `${rect.left + window.scrollX + rect.width / 2}px`
        };
        
        if (holiday) {
          this.popoverType = 'holiday';
          this.popoverLabel = 'Official Holiday';
          this.popoverContent = holiday.summary;
        } else if (app) {
          this.popoverType = 'appointment';
          this.popoverLabel = 'Scheduled Appointment';
          this.popoverContent = app.serviceType;
        }
        this.showPopover = true;
      } else {
        this.showPopover = false;
      }
    }
  };

  private destroy$ = new Subject<void>();

  constructor(private appointmentService: AppointmentService, private http: HttpClient) {}

  ngOnInit(): void {
    this.http.get<any[]>('http://localhost:5058/api/services/active').subscribe({
        next: (svcs) => {
            this.services = svcs;
        }
    });

    this.citizenNic = sessionStorage.getItem('userNic') || '';
    const userProfileStr = sessionStorage.getItem('userProfile');
    
    if (userProfileStr) {
        const userProfile = JSON.parse(userProfileStr);
        const gnDivisionId = userProfile.gnDivisionId || userProfile.GnDivisionId;
        
        // Fetch GN Officer for this division
        if (gnDivisionId) {
            this.http.get<any[]>('http://localhost:5058/api/GnOfficer').subscribe({
                next: (officers) => {
                    const officer = officers.find(o => (o.gnDivisionId || o.GnDivisionId) === gnDivisionId);
                    if (officer) {
                        this.gnOfficerNic = officer.nic || officer.Nic;
                        this.loadCitizenAppointments();
                        if (!this.isDateRestricted) {
                            this.loadSlotsForDate(this.selectedDate);
                        }
                    } else {
                        this.loadCitizenAppointments();
                    }
                },
                error: () => this.loadCitizenAppointments()
            });
        } else {
            this.loadCitizenAppointments();
        }
    } else {
        this.loadCitizenAppointments();
    }

    this.appointmentService.selectedDate$
      .pipe(takeUntil(this.destroy$))
      .subscribe(date => {
        this.selectedDate = date;
        this.checkDateRestrictions(date);
        if (!this.isDateRestricted && this.gnOfficerNic) {
            this.loadSlotsForDate(date);
        } else {
            this.slots = [];
        }
      });

    this.appointmentService.holidays$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.calendarOptions = { ...this.calendarOptions };
      });
  }

  loadCitizenAppointments() {
      if (!this.citizenNic) return;
      this.appointmentService.getCitizenAppointments(this.citizenNic).subscribe({
          next: (apps) => {
              this.userAppointments = apps;
              this.calendarOptions = { ...this.calendarOptions };
          }
      });
  }

  gnStatus: string = '';

  loadSlotsForDate(date: Date) {
      if (!this.gnOfficerNic) return;
      
      // Fetch availability status first
      this.appointmentService.getGnAvailabilityDay(this.gnOfficerNic, date).subscribe(avail => {
          if (avail && avail.length > 0) {
              this.gnStatus = avail[0].status || avail[0].Status;
          } else {
              this.gnStatus = 'NONE';
          }
          
          this.appointmentService.getAvailableSlots(this.gnOfficerNic, date).subscribe({
              next: (slots: any[]) => {
                  this.slots = slots.map(s => {
                      const st = (s.startTime || '').substring(0, 5);
                      const et = (s.endTime || '').substring(0, 5);
                      return {
                          id: s.startTime,
                          startTime: st,
                          endTime: et,
                          status: s.isBooked ? 'booked' : 'available'
                      };
                  });
              }
          });
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  isPastDate: boolean = false;
  isOutRange: boolean = false;
  isLeaveDay: boolean = false;

  get isDateRestricted(): boolean {
      return this.isHoliday || this.isPastDate || this.isOutRange;
  }

  get restrictionMessage(): string {
      if (this.isPastDate) return 'Selected date is in the past.';
      if (this.isOutRange) return 'Cannot book appointments more than 1 month in advance.';
      if (this.isHoliday) return 'Selected date is an official holiday.';
      
      if (this.gnStatus === 'FIELD_VISIT') return 'GN Officer is on a field visit today.';
      if (this.gnStatus === 'DIVISIONAL_OFFICE') return 'GN Officer is at the Divisional Secretariat today.';
      if (this.gnStatus === 'LEAVE') return 'GN Officer is on leave today.';
      if (this.gnStatus === 'WEEKEND_HOLIDAY') return 'GN Officer is unavailable due to a weekend holiday.';
      
      return 'No slots available'; // fallback
  }

  onDateSelect(date: Date) {
    this.appointmentService.setSelectedDate(date);
    this.selectedSlotId = null;
    this.selectedSlotTime = '';
    this.checkDateRestrictions(date);
  }

  checkDateRestrictions(date: Date = this.selectedDate) {
    this.holidayDetail = this.appointmentService.isHoliday(date);
    this.isHoliday = !!this.holidayDetail;

    const today = new Date();
    today.setHours(0, 0, 0, 0);
    this.isPastDate = date < today;

    const oneMonthLater = new Date();
    oneMonthLater.setMonth(today.getMonth() + 1);
    this.isOutRange = date > oneMonthLater;
  }

  closePopover() {
    this.showPopover = false;
  }

  selectSlot(slot: TimeSlot) {
    if (slot.status === 'available') {
      this.selectedSlotId = slot.id || null;
      this.selectedSlotTime = `${slot.startTime} - ${slot.endTime}`;
    }
  }

  onBook() {
    if (this.selectedSlotId && this.selectedService && this.gnOfficerNic) {
      const slot = this.slots.find(s => s.id === this.selectedSlotId);
      if (slot) {
          this.appointmentService.bookAppointment(
              this.citizenNic,
              this.gnOfficerNic,
              this.selectedDate,
              slot.startTime,
              slot.endTime,
              this.selectedService
          ).subscribe({
            next: (success) => {
              this.triggerAlert('Appointment booked successfully!', 'success');
              this.selectedSlotId = null;
              this.selectedService = '';
              this.selectedSlotTime = '';
              this.loadSlotsForDate(this.selectedDate);
              this.loadCitizenAppointments();
            },
            error: (err) => {
              this.triggerAlert(err.error?.message || 'This slot might have been just booked by someone else.', 'error');
            }
          });
      }
    }
  }

  triggerAlert(message: string, type: 'success' | 'error' = 'success') {
    this.alertMessage = message;
    this.alertType = type;
    this.showAlertModal = true;
  }

  closeAlert() {
    this.showAlertModal = false;
  }

  confirmCancel(id: string) {
    this.appointmentIdToCancel = id;
    this.showCancelModal = true;
  }

  closeModal() {
    this.showCancelModal = false;
    this.appointmentIdToCancel = null;
  }

  confirmCancellation() {
    if (this.appointmentIdToCancel) {
      this.appointmentService.cancelCitizenAppointment(Number(this.appointmentIdToCancel), this.citizenNic, 'Cancelled by citizen').subscribe({
        next: () => {
          this.triggerAlert('Appointment cancelled.', 'success');
          this.loadCitizenAppointments();
          this.loadSlotsForDate(this.selectedDate);
          this.closeModal();
        },
        error: (err) => {
            this.triggerAlert(err.error?.message || 'Failed to cancel appointment', 'error');
            this.closeModal();
        }
      });
    }
  }

  onCancel(id: string) {
    // Deprecated for custom modal: confirmCancel
  }

  getSelectedSlotTime(): string {
    return this.selectedSlotTime || 'Not selected';
  }
}
