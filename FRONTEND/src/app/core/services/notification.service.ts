import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of, BehaviorSubject } from 'rxjs';
import { catchError, tap } from 'rxjs/operators';

export interface NotificationModel {
  id: number;
  recipientNic: string;
  recipientRole: string;
  type: string; // 'pending' | 'rejected' | 'approved' | 'appointment' | 'chat' etc.
  title: string;
  message: string;
  relatedEntityType?: string;
  relatedEntityId?: string;
  actionUrl?: string;
  priority: string;
  isRead: boolean;
  readDate?: string;
  createdAt: string;
  createdBy?: string;
  isActive: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private http = inject(HttpClient);
  private baseUrl = 'http://localhost:5058/api/Notification';

  private unreadCountSubject = new BehaviorSubject<number>(0);
  unreadCount$ = this.unreadCountSubject.asObservable();

  private notificationsSubject = new BehaviorSubject<NotificationModel[]>([]);
  notifications$ = this.notificationsSubject.asObservable();

  private getHeaders(): HttpHeaders {
    const token = sessionStorage.getItem('token');
    return new HttpHeaders().set('Authorization', `Bearer ${token}`);
  }

  fetchNotifications(): Observable<NotificationModel[]> {
    return this.http.get<NotificationModel[]>(this.baseUrl, { headers: this.getHeaders() }).pipe(
      tap(list => {
        this.notificationsSubject.next(list);
        const unread = list.filter(n => !n.isRead).length;
        this.unreadCountSubject.next(unread);
      }),
      catchError(err => {
        console.error('[NotificationService fetchNotifications Error]', err);
        return of([]);
      })
    );
  }

  fetchUnreadCount(): Observable<{ count: number }> {
    return this.http.get<{ count: number }>(`${this.baseUrl}/unread-count`, { headers: this.getHeaders() }).pipe(
      tap(res => {
        this.unreadCountSubject.next(res.count);
      }),
      catchError(err => {
        console.error('[NotificationService fetchUnreadCount Error]', err);
        return of({ count: 0 });
      })
    );
  }

  markAsRead(id: number): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/${id}/read`, {}, { headers: this.getHeaders() }).pipe(
      tap(() => {
        // Optimistic update
        const currentList = this.notificationsSubject.value;
        const updatedList = currentList.map(n => n.id === id ? { ...n, isRead: true, readDate: new Date().toISOString() } : n);
        this.notificationsSubject.next(updatedList);
        this.unreadCountSubject.next(updatedList.filter(n => !n.isRead).length);
      }),
      catchError(err => {
        console.error('[NotificationService markAsRead Error]', err);
        return of(null);
      })
    );
  }

  markAllAsRead(): Observable<any> {
    return this.http.post<any>(`${this.baseUrl}/mark-all-read`, {}, { headers: this.getHeaders() }).pipe(
      tap(() => {
        const currentList = this.notificationsSubject.value;
        const updatedList = currentList.map(n => ({ ...n, isRead: true, readDate: new Date().toISOString() }));
        this.notificationsSubject.next(updatedList);
        this.unreadCountSubject.next(0);
      }),
      catchError(err => {
        console.error('[NotificationService markAllAsRead Error]', err);
        return of(null);
      })
    );
  }

  deleteNotification(id: number): Observable<any> {
    return this.http.delete<any>(`${this.baseUrl}/${id}`, { headers: this.getHeaders() }).pipe(
      tap(() => {
        const currentList = this.notificationsSubject.value;
        const updatedList = currentList.filter(n => n.id !== id);
        this.notificationsSubject.next(updatedList);
        this.unreadCountSubject.next(updatedList.filter(n => !n.isRead).length);
      }),
      catchError(err => {
        console.error('[NotificationService deleteNotification Error]', err);
        return of(null);
      })
    );
  }
}
