import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { ReactiveFormsModule, FormsModule } from '@angular/forms';
import { TranslationService } from '../../../core/services/translation.service';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [CommonModule, RouterModule, BaseChartDirective, FormsModule],
  templateUrl: './dashboard-page.component.html',
  styleUrls: ['./dashboard-page.component.scss']
})
export class DashboardPageComponent implements OnInit {
  translationService = inject(TranslationService);
  router = inject(Router);
  private authService = inject(AuthService);

  citizenName = 'Loading...';
  pendingRequests = 0;
  upcomingMeetings = 0;
  newMessages = 0;

  // Mock Profile Data
  citizenNIC = '';
  citizenAddress = '';
  citizenEmail = '';
  citizenPhone = '';
  citizenPassword = '••••••••'; // Sample masked password
  userImageUrl = ''; // Default profile image if empty
  gnDivision = 'Loading...';
  gnOfficer = 'Loading...';

  updateProfile(profile: any) {
    this.citizenName = profile.FullName || profile.fullName || 'Citizen';
    this.citizenNIC = profile.Nic || profile.nic || '';
    this.citizenAddress = profile.Address || profile.address || 'No Address Listed';
    this.citizenEmail = profile.Email || profile.email || 'No Email';
    this.citizenPhone = profile.Phone || profile.phone || 'No Phone';
    this.gnDivision = profile.GnDivisionId || profile.gnDivisionId || 'Unknown Division';
    this.gnOfficer = profile.GnOfficerName || profile.gnOfficerName || 'Unassigned';
  }

  updateStats(stats: any) {
    this.pendingRequests = stats.PendingRequests || stats.pendingRequests || 0;
    
    const approved = stats.ApprovedRequests || stats.approvedRequests || 0;
    const pending = stats.PendingRequests || stats.pendingRequests || 0;
    const total = stats.TotalRequests || stats.totalRequests || 0;
    const other = Math.max(0, total - approved - pending);
    
    // Update charts dynamically based on real data
    this.pieChartData = {
      labels: [ 'Pending Requests', 'Approved', 'Other/Completed' ],
      datasets: [ {
        data: [ pending, approved, other ],
        backgroundColor: ['#FEF3C7', '#D1FAE5', '#FEE2E2'],
        hoverBackgroundColor: ['#FDE68A', '#A7F3D0', '#FECACA'],
        borderColor: ['#D97706', '#059669', '#DC2626']
      } ]
    };
    
    this.pieChartAnalysis = [
      { label: 'Pending Requests', value: pending, percentage: total > 0 ? (pending * 100 / total).toFixed(1) + '%' : '0%', color: '#D97706' },
      { label: 'Approved', value: approved, percentage: total > 0 ? (approved * 100 / total).toFixed(1) + '%' : '0%', color: '#059669' },
      { label: 'Other/Completed', value: other, percentage: total > 0 ? (other * 100 / total).toFixed(1) + '%' : '0%', color: '#DC2626' }
    ];
  }

  ngOnInit() {
    const profileStr = sessionStorage.getItem('userProfile');
    const statsStr = sessionStorage.getItem('userStats');
    
    if (profileStr) {
      this.updateProfile(JSON.parse(profileStr));
    }
    
    if (statsStr) {
      this.updateStats(JSON.parse(statsStr));
    }

    this.authService.profile$.subscribe(profile => {
      if (profile) {
        this.updateProfile(profile);
      }
    });

    this.authService.stats$.subscribe(stats => {
      if (stats) {
        this.updateStats(stats);
      }
    });
  }

  recentActivities = [
    { date: '2025-10-12', action: 'Applied for Residence Certificate', status: 'Pending', detail: 'Waiting for GN approval' },
    { date: '2025-10-10', action: 'Booked Appointment', status: 'Approved', detail: 'Meeting on 15th Oct' },
    { date: '2025-10-05', action: 'Profile Update', status: 'Completed', detail: 'Phone number updated' }
  ];

  // Doughnut Chart Configuration
  public pieChartOptions: any = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false }
    },
    cutout: '70%'
  };
  public pieChartData: ChartData<'doughnut', number[], string | string[]> = {
    labels: [ 'Pending Requests', 'Approved', 'Rejected' ],
    datasets: [ {
      data: [ 10, 12, 4 ],
      backgroundColor: ['#FEF3C7', '#D1FAE5', '#FEE2E2'],
      hoverBackgroundColor: ['#FDE68A', '#A7F3D0', '#FECACA'],
      borderColor: ['#D97706', '#059669', '#DC2626']
    } ]
  };
  public pieChartType: ChartType = 'doughnut';

  // Custom analysis data for pie chart
  public pieChartAnalysis = [
    { label: 'Pending Requests', value: 10, percentage: '38.5%', color: '#D97706' },
    { label: 'Approved', value: 12, percentage: '46.1%', color: '#059669' },
    { label: 'Rejected', value: 4, percentage: '15.4%', color: '#DC2626' }
  ];

  // Bar Chart Configuration
  public barChartOptions: any = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false }
    },
    scales: { x: {}, y: { beginAtZero: true, ticks: { stepSize: 1 } } }
  };
  public barChartData: ChartData<'bar'> = {
    labels: [ 'Mon', 'Tue', 'Wed', 'Thu', 'Fri' ],
    datasets: [
      { data: [ 1, 2, 0, 3, 1 ], label: 'Activities', backgroundColor: '#3B82F6', borderRadius: 6 }
    ]
  };
  public barChartType: ChartType = 'bar';

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

  toggleProfileModal() {
    this.isProfileModalOpen = !this.isProfileModalOpen;
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

  toggleNotificationModal() {
    this.isNotificationModalOpen = !this.isNotificationModalOpen;
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
