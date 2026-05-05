import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of, forkJoin } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { map, catchError, tap, switchMap } from 'rxjs/operators';
import ICAL from 'ical.js';

export interface TimeSlot {
  id: string;
  startTime: string;
  endTime: string;
  status: 'available' | 'booked' | 'locked' | 'holiday';
  citizenId?: string;
  serviceType?: string;
}

export interface Holiday {
  date: string; // YYYY-MM-DD
  summary: string;
  type: 'holiday' | 'special-day' | 'leave';
}

@Injectable({
  providedIn: 'root'
})
export class AppointmentService {
  private holidaysSubject = new BehaviorSubject<Holiday[]>([]);
  holidays$ = this.holidaysSubject.asObservable();

  private slotsSubject = new BehaviorSubject<TimeSlot[]>([]);
  slots$ = this.slotsSubject.asObservable();

  private selectedDateSubject = new BehaviorSubject<Date>(new Date());
  selectedDate$ = this.selectedDateSubject.asObservable();

  private currentAppointmentsSubject = new BehaviorSubject<any[]>([]);
  currentAppointments$ = this.currentAppointmentsSubject.asObservable();

  constructor(private http: HttpClient) {
    this.refreshHolidays();
    this.generateMockSlots();
    this.loadUserAppointments();
  }

  refreshHolidays() {
    const holidayFiles = [
      { path: 'assets/srilanka-holidays-2026.ics', type: 'holiday' as const }
    ];

    const requests = holidayFiles.map(file => 
      this.http.get(file.path, { responseType: 'text' }).pipe(
        map(content => {
          console.log(`[AppointmentService] Fetching ${file.path}, length: ${content?.length || 0}`);
          const events = this.parseICS(content, file.type);
          return events;
        }),
        catchError(err => {
          console.error(`[AppointmentService] Network Error for ${file.path}:`, err);
          return of([]);
        })
      )
    );

    forkJoin(requests).pipe(
      map(results => results.flat()),
      tap(allHolidays => {
        console.log('[AppointmentService] Synced Holidays:', allHolidays.length);
        if (allHolidays.length > 0) {
          console.table(allHolidays.slice(0, 5));
        }
        // Load stored leaves from localStorage if any
        const storedLeaves = localStorage.getItem('gn_custom_leaves');
        const customLeaves: Holiday[] = storedLeaves ? JSON.parse(storedLeaves) : [];
        
        this.holidaysSubject.next([...allHolidays, ...customLeaves]);
      })
    ).subscribe();
  }

  private parseICS(content: string, type: 'holiday' | 'special-day'): Holiday[] {
    try {
      if (!content || !content.trim()) {
        console.warn('[AppointmentService] Empty ICS content received');
        return [];
      }
      
      const jcalData = ICAL.parse(content);
      const component = new ICAL.Component(jcalData);
      const events = component.getAllSubcomponents('vevent');

      return events.map((eventComp: any) => {
        const dateProp = eventComp.getFirstProperty('dtstart');
        const summaryProp = eventComp.getFirstPropertyValue('summary');
        
        let dateStr = '';
        if (dateProp) {
          const dt = dateProp.getFirstValue();
          dateStr = `${dt.year}-${String(dt.month).padStart(2, '0')}-${String(dt.day).padStart(2, '0')}`;
        }

        return {
          date: dateStr,
          summary: summaryProp || 'Holiday',
          type
        };
      }).filter((h: Holiday) => h.date && h.date !== '');
    } catch (e) {
      console.error('[AppointmentService] ICS Parsing Failed:', e);
      return [];
    }
  }

  private loadUserAppointments() {
    const mockAppointments = [
      { id: 'app1', citizenName: 'Saman Kumara', date: '2026-04-10', time: '10:00 - 10:30', service: 'Residence Verification', status: 'Confirmed' },
      { id: 'app2', citizenName: 'Nimali Perera', date: '2026-04-10', time: '11:00 - 11:30', service: 'Income Certificate', status: 'Pending' }
    ];
    this.currentAppointmentsSubject.next(mockAppointments);
  }

  cancelAppointment(id: string) {
    const apps = this.currentAppointmentsSubject.value.filter(a => a.id !== id);
    this.currentAppointmentsSubject.next(apps);
    return of(true);
  }

  private generateMockSlots() {
    const slots: TimeSlot[] = [
      { id: '1', startTime: '09:00', endTime: '09:30', status: 'available' },
      { id: '2', startTime: '09:30', endTime: '10:00', status: 'booked' },
      { id: '3', startTime: '10:00', endTime: '10:30', status: 'available' },
      { id: '4', startTime: '10:30', endTime: '11:00', status: 'available' },
      { id: '5', startTime: '11:00', endTime: '11:30', status: 'available' },
      { id: '6', startTime: '11:30', endTime: '12:00', status: 'booked' },
      { id: '7', startTime: '14:00', endTime: '14:30', status: 'available' },
      { id: '8', startTime: '14:30', endTime: '15:00', status: 'available' },
      { id: '9', startTime: '15:00', endTime: '15:30', status: 'available' },
      { id: '10', startTime: '15:30', endTime: '16:00', status: 'available' },
    ];
    this.slotsSubject.next(slots);
  }

  setSelectedDate(date: Date) {
    this.selectedDateSubject.next(date);
    this.generateMockSlots();
  }

  bookSlot(slotId: string, serviceType: string): Observable<boolean> {
    const currentSlots = this.slotsSubject.value;
    const slotIndex = currentSlots.findIndex(s => s.id === slotId);
    
    if (slotIndex !== -1 && currentSlots[slotIndex].status === 'available') {
      const updatedSlots = [...currentSlots];
      updatedSlots[slotIndex] = {
        ...updatedSlots[slotIndex],
        status: 'booked',
        serviceType: serviceType
      };
      this.slotsSubject.next(updatedSlots);
      return of(true);
    }
    return of(false);
  }

  isHoliday(date: Date): Holiday | undefined {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const dateStr = `${year}-${month}-${day}`;
    return this.holidaysSubject.value.find(h => h.date === dateStr);
  }

  markLeave(date: Date) {
    const dateStr = this.formatDate(date);
    const currentHolidays = this.holidaysSubject.value;
    
    if (!currentHolidays.some(h => h.date === dateStr)) {
      const newLeave: Holiday = {
        date: dateStr,
        summary: 'Marked Leave',
        type: 'leave'
      };
      const updated = [...currentHolidays, newLeave];
      this.holidaysSubject.next(updated);
      this.saveCustomLeaves(updated.filter(h => h.type === 'leave'));
    }
  }

  removeLeave(date: Date) {
    const dateStr = this.formatDate(date);
    const updated = this.holidaysSubject.value.filter(h => !(h.date === dateStr && h.type === 'leave'));
    this.holidaysSubject.next(updated);
    this.saveCustomLeaves(updated.filter(h => h.type === 'leave'));
  }

  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private saveCustomLeaves(leaves: Holiday[]) {
    localStorage.setItem('gn_custom_leaves', JSON.stringify(leaves));
  }
}

