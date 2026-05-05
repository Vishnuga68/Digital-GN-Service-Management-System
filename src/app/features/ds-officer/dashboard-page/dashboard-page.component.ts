import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-dashboard-page',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard-page.component.html',
  styleUrls: ['./dashboard-page.component.scss']
})
export class DashboardPageComponent implements OnInit {
  officerName = 'Aruna';
  divisionName = 'Colombo DS Division';
  today = new Date();

  stats = [
    { label: 'Total GN Officers', value: '48', icon: 'assets/icons/registry.png', color: '#3B82F6' },
    { label: 'Active GNs', value: '42', icon: 'assets/icons/home.png', color: '#10B981' },
    { label: 'Pending Transfers', value: '12', icon: 'assets/icons/switch.png', color: '#F59E0B' },
    { label: 'System Alerts', value: '03', icon: 'assets/icons/notification.png', color: '#EF4444' }
  ];

  recentActivities = [
    { id: 1, type: 'Transfer', description: 'GN Nimal Perera requested a transfer to Colombo West.', time: '2 hours ago', status: 'Pending' },
    { id: 2, type: 'Registration', description: 'New GN Officer Sunil Shantha registered for Division J/12.', time: '5 hours ago', status: 'Completed' },
    { id: 3, type: 'Update', description: 'Division boundaries updated for Kaduwela region.', time: 'Yesterday', status: 'Info' }
  ];

  constructor() { }

  ngOnInit(): void {
  }
}
