import { Component, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppointmentService, Holiday } from '../../../core/services/appointment.service';
import { Subject } from 'rxjs';
import { takeUntil } from 'rxjs/operators';

import { FullCalendarModule, FullCalendarComponent } from '@fullcalendar/angular';
import { CalendarOptions } from '@fullcalendar/core';
import dayGridPlugin from '@fullcalendar/daygrid';
import interactionPlugin from '@fullcalendar/interaction';

import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-availability-page',
  standalone: true,
  imports: [CommonModule, FormsModule, FullCalendarModule, RouterModule],
  templateUrl: './availability-page.component.html',
  styleUrl: './availability-page.component.scss'
})
export class AvailabilityPageComponent implements OnInit, OnDestroy {
  @ViewChild('calendar') calendarComponent!: FullCalendarComponent;

  selectedDate: Date = new Date();
  gnOfficerNic: string = '';
  availabilities: any[] = [];
  
  isHoliday: boolean = false;
  holidayDetail: Holiday | undefined;

  // Form State
  formStatus: string = '';
  formStartTime: string = '';
  formEndTime: string = '';
  formNotes: string = '';
  currentAvailabilityId: number | null = null;

  // Alert Modal
  showAlertModal: boolean = false;
  alertMessage: string = '';
  alertType: 'success' | 'error' = 'success';

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
    events: [],
    
    dayCellClassNames: (arg: any) => {
      const classes = [];
      const date = arg.date;
      const today = new Date();
      today.setHours(0, 0, 0, 0);
      
      const oneMonthLater = new Date();
      oneMonthLater.setMonth(today.getMonth() + 1);

      const holiday = this.appointmentService.isHoliday(date);
      if (holiday) {
        classes.push('fc-holiday-cell'); // RED
      }
      
      if (date < today || date > oneMonthLater || date.getDay() === 0) {
        classes.push('fc-date-restricted');
      }

      if (this.selectedDate && date.toDateString() === this.selectedDate.toDateString()) {
        classes.push('fc-day-selected');
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
      
      this.onDateSelect(date);
      
      if (holiday) {
        const rect = arg.dayEl.getBoundingClientRect();
        this.popoverPosition = {
          top: `${rect.top + window.scrollY - 5}px`, 
          left: `${rect.left + window.scrollX + rect.width / 2}px`
        };
        
        this.popoverType = 'holiday';
        this.popoverLabel = 'Official Holiday';
        this.popoverContent = holiday.summary;
        this.showPopover = true;
      } else {
        this.showPopover = false;
      }
    }
  };

  private destroy$ = new Subject<void>();

  constructor(private appointmentService: AppointmentService) {}

