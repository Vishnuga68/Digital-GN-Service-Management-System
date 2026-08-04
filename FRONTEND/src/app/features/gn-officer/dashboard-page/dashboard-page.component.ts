import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-gn-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, BaseChartDirective],
  templateUrl: './dashboard-page.component.html',
  styleUrls: ['./dashboard-page.component.scss']
})
export class DashboardPageComponent implements OnInit {
  public router = inject(Router);
  private authService = inject(AuthService);
  
  officerName = 'Loading...';
  divisionName = 'Loading...';
  divisionID = '';
  userImageUrl = '';

  summaryCards = [
    { title: 'Total Family Cards', value: '0', color: 'primary' },
    { title: 'Pending Requests', value: '0', color: 'warning' },
    { title: 'Total Citizens', value: '0', color: 'success' },
    { title: 'Total Service Requests', value: '0', color: 'info' }
  ];

  recentRequests = [
    { id: 'REQ-1001', name: 'Saman Perera', type: 'Income Certificate', status: 'Pending', date: '2026-03-27' },
    { id: 'REQ-1002', name: 'Nalini Silva', type: 'Residence Certificate', status: 'Verified', date: '2026-03-26' },
    { id: 'REQ-1003', name: 'Kamal Bandara', type: 'Character Certificate', status: 'Pending', date: '2026-03-25' },
    { id: 'REQ-1004', name: 'Priya Rajan', type: 'Voter Registration', status: 'Verified', date: '2026-03-24' },
    { id: 'REQ-1005', name: 'Aruna Shantha', type: 'ID Card Application', status: 'Rejected', date: '2026-03-23' }
  ];

  // Pie Chart: Request Types Breakdown
  public pieChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { display: false }
    }
  };
  public pieChartData: ChartData<'pie', number[], string | string[]> = {
    labels: ['Registration', 'Document Request', 'Document Verification', 'Complaints', 'Other'],
    datasets: [{
      data: [35, 25, 20, 10, 10],
      backgroundColor: ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#64748B'],
      hoverBackgroundColor: ['#2563EB', '#059669', '#D97706', '#DC2626', '#475569'],
    }]
  };
  public pieChartType: ChartType = 'pie';

  // Custom analysis data for pie chart (used for the legend)
  public pieChartAnalysis = [
    { label: 'Registration', value: 35, percentage: '35%', color: '#3B82F6' },
    { label: 'Document Request', value: 25, percentage: '25%', color: '#10B981' },
    { label: 'Document Verification', value: 20, percentage: '20%', color: '#F59E0B' },
    { label: 'Complaints', value: 10, percentage: '10%', color: '#EF4444' },
    { label: 'Other', value: 10, percentage: '10%', color: '#64748B' }
  ];

  // Bar Chart: Weekly Request Summary
  public barChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    scales: {
      x: { grid: { display: false } },
      y: { beginAtZero: true, ticks: { stepSize: 5 } }
    },
    plugins: {
      legend: { display: false }
    }
  };
  public barChartData: ChartData<'bar'> = {
    labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
    datasets: [
      { data: [12, 19, 8, 15, 22, 5, 3], label: 'Requests', backgroundColor: '#3B82F6', borderRadius: 4 }
    ]
  };
  public barChartType: ChartType = 'bar';

  updateProfile(profile: any) {
    this.officerName = profile.FullName || profile.fullName || 'GN Officer';
    this.divisionName = profile.GnDivisionName || profile.gnDivisionName || 'Division';
    this.divisionID = profile.GnDivisionId || profile.gnDivisionId || '';
    this.userImageUrl = profile.Profile || profile.profile || '';
  }

  updateStats(stats: any) {
    this.summaryCards = [
      { title: 'Total Family Cards', value: stats.TotalFamilyCards?.toString() || '0', color: 'primary' },
      { title: 'Pending Requests', value: stats.PendingRequests?.toString() || '0', color: 'warning' },
      { title: 'Total Citizens', value: stats.TotalCitizens?.toString() || '0', color: 'success' },
      { title: 'Total Service Requests', value: stats.TotalRequests?.toString() || '0', color: 'info' }
    ];

    const registry = stats.RegistryUpdates || 0;
    const docSvc = stats.DocumentServices || 0;
    const welfare = stats.WelfareAssistance || 0;
    const complaints = stats.ComplaintsGrievances || 0;
    const general = stats.GeneralRequests || 0;
    const total = registry + docSvc + welfare + complaints + general;

    this.pieChartData = {
      labels: ['Registry Updates', 'Document Services', 'Welfare & Assistance', 'Complaints & Grievances', 'General Requests'],
      datasets: [{
        data: [registry, docSvc, welfare, complaints, general],
        backgroundColor: ['#3B82F6', '#10B981', '#F59E0B', '#EF4444', '#64748B'],
        hoverBackgroundColor: ['#2563EB', '#059669', '#D97706', '#DC2626', '#475569'],
      }]
    };

    this.pieChartAnalysis = [
      { label: 'Registry Updates', value: registry, percentage: total > 0 ? (registry * 100 / total).toFixed(1) + '%' : '0%', color: '#3B82F6' },
      { label: 'Document Services', value: docSvc, percentage: total > 0 ? (docSvc * 100 / total).toFixed(1) + '%' : '0%', color: '#10B981' },
      { label: 'Welfare & Assistance', value: welfare, percentage: total > 0 ? (welfare * 100 / total).toFixed(1) + '%' : '0%', color: '#F59E0B' },
      { label: 'Complaints & Grievances', value: complaints, percentage: total > 0 ? (complaints * 100 / total).toFixed(1) + '%' : '0%', color: '#EF4444' },
      { label: 'General Requests', value: general, percentage: total > 0 ? (general * 100 / total).toFixed(1) + '%' : '0%', color: '#64748B' }
    ];

    const weeklySummary = stats.WeeklyRequestSummary || [0, 0, 0, 0, 0, 0, 0];
    const weeklyLabels = stats.WeeklyRequestSummaryLabels || stats.weeklyRequestSummaryLabels || ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
    this.barChartData = {
      labels: weeklyLabels,
      datasets: [
        { ...this.barChartData.datasets[0], data: weeklySummary }
      ]
    };

    if (stats.RecentRequests) {
      this.recentRequests = stats.RecentRequests.map((r: any) => ({
        id: r.id ?? r.Id,
        name: r.name ?? r.Name,
        type: r.type ?? r.Type,
        status: r.status ?? r.Status,
        date: r.date ?? r.Date
      }));
    } else {
      this.recentRequests = [];
    }
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
