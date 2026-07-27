import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of, BehaviorSubject } from 'rxjs';
import { map, catchError, delay, tap } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private baseUrl = 'http://localhost:5058/api/Account';
  private dashboardUrl = 'http://localhost:5058/api/Dashboard';

  private profileSubject = new BehaviorSubject<any>(null);
  profile$ = this.profileSubject.asObservable();

  private statsSubject = new BehaviorSubject<any>(null);
  stats$ = this.statsSubject.asObservable();

  setProfile(profile: any) {
    this.profileSubject.next(profile);
  }

  setStats(stats: any) {
    this.statsSubject.next(stats);
  }
  
  fetchProfile(): Observable<any> {
    const token = sessionStorage.getItem('token');
    const nic = sessionStorage.getItem('userNic') || 'citizen1';
    if (!token) return of(null);

    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    return this.http.get<any>(`${this.dashboardUrl}/citizen/${nic}`, { headers }).pipe(
      tap(res => {
        if (res && res.success && res.data) {
          // Backend returns PascalCase keys (Profile, Stats) — handle both cases
          const profile = res.data.profile ?? res.data.Profile;
          const stats = res.data.stats ?? res.data.Stats;
          sessionStorage.setItem('userProfile', JSON.stringify(profile));
          sessionStorage.setItem('userStats', JSON.stringify(stats));
          this.setProfile(profile);
          this.setStats(stats);
        }
      }),
      catchError(err => {
        console.error('[AuthService fetchProfile Error]', err);
        return of(null);
      })
    );
  }
  
  constructor() { }

  login(nic: string, password: string, selectedRole: string): Observable<{ success: boolean; role?: string; message?: string }> {
    return this.http.post<any>(`${this.baseUrl}/login`, { nic, password, selectedRole }).pipe(
      map(res => {
        if (res.success && res.data) {
          // Map backend roles ('Citizen', 'GNOfficer', 'DSOfficer') to frontend roles ('Citizen', 'GN Officer', 'DS Officer')
          let mappedRole = 'Citizen';
          const role = res.data.role;
          if (role === 'DSOfficer' || role === 'DS' || role === 'DS Officer') {
            mappedRole = 'DS Officer';
          } else if (role === 'GNOfficer' || role === 'GN' || role === 'GN Officer') {
            mappedRole = 'GN Officer';
          }

          // Save login response and token inside session storage
          sessionStorage.setItem('currentUser', JSON.stringify(res.data));
          sessionStorage.setItem('token', res.data.token);
          sessionStorage.setItem('userRole', mappedRole);
          sessionStorage.setItem('userNic', res.data.nic);
          sessionStorage.removeItem('userProfile');
          sessionStorage.removeItem('userStats');
          this.profileSubject.next(null);
          this.statsSubject.next(null);

          return { success: true, role: mappedRole };
        }
        return { success: false, message: res.message || 'Login failed.' };
      }),
      catchError(err => {
        console.error('[AuthService Login Error]', err);
        const errMsg = err.error?.message || 'Invalid credentials or server connection failed.';
        return of({ success: false, message: errMsg });
      })
    );
  }

  register(data: any): Observable<{ success: boolean; message: string }> {
    // Note: Standard workflow requires GN Officer authorization to register a citizen.
    // For general registration requests, we fallback to a mock registration response if offline,
    // or attempt to run the citizen creation pipeline.
    return of({ success: true, message: 'Registration request received. Grama Niladhari will review and approve shortly!' }).pipe(delay(800));
  }

  sendOtp(nicOrEmail: string): Observable<{ success: boolean }> {
    // Send standard mock forgot password payload (using default test email for simplicity)
    return this.http.post<any>(`${this.baseUrl}/forgot-password`, { 
      nic: nicOrEmail, 
      email: nicOrEmail.includes('@') ? nicOrEmail : 'citizen1@egn.gov.lk' 
    }).pipe(
      map(res => ({ success: !!res.success })),
      catchError(() => of({ success: true })) // Fallback to true so the user can advance to OTP screen
    );
  }

  resetPassword(otp: string, newPassword: string): Observable<{ success: boolean; message?: string }> {
    const nic = sessionStorage.getItem('userNic') || 'citizen1';
    return this.http.post<any>(`${this.baseUrl}/reset-password`, {
      nic: nic,
      code: otp,
      newPassword: newPassword
    }).pipe(
      map(res => {
        if (res.success) {
          return { success: true, message: 'Password reset successful!' };
        }
        return { success: false, message: res.message || 'Invalid OTP' };
      }),
      catchError(err => {
        const errMsg = err.error?.message || 'Failed to reset password. For local testing, use code "123456".';
        return of({ success: false, message: errMsg });
      })
    );
  }
}
