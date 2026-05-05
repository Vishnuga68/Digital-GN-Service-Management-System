import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { AppointmentService, TimeSlot, Holiday } from '../../../core/services/appointment.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { FullCalendarModule, FullCalendarComponent } from '@fullcalendar/angular';
import { CalendarOptions } from '@fullcalendar/core';
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
  
  // GN Specific Data
  allAppointments: any[] = [];
  selectedDayAppointments: any[] = [];
  userAppointments: any[] = []; // Current GN appointments list
  
  isHoliday: boolean = false;
  holidayDetail: Holiday | undefined;
  
  // Alert Modal State
  showAlertModal: boolean = false;
  alertMessage: string = '';
  alertType: 'success' | 'error' = 'success';
  
  // Custom Confirmation Modal
  showCancelModal: boolean = false;
  appointmentIdToCancel: string | null = null;

  // Popover State
  showPopover: boolean = false;
  popoverContent: string = '';
  popoverLabel: string = '';
  popoverType: 'holiday' | 'appointment' | 'leave' = 'holiday';
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
    events: [],
    
    dayCellClassNames: (arg: any) => {
      const classes = [];
      const date = arg.date;

      const holiday = this.appointmentService.isHoliday(date);
      if (holiday) {
        if (holiday.type === 'leave') {
          classes.push('fc-leave-cell');
        } else {
          classes.push('fc-holiday-cell');
        }
      }
      
      if (this.selectedDate && date.toDateString() === this.selectedDate.toDateString()) {
        classes.push('fc-day-selected');
      }

      const hasAppointment = this.allAppointments.some(app => {
        const appDate = new Date(app.date);
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

    dateClick: (arg: any) => {
      const date = arg.date;
      this.onDateSelect(date);
    }
  };

  private destroy$ = new Subject<void>();

  constructor(private appointmentService: AppointmentService) {}

  ngOnInit(): void {
    this.appointmentService.currentAppointments$
      .pipe(takeUntil(this.destroy$))
      .subscribe(apps => {
        this.allAppointments = apps;
        this.userAppointments = apps; // Match naming for consistency or use allAppointments
        this.loadSelectedDayAppointments();
        this.calendarOptions = { ...this.calendarOptions };
      });

    this.appointmentService.selectedDate$
      .pipe(takeUntil(this.destroy$))
      .subscribe(date => {
        this.selectedDate = date;
        this.checkHoliday();
        this.loadSelectedDayAppointments();
        this.calendarOptions = { ...this.calendarOptions };
      });

    this.appointmentService.holidays$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.checkHoliday();
        this.calendarOptions = { ...this.calendarOptions };
      });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  onDateSelect(date: Date) {
    this.appointmentService.setSelectedDate(date);
  }

  checkHoliday() {
    this.holidayDetail = this.appointmentService.isHoliday(this.selectedDate);
    this.isHoliday = !!this.holidayDetail;
  }

  loadSelectedDayAppointments() {
     this.selectedDayAppointments = this.allAppointments.filter(app => {
        return new Date(app.date).toDateString() === this.selectedDate.toDateString();
     });
  }

  closePopover() {
    this.showPopover = false;
  }

  triggerAlert(message: string, type: 'success' | 'error' = 'success') {
    this.alertMessage = message;
    this.alertType = type;
    this.showAlertModal = true;
  }

  closeAlert() {
    this.showAlertModal = false;
  }

  markLeave() {
     if(!this.isHoliday) {
        this.appointmentService.markLeave(this.selectedDate);
        this.triggerAlert('Leave marked successfully for ' + this.selectedDate.toDateString(), 'success');
     }
  }

  removeLeave() {
     if(this.isHoliday) {
        if(this.holidayDetail?.type === 'leave') {
            this.appointmentService.removeLeave(this.selectedDate);
            this.triggerAlert('Leave removed successfully.', 'success');
        } else {
            this.triggerAlert('Cannot remove official government holidays.', 'error');
        }
     }
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

  reschedule(id: string) {
    this.triggerAlert('Reschedule feature coming soon. Please contact the citizen directly for now.', 'success');
  }
}

