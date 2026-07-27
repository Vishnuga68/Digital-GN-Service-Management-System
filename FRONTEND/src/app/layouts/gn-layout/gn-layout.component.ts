import { Component, inject, OnInit, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { filter } from 'rxjs/operators';
import { TranslationService } from '../../core/services/translation.service';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-gn-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './gn-layout.component.html',
  styleUrls: ['./gn-layout.component.scss']
})
export class GnLayoutComponent implements OnInit {
  translationService = inject(TranslationService);
  router = inject(Router);
  el = inject(ElementRef);
  private http = inject(HttpClient);
  private authService = inject(AuthService);

  officerName = 'GN Officer';
  isDashboard = true;
  searchPlaceholder = 'Search...';
  userImageUrl = ''; 
  isVerificationEditor = false; 

  // GN Profile Data
  officerNIC = '';
  officerAddress = '';
  officerEmail = '';
  officerPhone = '';
  officerPassword = '••••••••'; 

  isSidebarExpanded = true;
  isProfileModalOpen = false;
  isEditProfileModalOpen = false;
  isChangeEmailModalOpen = false;
  isChangePasswordModalOpen = false;
  isNotificationModalOpen = false;

  // Form states
  editableEmail = this.officerEmail;
  editablePhone = this.officerPhone;
  editablePassword = '';

  // Change Email Form
  newEmail = '';
  otpValue = '';
  isOtpSent = false;

  // Change Password Form
  oldPassword = '';
  newPassword = '';
  confirmPassword = '';

  activeNotificationFilter = 'all';
  categoryMapping: { [key: string]: string } = {
    'approved': 'Request Approved',
    'rejected': 'Request Rejected',
    'mention': 'New Mention',
    'info': 'System Info'
  };

  notifications = [
    { id: 1, type: 'mention', title: 'New Service Request', message: 'Citizen Saman applied for Residence Certificate.', time: '2 hours ago', read: false },
    { id: 2, type: 'info', title: 'Meeting Reminder', message: 'Appointment with Nalini Silva at 2 PM.', time: '5 hours ago', read: false },
    { id: 3, type: 'mention', title: 'Document Verification', message: 'Waiting for your signature on REQ-1004.', time: 'Yesterday', read: true }
  ];

  ngOnInit() {
    this.updateTopBar(this.router.url);
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      this.updateTopBar(event.urlAfterRedirects);
      this.isProfileModalOpen = false;
      this.isNotificationModalOpen = false;
    });

    // Fetch dynamic GN dashboard data from DB
    const token = sessionStorage.getItem('token');
    const nic = sessionStorage.getItem('userNic') || 'gn1';
    if (token) {
      const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
      this.http.get<any>(`http://localhost:5058/api/Dashboard/gn/${nic}`, { headers }).subscribe(res => {
        if (res.success && res.data) {
          const profile = res.data.profile;
          this.officerName = profile.FullName || profile.fullName || 'GN Officer';
          this.officerNIC = profile.Nic || profile.nic || nic;
          this.officerAddress = `GN Division: ${profile.GnDivisionName || profile.gnDivisionName} (${profile.GnDivisionId || profile.gnDivisionId})`;
          this.officerEmail = profile.Email || profile.email || 'No Email';
          this.officerPhone = profile.Phone || profile.phone || 'No Phone';
          
          sessionStorage.setItem('userProfile', JSON.stringify(profile));
          sessionStorage.setItem('userStats', JSON.stringify(res.data.stats));

          this.authService.setProfile(profile);
          this.authService.setStats(res.data.stats);
        }
      }, err => {
        console.error('[GN Dashboard Error]', err);
      });
    }
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
    
    this.isVerificationEditor = path.includes('verification-editor');

    if (path === 'gn-officer' || path === 'gn-officer/dashboard') {
      this.isDashboard = true;
      this.searchPlaceholder = 'Type to search...';
    } else {
      this.isDashboard = false;
      this.searchPlaceholder = 'Search...';
    }
  }

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
      this.isProfileModalOpen = false;
      this.editableEmail = this.officerEmail;
      this.editablePhone = this.officerPhone;
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

  toggleChangeEmailModal() {
    this.isChangeEmailModalOpen = !this.isChangeEmailModalOpen;
    if (this.isChangeEmailModalOpen) {
      this.newEmail = '';
      this.otpValue = '';
      this.isOtpSent = false;
    }
  }

  toggleChangePasswordModal() {
    this.isChangePasswordModalOpen = !this.isChangePasswordModalOpen;
    if (this.isChangePasswordModalOpen) {
      this.oldPassword = '';
      this.newPassword = '';
      this.confirmPassword = '';
    }
  }

  sendOTP() {
    if (this.newEmail) {
      this.isOtpSent = true;
      console.log('OTP sent to:', this.newEmail);
    }
  }

  verifyOTP() {
    if (this.otpValue === '1234') { 
      this.officerEmail = this.newEmail;
      this.toggleChangeEmailModal();
    }
  }

  updatePassword() {
    if (this.newPassword && this.newPassword === this.confirmPassword) {
      this.officerPassword = this.newPassword;
      this.toggleChangePasswordModal();
    }
  }

  saveProfile() {
    this.officerEmail = this.editableEmail;
    this.officerPhone = this.editablePhone;
    this.toggleEditProfileModal();
  }

  toggleNotificationModal(event?: Event) {
    if (event) event.stopPropagation();
    this.isNotificationModalOpen = !this.isNotificationModalOpen;
    if (this.isNotificationModalOpen) {
      this.isProfileModalOpen = false;
    }
  }

  setNotificationFilter(filter: string) {
    this.activeNotificationFilter = filter;
  }

  markAllAsRead() {
    this.notifications.forEach(n => n.read = true);
  }

  markAsRead(id: number) {
    const notif = this.notifications.find(n => n.id === id);
    if (notif) {
      notif.read = true;
    }
  }

  get filteredNotifications() {
    let filtered = this.notifications;
    if (this.activeNotificationFilter === 'unread') {
      filtered = this.notifications.filter(n => !n.read);
    } else if (this.activeNotificationFilter === 'mention') {
      filtered = this.notifications.filter(n => n.type === 'mention');
    }
    return filtered.map(n => ({
      ...n,
      category: this.categoryMapping[n.type] || 'Uncategorized'
    }));
  }

  get unreadCount() {
    return this.notifications.filter(n => !n.read).length;
  }

  switchLanguage(lang: string) {
    this.translationService.setLanguage(lang);
  }

  logout() {
    this.router.navigate(['/auth']);
  }
}
