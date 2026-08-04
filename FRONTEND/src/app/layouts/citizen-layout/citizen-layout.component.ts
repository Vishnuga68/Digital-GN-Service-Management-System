import { Component, inject, OnInit, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { filter } from 'rxjs/operators';
import { TranslationService } from '../../core/services/translation.service';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService, NotificationModel } from '../../core/services/notification.service';
import { SearchService } from '../../core/services/search.service';

@Component({
  selector: 'app-citizen-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './citizen-layout.component.html',
  styleUrls: ['./citizen-layout.component.scss']
})
export class CitizenLayoutComponent implements OnInit {
  translationService = inject(TranslationService);
  public router = inject(Router);
  el = inject(ElementRef);
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  notificationService = inject(NotificationService);
  searchService = inject(SearchService);

  citizenName = 'Citizen';
  isDashboard = true;
  searchPlaceholder = 'Search...';
  searchQuery = '';

  ngOnInit() {
    this.updateTopBar(this.router.url);
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      this.updateTopBar(event.urlAfterRedirects);
      
      // Reset search query on page navigation
      this.searchQuery = '';
      this.searchService.setSearchQuery('');

      // Close dropdowns on navigate unless password change is forced
      if (!this.isForcedPasswordChange) {
        this.isProfileModalOpen = false;
        this.isNotificationModalOpen = false;
      }
    });

    const isFirstLogin = sessionStorage.getItem('isFirstLogin') === 'true';
    if (isFirstLogin) {
      this.isForcedPasswordChange = true;
      this.isChangePasswordModalOpen = true;
    }

    // Fetch dynamic profile and dashboard stats from DB
    this.authService.fetchProfile().subscribe();
    
    // Subscribe to profile to update top bar UI
    this.authService.profile$.subscribe(profile => {
      if (profile) {
        this.citizenName = profile.FullName || profile.fullName || 'Citizen';
        this.citizenNIC = profile.Nic || profile.nic || sessionStorage.getItem('userNic') || '';
        this.citizenAddress = profile.Address || profile.address || 'No Address Listed';
        this.citizenEmail = profile.Email || profile.email || 'No Email';
        this.citizenPhone = profile.Phone || profile.phone || 'No Phone';
        this.userImageUrl = profile.Profile || profile.profile || '';
      }
    });

    // Fetch notifications
    this.notificationService.fetchNotifications().subscribe();
    this.notificationService.notifications$.subscribe(list => {
      this.notifications = list.map((n: NotificationModel) => ({
        id: n.id,
        type: n.type,
        title: n.title,
        message: n.message,
        time: this.formatTime(n.createdAt),
        read: n.isRead,
        actionUrl: n.actionUrl
      }));
    });
  }

  formatTime(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    const diffMs = new Date().getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMins / 60);
    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins} mins ago`;
    if (diffHours < 24) return `${diffHours} hours ago`;
    return date.toLocaleDateString();
  }

  @HostListener('document:click', ['$event'])
  onClickOutside(event: Event) {
    if (!this.el.nativeElement.contains(event.target)) {
      this.isProfileModalOpen = false;
      this.isNotificationModalOpen = false;
    }
  }

  updateTopBar(url: string) {
    const urlTree = this.router.parseUrl(url);
    const path = urlTree.root.children['primary'] ? urlTree.root.children['primary'].segments.map(s => s.path).join('/') : '';
    
    if (path === 'citizen' || path === 'citizen/dashboard') {
      this.isDashboard = true;
      this.searchPlaceholder = 'Type to search...';
    } else {
      this.isDashboard = false;
      if (path.includes('service-requests')) {
        this.searchPlaceholder = 'Search requests...';
      } else if (path.includes('appointments')) {
        this.searchPlaceholder = 'Search appointments...';
      } else if (path.includes('registry')) {
        this.searchPlaceholder = 'Search registry records...';
      } else if (path.includes('announcements')) {
        this.searchPlaceholder = 'Search announcements...';

      } else if (path.includes('messages')) {
        this.searchPlaceholder = 'Search messages...';
      } else if (path.includes('document-handling')) {
        this.searchPlaceholder = 'Search forms...';
      } else if (path.includes('notifications')) {
        this.searchPlaceholder = 'Search notifications...';
      } else {
        this.searchPlaceholder = 'Search...';
      }
    }
  }
  citizenNIC = '';
  citizenAddress = '';
  citizenEmail = '';
  citizenPhone = '';
  citizenPassword = '••••••••'; // Masked password
  userImageUrl = ''; // Default profile image if empty

  isSidebarExpanded = true;
  isProfileModalOpen = false;
  isEditProfileModalOpen = false;
  isDeleteModalOpen = false;
  isChangeEmailModalOpen = false;
  isChangePasswordModalOpen = false;
  isForcedPasswordChange = false;
  deleteConfirmPassword = '';
  isLoading = false;

  // Form states
  editableEmail = this.citizenEmail;
  editablePhone = this.citizenPhone;
  editablePassword = '';

  // Change Email Form
  newEmail = '';
  currentPassword = '';
  otpValue = '';
  isOtpSent = false;
  emailChangeStep: 'password' | 'email' | 'otp' = 'password';
  resendCountdown = 0;
  resendInterval: any;

  // Change Password Form
  oldPassword = '';
  newPassword = '';
  confirmPassword = '';

  // Custom Modal State
  showCustomModal = false;
  customModalTitle = '';
  customModalMessage = '';
  customModalType: 'success' | 'error' | 'info' = 'info';

  openCustomModal(title: string, message: string, type: 'success' | 'error' | 'info' = 'info') {
    this.customModalTitle = title;
    this.customModalMessage = message;
    this.customModalType = type;
    this.showCustomModal = true;
  }

  closeCustomModal() {
    this.showCustomModal = false;
  }

  isNotificationModalOpen = false;
  activeNotificationFilter = 'all';
  categoryMapping: { [key: string]: string } = {
    'approved': 'Request Approved',
    'rejected': 'Request Rejected',
    'pending': 'Pending Request',
    'appointment': 'Appointment',
    'chat': 'New Message',
    'info': 'System Info'
  };

  notifications: any[] = [];

  toggleSidebar() {
    this.isSidebarExpanded = !this.isSidebarExpanded;
  }

  toggleProfileModal(event?: Event) {
    if (event) event.stopPropagation();
    this.isProfileModalOpen = !this.isProfileModalOpen;
    if (this.isProfileModalOpen) {
      this.isNotificationModalOpen = false;
    }
  }

  toggleEditProfileModal() {
    this.isEditProfileModalOpen = !this.isEditProfileModalOpen;
    if (this.isEditProfileModalOpen) {
      this.isProfileModalOpen = false; // Close small profile dropdown if open
      // Load current values
      this.editableEmail = this.citizenEmail;
      this.editablePhone = this.citizenPhone;
      this.editablePassword = '';
    }
  }

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.userImageUrl = e.target.result;
      };
      reader.readAsDataURL(file);
    }
  }

  removePhoto() {
    this.userImageUrl = '';
  }

  openDeleteModal() {
    this.isDeleteModalOpen = true;
  }

  closeDeleteModal() {
    this.isDeleteModalOpen = false;
  }

  toggleChangeEmailModal() {
    this.isChangeEmailModalOpen = !this.isChangeEmailModalOpen;
    if (this.isChangeEmailModalOpen) {
      this.newEmail = '';
      this.currentPassword = '';
      this.otpValue = '';
      this.isOtpSent = false;
      this.emailChangeStep = 'password';
      this.clearResendTimer();
    } else {
      this.clearResendTimer();
    }
  }

  toggleChangePasswordModal() {
    if (this.isForcedPasswordChange) {
      return;
    }
    this.isChangePasswordModalOpen = !this.isChangePasswordModalOpen;
    if (this.isChangePasswordModalOpen) {
      this.oldPassword = '';
      this.newPassword = '';
      this.confirmPassword = '';
    }
  }

  goToEmailStep() {
    if (!this.currentPassword) {
      this.openCustomModal('Validation Error', 'Please enter your current password to proceed.', 'error');
      return;
    }
    
    const token = sessionStorage.getItem('token');
    if (!token) return;
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    
    this.isLoading = true;
    this.http.post<any>('http://localhost:5058/api/Account/profile/verify-password', {
      password: this.currentPassword
    }, { headers }).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          this.emailChangeStep = 'email';
        } else {
          this.openCustomModal('Verification Failed', res.message || 'Incorrect current password.', 'error');
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.openCustomModal('Verification Error', err.error?.message || 'Incorrect password. Please enter your correct current password.', 'error');
      }
    });
  }

  sendOTP() {
    if (!this.newEmail) {
      this.openCustomModal('Validation Error', 'Please enter a new email address.', 'error');
      return;
    }
    const emailRegex = /^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$/;
    if (!emailRegex.test(this.newEmail)) {
      this.openCustomModal('Validation Error', 'Please enter a valid email address.', 'error');
      return;
    }
    
    if (this.newEmail.toLowerCase() === this.citizenEmail.toLowerCase()) {
      this.openCustomModal('Validation Error', 'New email must be different from your current email.', 'error');
      return;
    }

    if (!this.currentPassword) {
      this.openCustomModal('Validation Error', 'Please enter your current password.', 'error');
      this.emailChangeStep = 'password';
      return;
    }

    const token = sessionStorage.getItem('token');
    if (!token) return;
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    
    this.isLoading = true;
    this.http.post<any>('http://localhost:5058/api/Account/profile/change-email/request', {
      currentPassword: this.currentPassword,
      newEmail: this.newEmail
    }, { headers }).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          this.isOtpSent = true;
          this.emailChangeStep = 'otp';
          this.startResendTimer();
          this.openCustomModal('OTP Sent', 'A verification OTP has been sent to your new email. Please check your inbox.', 'success');
        } else {
          this.openCustomModal('Error', res.message || 'Failed to send OTP.', 'error');
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.openCustomModal('Error', err.error?.message || 'Error occurred while sending OTP.', 'error');
      }
    });
  }

  verifyOTP() {
    if (!this.otpValue) {
      this.openCustomModal('Validation Error', 'Please enter the verification code.', 'error');
      return;
    }
    const token = sessionStorage.getItem('token');
    if (!token) return;
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    this.isLoading = true;
    this.http.post<any>('http://localhost:5058/api/Account/profile/change-email/verify', {
      code: this.otpValue
    }, { headers }).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          this.citizenEmail = this.newEmail;
          this.editableEmail = this.newEmail;

          // Update profile in session storage
          const profileStr = sessionStorage.getItem('userProfile');
          if (profileStr) {
            const profile = JSON.parse(profileStr);
            profile.Email = this.newEmail;
            profile.email = this.newEmail;
            sessionStorage.setItem('userProfile', JSON.stringify(profile));
            this.authService.setProfile(profile);
          }

          this.openCustomModal('Success', 'Email address verified and updated successfully.', 'success');
          this.toggleChangeEmailModal();
        } else {
          this.openCustomModal('Verification Failed', res.message || 'OTP verification failed.', 'error');
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.openCustomModal('Verification Error', err.error?.message || 'Verification error.', 'error');
      }
    });
  }

  updatePassword() {
    if (!this.oldPassword) {
      this.openCustomModal('Validation Error', 'Please enter your current password.', 'error');
      return;
    }
    if (!this.newPassword) {
      this.openCustomModal('Validation Error', 'Please enter your new password.', 'error');
      return;
    }
    if (this.newPassword.length < 6) {
      this.openCustomModal('Validation Error', 'New password must be at least 6 characters long.', 'error');
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.openCustomModal('Validation Error', 'New password and confirm password do not match.', 'error');
      return;
    }
    if (this.oldPassword === this.newPassword) {
      this.openCustomModal('Validation Error', 'New password cannot be the same as your current password.', 'error');
      return;
    }

    const token = sessionStorage.getItem('token');
    if (!token) return;
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    this.isLoading = true;
    this.http.post<any>('http://localhost:5058/api/Account/profile/change-password', {
      currentPassword: this.oldPassword,
      newPassword: this.newPassword
    }, { headers }).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          this.openCustomModal('Success', 'Your password has been successfully updated.', 'success');
          this.isForcedPasswordChange = false;
          sessionStorage.removeItem('isFirstLogin');
          this.isChangePasswordModalOpen = false;
        } else {
          this.openCustomModal('Error', res.message || 'Failed to update password.', 'error');
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.openCustomModal('Error', err.error?.message || 'Password update error.', 'error');
      }
    });
  }

  startResendTimer() {
    this.resendCountdown = 60;
    if (this.resendInterval) {
      clearInterval(this.resendInterval);
    }
    this.resendInterval = setInterval(() => {
      if (this.resendCountdown > 0) {
        this.resendCountdown--;
      } else {
        this.clearResendTimer();
      }
    }, 1000);
  }

  clearResendTimer() {
    if (this.resendInterval) {
      clearInterval(this.resendInterval);
      this.resendInterval = null;
    }
    this.resendCountdown = 0;
  }

  saveProfile() {
    const token = sessionStorage.getItem('token');
    if (!token) return;
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    
    this.isLoading = true;
    this.http.put<any>('http://localhost:5058/api/Account/profile/update', {
      phone: this.editablePhone,
      profile: this.userImageUrl
    }, { headers }).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          this.citizenPhone = this.editablePhone;
          
          const profileStr = sessionStorage.getItem('userProfile');
          if (profileStr) {
            const profile = JSON.parse(profileStr);
            profile.Phone = this.editablePhone;
            profile.phone = this.editablePhone;
            profile.Profile = this.userImageUrl;
            profile.profile = this.userImageUrl;
            sessionStorage.setItem('userProfile', JSON.stringify(profile));
            this.authService.setProfile(profile);
          }
          
          this.openCustomModal('Success', 'Profile updated successfully.', 'success');
          this.toggleEditProfileModal();
        } else {
          this.openCustomModal('Error', res.message || 'Failed to update profile.', 'error');
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.openCustomModal('Error', err.error?.message || 'Error updating profile.', 'error');
      }
    });
  }

  confirmDeleteAccount() {
    if (!this.deleteConfirmPassword) {
      this.openCustomModal('Validation Error', 'Please enter your current password to confirm deletion.', 'error');
      return;
    }

    const token = sessionStorage.getItem('token');
    if (!token) return;
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    this.isLoading = true;
    this.http.post<any>('http://localhost:5058/api/Account/profile/delete', {
      password: this.deleteConfirmPassword
    }, { headers }).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.success) {
          this.isDeleteModalOpen = false;
          this.isEditProfileModalOpen = false;
          this.deleteConfirmPassword = '';
          this.openCustomModal('Success', 'Your account has been deleted successfully. You will now be logged out.', 'success');
          setTimeout(() => {
            this.logout();
          }, 2500);
        } else {
          this.openCustomModal('Error', res.message || 'Failed to delete account.', 'error');
        }
      },
      error: (err) => {
        this.isLoading = false;
        this.openCustomModal('Error', err.error?.message || 'Failed to verify password or delete account.', 'error');
      }
    });
  }

  switchLanguage(lang: string) {
    this.translationService.setLanguage(lang);
  }

  logout() {
    sessionStorage.clear();
    this.authService.setProfile(null);
    this.authService.setStats(null);
    this.router.navigate(['/auth']);
  }

  // Notification Methods
  get filteredNotifications() {
    let filtered = this.notifications;
    if (this.activeNotificationFilter === 'unread') {
      filtered = this.notifications.filter(n => !n.read);
    } else if (this.activeNotificationFilter === 'mention') {
      filtered = this.notifications.filter(n => n.type === 'mention' || n.type === 'chat');
    }
    
    return filtered.map(n => ({
      ...n,
      category: this.categoryMapping[n.type] || 'System Info'
    }));
  }

  get unreadCount() {
    return this.notifications.filter(n => !n.read).length;
  }

  toggleNotificationModal(event?: Event) {
    if (event) event.stopPropagation();
    this.isNotificationModalOpen = !this.isNotificationModalOpen;
    if (this.isNotificationModalOpen) {
      this.isProfileModalOpen = false;
      this.notificationService.fetchNotifications().subscribe();
    }
  }

  setNotificationFilter(filter: string) {
    this.activeNotificationFilter = filter;
  }

  markAllAsRead() {
    this.notificationService.markAllAsRead().subscribe();
  }

  markAsRead(id: number) {
    const notif = this.notifications.find(n => n.id === id);
    if (notif) {
      this.notificationService.markAsRead(id).subscribe();
      if (notif.actionUrl) {
        this.router.navigateByUrl(notif.actionUrl);
        this.isNotificationModalOpen = false;
      }
    }
  }

  onSearchChange() {
    this.searchService.setSearchQuery(this.searchQuery);
  }
}

