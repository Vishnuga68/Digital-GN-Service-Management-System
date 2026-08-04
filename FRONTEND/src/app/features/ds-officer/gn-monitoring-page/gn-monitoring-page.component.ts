import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient, HttpHeaders } from '@angular/common/http';

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
  private http = inject(HttpClient);

  gnOfficers: GNOfficer[] = [];

  selectedOfficer: GNOfficer | null = null;
  isModalOpen = false;

  constructor() { }
  ngOnInit(): void {
    this.fetchMonitoringData();
  }

  fetchMonitoringData() {
    const nic = sessionStorage.getItem('userNic');
    const token = sessionStorage.getItem('token');
    if (nic && token) {
      const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
      this.http.get<any>(`http://localhost:5058/api/Dashboard/ds/${nic}/monitoring`, { headers })
        .subscribe({
          next: (res) => {
            if (res && res.data) {
              this.gnOfficers = res.data;
            }
          },
          error: (err) => {
            console.error('Failed to fetch monitoring data', err);
          }
        });
    }
  }

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
