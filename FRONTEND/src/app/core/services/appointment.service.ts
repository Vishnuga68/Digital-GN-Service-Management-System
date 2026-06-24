import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable, of, forkJoin } from 'rxjs';
import { HttpClient } from '@angular/common/http';
import { map, catchError, tap, switchMap } from 'rxjs/operators';
import ICAL from 'ical.js';

export interface TimeSlot {
  id?: string;
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
  private baseUrl = 'http://localhost:5058/api/Appointment';
  
  private holidaysSubject = new BehaviorSubject<Holiday[]>([]);
  holidays$ = this.holidaysSubject.asObservable();

  private selectedDateSubject = new BehaviorSubject<Date>(new Date());
  selectedDate$ = this.selectedDateSubject.asObservable();

  constructor(private http: HttpClient) {
    this.refreshHolidays();
  }

  // --- Holidays / ICS Integration (Unchanged logic) ---
  refreshHolidays() {
    const holidayFiles = [
      { path: 'assets/srilanka-holidays-2026.ics', type: 'holiday' as const }
    ];

    const requests = holidayFiles.map(file => 
      this.http.get(file.path, { responseType: 'text' }).pipe(
        map(content => {
          const events = this.parseICS(content, file.type);
          return events;
        }),
        catchError(err => {
          return of([]);
        })
      )
    );

    forkJoin(requests).pipe(
      map(results => results.flat()),
      tap(allHolidays => {
        this.holidaysSubject.next(allHolidays);
      })
    ).subscribe();
  }

  private parseICS(content: string, type: 'holiday' | 'special-day'): Holiday[] {
    try {
      if (!content || !content.trim()) return [];
      
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
      return [];
    }
  }

  isHoliday(date: Date): Holiday | undefined {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    const dateStr = `${year}-${month}-${day}`;
    return this.holidaysSubject.value.find(h => h.date === dateStr);
  }

  setSelectedDate(date: Date) {
    this.selectedDateSubject.next(date);
  }

  private formatDate(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  // --- GN Officer APIs ---
  getGnAvailability(gnOfficerNic: string, startDate: Date, endDate: Date): Observable<any[]> {
    const startStr = this.formatDate(startDate);
    const endStr = this.formatDate(endDate);
    return this.http.get<any[]>(`http://localhost:5058/api/availability/month?gnOfficerNic=${gnOfficerNic}&startDate=${startStr}&endDate=${endStr}`);
  }

  getGnAvailabilityDay(gnOfficerNic: string, date: Date): Observable<any[]> {
    const dateStr = this.formatDate(date);
    return this.http.get<any[]>(`http://localhost:5058/api/availability/day?gnOfficerNic=${gnOfficerNic}&date=${dateStr}`);
  }

  setGnAvailability(gnOfficerNic: string, date: Date, startTime: string | null, endTime: string | null, status: string, notes: string = ''): Observable<any> {
    const formattedStartTime = startTime && startTime.length === 5 ? `${startTime}:00` : startTime;
    const formattedEndTime = endTime && endTime.length === 5 ? `${endTime}:00` : endTime;

    const payload = {
      gnOfficerNic: gnOfficerNic,
      availableDate: this.formatDate(date),
      startTime: formattedStartTime,
      endTime: formattedEndTime,
      status: status,
      notes: notes
    };
    return this.http.post<any>(`http://localhost:5058/api/availability/save`, payload);
  }

  deleteGnAvailability(id: number): Observable<any> {
    return this.http.delete(`${this.baseUrl}/gn/availability/${id}`); // fallback if needed
  }

  getGnAppointments(gnOfficerNic: string, startDate: Date, endDate: Date): Observable<any[]> {
    const startStr = this.formatDate(startDate);
    const endStr = this.formatDate(endDate);
    return this.http.get<any[]>(`http://localhost:5058/api/appointments/gn/list?gnOfficerNic=${gnOfficerNic}&startDate=${startStr}&endDate=${endStr}`);
  }

  cancelGnAppointment(appointmentId: number, reason: string): Observable<any> {
    return this.http.put(`http://localhost:5058/api/appointments/cancel`, { appointmentId, cancellationReason: reason });
  }

  updateGnAppointmentStatus(appointmentId: number, status: string, reason: string = ''): Observable<any> {
    return this.http.put(`http://localhost:5058/api/Appointment/gn/appointments/${appointmentId}/status`, { status, cancellationReason: reason });
  }

  rescheduleGnAppointment(appointmentId: number, newDate: Date, newStartTime: string, newEndTime: string): Observable<any> {
    const formattedStartTime = newStartTime && newStartTime.length === 5 ? `${newStartTime}:00` : newStartTime;
    const formattedEndTime = newEndTime && newEndTime.length === 5 ? `${newEndTime}:00` : newEndTime;

    return this.http.put(`http://localhost:5058/api/appointments/reschedule`, {
      appointmentId,
      newDate: this.formatDate(newDate),
      newStartTime: formattedStartTime,
      newEndTime: formattedEndTime
    });
  }

  // --- Citizen APIs ---
  getAvailableSlots(gnOfficerNic: string, targetDate: Date): Observable<any[]> {
    const dateStr = this.formatDate(targetDate);
    return this.http.get<any[]>(`${this.baseUrl}/citizen/slots/${gnOfficerNic}?targetDate=${dateStr}`);
  }

  bookAppointment(citizenNic: string, gnOfficerNic: string, date: Date, startTime: string, endTime: string, serviceType: string): Observable<any> {
    const formattedStartTime = startTime && startTime.length === 5 ? `${startTime}:00` : startTime;
    const formattedEndTime = endTime && endTime.length === 5 ? `${endTime}:00` : endTime;

    const payload = {
      citizenNic: citizenNic,
      gnOfficerNic: gnOfficerNic,
      appointmentDate: this.formatDate(date),
      startTime: formattedStartTime,
      endTime: formattedEndTime,
      serviceType: serviceType
    };
    return this.http.post<any>(`${this.baseUrl}/citizen/book`, payload);
  }

  getCitizenAppointments(citizenNic: string): Observable<any[]> {
    return this.http.get<any[]>(`${this.baseUrl}/citizen/appointments/${citizenNic}`);
  }

  cancelCitizenAppointment(id: number, citizenNic: string, reason: string): Observable<any> {
    return this.http.put(`${this.baseUrl}/citizen/appointments/${id}/cancel`, { citizenNic, cancellationReason: reason });
  }
}
