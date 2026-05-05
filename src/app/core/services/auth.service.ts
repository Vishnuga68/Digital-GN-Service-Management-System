import { Injectable } from '@angular/core';
import { Observable, of, delay } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  
  constructor() { }

  login(nic: string, password: string): Observable<{ success: boolean; role?: string; message?: string }> {
    // Hardcoded roles for testing (3 for each role)
    const citNics = ['citizen1', 'citizen2', 'citizen3'];
    const gnNics = ['gn1', 'gn2', 'gn3'];
    const dsNics = ['ds1', 'ds2', 'ds3'];

    if (citNics.includes(nic) && password === nic) {
      return of({ success: true, role: 'Citizen' }).pipe(delay(800));
    } else if (gnNics.includes(nic) && password === nic) {
      return of({ success: true, role: 'GN Officer' }).pipe(delay(800));
    } else if (dsNics.includes(nic) && password === nic) {
      return of({ success: true, role: 'DS Officer' }).pipe(delay(800));
    }

    return of({ success: false, message: 'Invalid credentials. Try citizen1/citizen1, gn1/gn1, ds1/ds1.' }).pipe(delay(800));
  }

  register(data: any): Observable<{ success: boolean; message: string }> {
    return of({ success: true, message: 'Registration successful!' }).pipe(delay(800));
  }

  sendOtp(nicOrEmail: string): Observable<{ success: boolean }> {
    return of({ success: true }).pipe(delay(800));
  }

  resetPassword(otp: string, newPassword: string): Observable<{ success: boolean; message?: string }> {
    // any OTP is accepted for simplicity in this mock, or standard '1234'
    if (otp === '1234' || otp.length === 4) { 
      return of({ success: true, message: 'Password reset successful!' }).pipe(delay(800));
    }
    return of({ success: false, message: 'Invalid OTP' }).pipe(delay(800));
  }
}
