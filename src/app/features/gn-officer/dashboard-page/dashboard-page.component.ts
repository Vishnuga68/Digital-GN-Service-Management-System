import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartData, ChartType } from 'chart.js';

@Component({
  selector: 'app-gn-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, BaseChartDirective],
  templateUrl: './dashboard-page.component.html',
  styleUrls: ['./dashboard-page.component.scss']
})
export class DashboardPageComponent implements OnInit {
  public router = inject(Router);
  
  officerName = 'Nimal Perera';
  divisionName = 'Puliyankoodal';
  divisionID = 'J/60';

  summaryCards = [
    { title: 'Total Families', value: '450', color: 'primary' },
    { title: 'Pending Requests', value: '12', color: 'warning' },
    { title: 'Verified Documents', value: '1,240', color: 'success' },
    { title: 'Complaints', value: '05', color: 'danger' }
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

  ngOnInit(): void {}
}
