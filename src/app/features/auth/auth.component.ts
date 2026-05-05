import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, AbstractControl } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { TranslationService } from '../../core/services/translation.service';

type AuthView = 'login' | 'register' | 'forgot' | 'reset';

@Component({
  selector: 'app-auth',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './auth.component.html',
  styleUrls: ['./auth.component.scss']
})
export class AuthComponent implements OnInit {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  public translationService = inject(TranslationService);
  private router = inject(Router);

  currentView: AuthView = 'login';
  translations: any = {};
  
  loginForm!: FormGroup;
  registerForm!: FormGroup;
  forgotForm!: FormGroup;
  resetForm!: FormGroup;

  isLoading = false;
  toastMessage: string | null = null;
  toastType: 'success' | 'error' = 'success';

  ngOnInit(): void {
    this.translationService.translations$.subscribe(data => {
      this.translations = data;
    });

    this.initForms();
  }

  initForms() {
    this.loginForm = this.fb.group({
      nic: ['', Validators.required],
      password: ['', Validators.required]
    });

    this.registerForm = this.fb.group({
      nic: ['', [Validators.required, Validators.pattern(/^[0-9]{9}[vVxX]|[0-9]{12}$/)]],
      fullName: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      telephone: ['', Validators.required],
      province: ['', Validators.required],
      district: ['', Validators.required],
      dsDivision: ['', Validators.required],
      gnDivision: ['', Validators.required],
      password: ['', [Validators.required, Validators.minLength(4)]],
      confirmPassword: ['', Validators.required]
    }, { validators: this.passwordMatchValidator });

    this.forgotForm = this.fb.group({
      emailNic: ['', Validators.required]
    });

    this.resetForm = this.fb.group({
      otp: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(4)]]
    });
  }

  passwordMatchValidator(g: AbstractControl): { [key: string]: boolean } | null {
    const password = g.get('password')?.value;
    const confirmPassword = g.get('confirmPassword')?.value;
    if (password && confirmPassword && password !== confirmPassword) {
      return { 'mismatch': true };
    }
    return null;
  }

  switchLanguage(lang: string) {
    this.translationService.setLanguage(lang);
  }

  setView(view: AuthView) {
    this.currentView = view;
  }

  showToast(message: string, type: 'success' | 'error') {
    this.toastMessage = message;
    this.toastType = type;
    setTimeout(() => { this.toastMessage = null; }, 3000);
  }

  onLogin() {
    if (this.loginForm.invalid) {
      this.showToast(this.translations.ERROR_INVALID, 'error');
      return;
    }
    this.isLoading = true;
    const { nic, password } = this.loginForm.value;
    this.authService.login(nic, password).subscribe(res => {
      this.isLoading = false;
      if (res.success) {
        this.showToast(this.translations.SUCCESS_LOGIN + ' Role: ' + res.role, 'success');
        if (res.role === 'Citizen') {
          this.router.navigate(['/citizen']);
        } else if (res.role === 'GN Officer') {
          this.router.navigate(['/gn-officer']);
        } else if (res.role === 'DS Officer') {
          this.router.navigate(['/ds-officer']);
        }
      } else {
        this.showToast(res.message || 'Login failed', 'error');
      }
    });
  }

  onRegister() {
    if (this.registerForm.invalid) {
      this.showToast(this.translations.ERROR_INVALID, 'error');
      return;
    }
    this.isLoading = true;
    this.authService.register(this.registerForm.value).subscribe(res => {
      this.isLoading = false;
      if (res.success) {
        this.showToast(this.translations.SUCCESS_REGISTER, 'success');
        this.setView('login');
        this.registerForm.reset();
      }
    });
  }

  onSendOtp() {
    if (this.forgotForm.invalid) return;
    this.isLoading = true;
    this.authService.sendOtp(this.forgotForm.value.emailNic).subscribe(res => {
      this.isLoading = false;
      this.setView('reset');
    });
  }

  onResetPassword() {
    if (this.resetForm.invalid) return;
    this.isLoading = true;
    const { otp, newPassword } = this.resetForm.value;
    this.authService.resetPassword(otp, newPassword).subscribe(res => {
      this.isLoading = false;
      if (res.success) {
        this.showToast(this.translations.SUCCESS_PWD_RESET, 'success');
        this.setView('login');
      } else {
        this.showToast(res.message || 'Error', 'error');
      }
    });
  }
}
