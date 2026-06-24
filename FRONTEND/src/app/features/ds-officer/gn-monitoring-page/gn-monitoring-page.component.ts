import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

interface GNOfficer {
  id: string;
  name: string;
  division: string;
  status: 'Active' | 'Inactive';
  lastLogin: string;
  citizenCount: number;
  activeRequests: number;
  workloadStatus: 'Underutilized' | 'Optimal' | 'Overloaded';
  email: string;
  phone: string;
  joinedDate: string;
  performanceScore: number;
  pendingAppointments: number;
}

@Component({
  selector: 'app-gn-monitoring-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './gn-monitoring-page.component.html',
  styleUrls: ['./gn-monitoring-page.component.scss']
})
export class GnMonitoringPageComponent implements OnInit {
  gnOfficers: GNOfficer[] = [
    { 
      id: 'GN-5001', 
      name: 'Nimal Perera', 
      division: 'J/60', 
      status: 'Active', 
      lastLogin: '2026-04-20 08:30 AM', 
      citizenCount: 1250, 
      activeRequests: 45, 
      workloadStatus: 'Overloaded',
      email: 'nimal.p@gn.gov.lk',
      phone: '+94 77 123 4567',
      joinedDate: '2020-05-12',
      performanceScore: 92,
      pendingAppointments: 8
    },
    { 
      id: 'GN-5002', 
      name: 'Sunil Shantha', 
      division: 'M/22', 
      status: 'Active', 
      lastLogin: '2026-04-20 07:15 AM', 
      citizenCount: 850, 
      activeRequests: 12, 
      workloadStatus: 'Optimal',
      email: 'sunil.s@gn.gov.lk',
      phone: '+94 71 987 6543',
      joinedDate: '2022-01-15',
      performanceScore: 88,
      pendingAppointments: 3
    },
    { 
      id: 'GN-5003', 
      name: 'Kamala Silva', 
      division: 'N/10', 
      status: 'Inactive', 
      lastLogin: '2026-04-17 04:45 PM', 
      citizenCount: 450, 
      activeRequests: 5, 
      workloadStatus: 'Underutilized',
      email: 'kamala.s@gn.gov.lk',
      phone: '+94 75 456 7890',
      joinedDate: '2018-11-20',
      performanceScore: 75,
      pendingAppointments: 0
    }
  ];

  selectedOfficer: GNOfficer | null = null;
  isModalOpen = false;

  constructor() { }
  ngOnInit(): void { }

  openDetails(officer: GNOfficer) {
    this.selectedOfficer = officer;
    this.isModalOpen = true;
  }

  closeModal() {
    this.isModalOpen = false;
    this.selectedOfficer = null;
  }

  getStatusClass(status: string): string { return status.toLowerCase(); }
}
