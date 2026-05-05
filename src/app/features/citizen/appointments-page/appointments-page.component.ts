import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AppointmentService, TimeSlot, Holiday } from '../../../core/services/appointment.service';
import { Subject, interval } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

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
  
  isHoliday: boolean = false;
  holidayDetail: Holiday | undefined;
  
  showCancelModal: boolean = false;
  appointmentIdToCancel: string | null = null;
  
  // Alert Modal State
  showAlertModal: boolean = false;
  alertMessage: string = '';
  alertType: 'success' | 'error' = 'success';
  
  services: string[] = [
    'Residence Verification',
    'Income Certificate',
    'Character Certificate',
    'Land Verification',
    'Elderly Allowance Application',
    'Other Inquiries'
  ];

  // Popover State
  showPopover: boolean = false;
  popoverContent: string = '';
  popoverLabel: string = '';
  popoverType: 'holiday' | 'appointment' = 'holiday';
  popoverPosition = { top: '0px', left: '0px' };

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
    events: [], // Initially empty
    
    // Custom day styling instead of hard validRange to keep dates visible
    dayCellClassNames: (arg: any) => {
      const classes = [];
      const date = arg.date;
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      
      const twoMonthsLater = new Date();
      twoMonthsLater.setMonth(today.getMonth() + 2);

      const isHoliday = this.appointmentService.isHoliday(date);
      if (isHoliday) classes.push('fc-holiday-cell');
      
      // Mark dates outside range
      if (date < today || date > twoMonthsLater) {
        classes.push('fc-date-restricted');
      }

      // Highlight selected date
      if (this.selectedDate && date.toDateString() === this.selectedDate.toDateString()) {
        classes.push('fc-day-selected');
      }

      // Check for user appointments to apply blue indicator
      const hasAppointment = this.userAppointments.some(app => {
        const appDate = new Date(app.date);
        return appDate.toDateString() === date.toDateString();
      });
      if (hasAppointment) {
        classes.push('fc-day-appointment');
      }
      
      return classes;
    },

    // Customize the content of the day cell
    dayCellContent: (arg: any) => {
      return { html: `<div class="fc-daygrid-day-number">${arg.dayNumberText}</div>` };
    },

    // Prevent selection on holidays and outside 2-month range
    selectAllow: (selectInfo: any) => {
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      const twoMonthsLater = new Date();
      twoMonthsLater.setMonth(today.getMonth() + 2);
      
      const isOutRange = selectInfo.start < today || selectInfo.start > twoMonthsLater;
      return !this.appointmentService.isHoliday(selectInfo.start) && !isOutRange;
    },

    // Combined Date Click and Selection for Details/Booking
    dateClick: (arg: any) => {
      const date = arg.date;
      const holiday = this.appointmentService.isHoliday(date);
      const app = this.userAppointments.find(a => new Date(a.date).toDateString() === date.toDateString());
      
      // Select the date for booking slots (even if it's an appointment or just a day)
      // Note: selectAllow already prevents holiday selections if we need that
      if (!holiday) {
        this.onDateSelect(date);
      }
      
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
          this.popoverContent = app.service;
        }
        this.showPopover = true;
      } else {
        this.showPopover = false;
      }
    }
  };

  private destroy$ = new Subject<void>();

  constructor(private appointmentService: AppointmentService) {}

  ngOnInit(): void {
    // Subscribe to selected date changes
    this.appointmentService.selectedDate$
      .pipe(takeUntil(this.destroy$))
      .subscribe(date => {
        this.selectedDate = date;
        this.checkHoliday();
      });

    // Subscribe to holiday changes to refresh calendar data
    this.appointmentService.holidays$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        // Just trigger change detection for the calendar
        this.calendarOptions = { ...this.calendarOptions };
      });

    // Subscribe to slots changes
    this.appointmentService.slots$
      .pipe(takeUntil(this.destroy$))
      .subscribe(slots => {
        this.slots = slots;
      });

    this.appointmentService.currentAppointments$
      .pipe(takeUntil(this.destroy$))
      .subscribe(apps => {
        this.userAppointments = apps;
      });
  }

  // updateCalendarEvents() removed - no longer reflecting appointments on calendar


  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onDateSelect(date: Date) {
    this.appointmentService.setSelectedDate(date);
    this.selectedSlotId = null;
    this.checkHoliday();
  }

  checkHoliday() {
    this.holidayDetail = this.appointmentService.isHoliday(this.selectedDate);
    this.isHoliday = !!this.holidayDetail;
  }

  closePopover() {
    this.showPopover = false;
  }

  selectSlot(slot: TimeSlot) {
    if (slot.status === 'available') {
      this.selectedSlotId = slot.id;
    }
  }

  onBook() {
    if (this.selectedSlotId && this.selectedService) {
      this.appointmentService.bookSlot(this.selectedSlotId, this.selectedService).subscribe(success => {
        if (success) {
          this.triggerAlert('Appointment booked successfully!', 'success');
          this.selectedSlotId = null;
        } else {
          this.triggerAlert('This slot might have been just booked by someone else.', 'error');
        }
      });
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
      this.appointmentService.cancelAppointment(this.appointmentIdToCancel).subscribe(() => {
        this.triggerAlert('Appointment cancelled.', 'success');
        this.closeModal();
      });
    }
  }

  onCancel(id: string) {
    // Deprecated for custom modal: confirmCancel
  }

  getSelectedSlotTime(): string {
    const slot = this.slots.find(s => s.id === this.selectedSlotId);
    return slot ? `${slot.startTime} - ${slot.endTime}` : 'Not selected';
  }
}
