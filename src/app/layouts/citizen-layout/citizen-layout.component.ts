import { Component, inject, OnInit, HostListener, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router, NavigationEnd } from '@angular/router';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { filter } from 'rxjs/operators';
import { TranslationService } from '../../core/services/translation.service';

@Component({
  selector: 'app-citizen-layout',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  templateUrl: './citizen-layout.component.html',
  styleUrls: ['./citizen-layout.component.scss']
})
export class CitizenLayoutComponent implements OnInit {
  translationService = inject(TranslationService);
  router = inject(Router);
  el = inject(ElementRef);

  citizenName = 'Citizen Nimal';
  isDashboard = true;
  searchPlaceholder = 'Search...';

  ngOnInit() {
    this.updateTopBar(this.router.url);
    this.router.events.pipe(
      filter(event => event instanceof NavigationEnd)
    ).subscribe((event: any) => {
      this.updateTopBar(event.urlAfterRedirects);
      // Close dropdowns on navigate
      this.isProfileModalOpen = false;
      this.isNotificationModalOpen = false;
    });
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
      } else if (path.includes('downloads')) {
        this.searchPlaceholder = 'Search forms...';
      } else if (path.includes('notifications')) {
        this.searchPlaceholder = 'Search notifications...';
      } else {
        this.searchPlaceholder = 'Search...';
      }
    }
  }
    // Mock Profile Data
  citizenNIC = '199812345678';
  citizenAddress = 'No. 123, Galle Road, Colombo 03, Sri Lanka';
  citizenEmail = 'nimal.citizen@example.com';
  citizenPhone = '+94 77 123 4567';
  citizenPassword = 'password123'; // Sample masked password
  userImageUrl = ''; // Default profile image if empty

    isSidebarExpanded = true;
  isProfileModalOpen = false;
  isEditProfileModalOpen = false;
  isDeleteModalOpen = false;
  isChangeEmailModalOpen = false;
  isChangePasswordModalOpen = false;

  // Form states
  editableEmail = this.citizenEmail;
  editablePhone = this.citizenPhone;
  editablePassword = '';

  // Change Email Form
  newEmail = '';
  otpValue = '';
  isOtpSent = false;

  // Change Password Form
  oldPassword = '';
  newPassword = '';
  confirmPassword = '';

  isNotificationModalOpen = false;
  activeNotificationFilter = 'all';
  categoryMapping: { [key: string]: string } = {
    'approved': 'Request Approved',
    'rejected': 'Request Rejected',
    'mention': 'New Mention',
    'info': 'System Info'
  };

  notifications = [
    {
      id: 1,
      type: 'approved',
      title: 'Income Certificate Approved',
      message: 'Your income certificate request has been approved.',
      time: '2 hours ago',
      read: false
    },
    {
      id: 2,
      type: 'rejected',
      title: 'Residence Certificate Rejected',
      message: 'Your request was rejected due to missing documents.',
      time: '5 hours ago',
      read: false
    },
    {
      id: 3,
      type: 'mention',
      title: 'You were mentioned in a request',
      message: 'GN officer mentioned you in Request #1023.',
      time: 'Yesterday',
      read: true
    },
    {
      id: 4,
      type: 'info',
      title: 'System Maintenance Notice',
      message: 'The system will be unavailable on Sunday from 10 PM.',
      time: '2 days ago',
      read: true
    }
  ];

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
    if (this.otpValue === '1234') { // Mock verification
      this.citizenEmail = this.newEmail;
      this.toggleChangeEmailModal();
      console.log('Email updated successfully');
    }
  }

  updatePassword() {
    if (this.newPassword && this.newPassword === this.confirmPassword) {
      this.citizenPassword = this.newPassword;
      this.toggleChangePasswordModal();
      console.log('Password updated successfully');
    }
  }

  saveProfile() {
    // Mock save logic
    this.citizenEmail = this.editableEmail;
    this.citizenPhone = this.editablePhone;
    console.log('Profile saved successfully');
    this.toggleEditProfileModal();
  }

  confirmDeleteAccount() {
    // Mock delete logic
    console.log('Account deleted permanently');
    this.isDeleteModalOpen = false;
    this.isEditProfileModalOpen = false;
    this.logout();
  }

  switchLanguage(lang: string) {
    this.translationService.setLanguage(lang);
  }

  logout() {
    this.router.navigate(['/auth']);
  }

  // Notification Methods
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
}
