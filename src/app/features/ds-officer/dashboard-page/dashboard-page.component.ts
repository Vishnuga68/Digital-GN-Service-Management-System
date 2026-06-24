import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard-page.component.html',
  styleUrls: ['./dashboard-page.component.scss']
})
export class DashboardPageComponent implements OnInit {
  private authService = inject(AuthService);

  officerName = 'Loading...';
  divisionName = 'Loading...';
  divisionId = '';
  today = new Date();

  stats = [
    { label: 'Total GN Officers', value: '0', icon: 'assets/icons/registry.png', color: '#3B82F6' },
    { label: 'Total GN Divisions', value: '0', icon: 'assets/icons/home.png', color: '#10B981' },
    { label: 'Total Service Requests', value: '0', icon: 'assets/icons/switch.png', color: '#F59E0B' },
    { label: 'Total Registered Citizens', value: '0', icon: 'assets/icons/notification.png', color: '#EF4444' }
  ];

  recentActivities = [
    { id: 1, type: 'Transfer', description: 'GN Nimal Perera requested a transfer to Colombo West.', time: '2 hours ago', status: 'Pending' },
    { id: 2, type: 'Registration', description: 'New GN Officer Sunil Shantha registered for Division J/12.', time: '5 hours ago', status: 'Completed' },
    { id: 3, type: 'Update', description: 'Division boundaries updated for Kaduwela region.', time: 'Yesterday', status: 'Info' }
  ];

  constructor() { }

  updateProfile(profile: any) {
    this.officerName = profile.FullName || profile.fullName || 'DS Officer';
    this.divisionName = profile.DsDivisionName || profile.dsDivisionName || 'DS Division';
    this.divisionId = profile.DsDivisionId || profile.dsDivisionId || '';
  }

  updateStats(stats: any) {
    this.stats = [
      { label: 'Total GN Officers', value: stats.TotalGnOfficers?.toString() || '0', icon: 'assets/icons/registry.png', color: '#3B82F6' },
      { label: 'Total GN Divisions', value: stats.TotalGnDivisions?.toString() || '0', icon: 'assets/icons/home.png', color: '#10B981' },
      { label: 'Total Service Requests', value: stats.TotalRequests?.toString() || '0', icon: 'assets/icons/switch.png', color: '#F59E0B' },
      { label: 'Total Registered Citizens', value: stats.TotalCitizens?.toString() || '0', icon: 'assets/icons/notification.png', color: '#EF4444' }
    ];
  }

  ngOnInit(): void {
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
}