  ngOnInit(): void {
    this.gnOfficerNic = sessionStorage.getItem('userNic') || '';

    this.appointmentService.selectedDate$
      .pipe(takeUntil(this.destroy$))
      .subscribe(date => {
        this.selectedDate = date;
        this.checkHoliday();
        this.loadDayDetails();
      });

    this.appointmentService.holidays$
      .pipe(takeUntil(this.destroy$))
      .subscribe(() => {
        this.checkHoliday();
        this.calendarOptions = { ...this.calendarOptions };
      });

    // Load month initially
    this.loadMonthAvailabilities();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  closePopover() {
    this.showPopover = false;
  }

  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  loadMonthAvailabilities() {
    const start = new Date(this.selectedDate.getFullYear(), this.selectedDate.getMonth() - 1, 1);
    const end = new Date(this.selectedDate.getFullYear(), this.selectedDate.getMonth() + 2, 0);

    this.appointmentService.getGnAvailability(this.gnOfficerNic, start, end).subscribe(avails => {
      this.availabilities = avails;
      this.calendarOptions = { ...this.calendarOptions };
      this.loadDayDetails();
    });
  }

  onDateSelect(date: Date) {
    this.appointmentService.setSelectedDate(date);
  }

  checkHoliday() {
    this.holidayDetail = this.appointmentService.isHoliday(this.selectedDate);
    this.isHoliday = !!this.holidayDetail;
  }

  isLeaveDay: boolean = false;
  isPastDate: boolean = false;
  isOutRange: boolean = false;

  get isFormHidden(): boolean {
    return this.isHoliday || this.isPastDate || this.isOutRange || this.selectedDate.getDay() === 0;
  }

  get isFormDisabled(): boolean {
    return this.isFormHidden;
  }

  get restrictionMessage(): string {
    if (this.isPastDate) return 'You cannot set availability for past dates.';
    if (this.isOutRange) return 'You cannot set availability more than 1 month in advance.';
    if (this.isHoliday) return 'You cannot set availability on official holidays.';
    if (this.selectedDate.getDay() === 0) return 'You cannot set availability on weekend holidays.';
    return 'Availability configuration is restricted for this date.';
  }

  loadDayDetails() {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const oneMonthLater = new Date();
    oneMonthLater.setMonth(today.getMonth() + 1);

    this.isPastDate = this.selectedDate < today;
    this.isOutRange = this.selectedDate > oneMonthLater;

    this.appointmentService.getGnAvailabilityDay(this.gnOfficerNic, this.selectedDate).subscribe({
      next: (avails) => {
        const avail = avails && avails.length > 0 ? avails[0] : null;
        if (avail) {
          this.formStatus = avail.status || avail.Status;
          
          const st = avail.startTime || avail.StartTime || '08:30:00';
          const et = avail.endTime || avail.EndTime || '16:15:00';
          this.formStartTime = st.substring(0, 5);
          this.formEndTime = et.substring(0, 5);
          
          this.formNotes = avail.notes || avail.Notes || '';
          this.currentAvailabilityId = avail.availabilityId || avail.AvailabilityId;
          this.isLeaveDay = this.formStatus === 'LEAVE';
        } else {
          this.resetForm();
          this.isLeaveDay = false;
        }

        if (this.isFormHidden) {
          this.resetFormStateOnly();
        }
      },
      error: () => {
        this.resetForm();
        this.isLeaveDay = false;
        if (this.isFormHidden) {
          this.resetFormStateOnly();
        }
      }
    });
  }

  onStatusChange() {
    if (this.formStatus === 'LEAVE') {
      this.formStartTime = '';
      this.formEndTime = '';
    } else if (this.formStartTime === '' && this.formEndTime === '') {
      // If it was LEAVE and user switched back to something else, reset to defaults
      this.formStartTime = '08:30';
      this.formEndTime = '16:15';
    }
  }

  resetFormStateOnly() {
    this.formStatus = '';
    this.formStartTime = '08:30';
    this.formEndTime = '16:15';
    this.formNotes = '';
    this.currentAvailabilityId = null;
  }

  resetForm() {
    this.resetFormStateOnly();
    this.isLeaveDay = false;
  }

  saveAvailability() {
    if (this.isHoliday) {
      this.triggerAlert('Cannot set availability on holiday.', 'error');
      return;
    }
    if (this.selectedDate.getDay() === 0) { // Sunday
      this.triggerAlert('Cannot set availability on this day.', 'error');
      return;
    }

    if (!this.formStatus) {
      this.triggerAlert('Status is required.', 'error');
      return;
    }

    if (this.formStatus !== 'LEAVE') {
      if (!this.formStartTime || !this.formEndTime) {
        this.triggerAlert('Start Time and End Time are required.', 'error');
        return;
      }
      if (this.formEndTime <= this.formStartTime) {
        this.triggerAlert('End Time must be later than Start Time.', 'error');
        return;
      }
    } else {
      this.formStartTime = '';
      this.formEndTime = '';
    }

    this.appointmentService.setGnAvailability(
      this.gnOfficerNic, 
      this.selectedDate, 
      this.formStartTime || null, 
      this.formEndTime || null, 
      this.formStatus, 
      this.formNotes
    ).subscribe({
      next: () => {
        this.triggerAlert('Availability saved successfully.', 'success');
        this.loadMonthAvailabilities();
      },
      error: (err) => {
        this.triggerAlert(err.error?.Message || 'Failed to save availability.', 'error');
      }
    });
  }

  triggerAlert(message: string, type: 'success' | 'error' = 'success') {
    this.alertMessage = message;
    this.alertType = type;
    this.showAlertModal = true;
  }

  closeAlert() {
    this.showAlertModal = false;
  }
}
