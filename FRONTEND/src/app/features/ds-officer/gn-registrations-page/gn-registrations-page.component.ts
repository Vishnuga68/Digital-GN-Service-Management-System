import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';

interface GNOfficer {
  id: string;
  name: string;
  division: string;
  email: string;
  phone: string;
  status: 'Active' | 'Inactive';
  lastLogin: string;
  citizenCount: number;
  activeRequests: number;
  workloadStatus: 'Underutilized' | 'Optimal' | 'Overloaded';
}

interface TransferRequest {
  id: string;
  officerName: string;
  currentDivision: string;
  requestedDivision: string;
  reason: string;
  date: string;
  status: 'Pending' | 'Approved' | 'Rejected';
}

@Component({
  selector: 'app-gn-registrations-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './gn-registrations-page.component.html',
  styleUrls: ['./gn-registrations-page.component.scss']
})
export class GnRegistrationsPageComponent implements OnInit {
  private route = inject(ActivatedRoute);
  activeTab: 'monitoring' | 'transfers' | 'registration' = 'monitoring';

  gnOfficers: GNOfficer[] = [
    {
      id: 'GN-5001',
      name: 'Nimal Perera',
      division: 'J/60',
      email: 'nimal.p@example.com',
      phone: '+94 77 123 4567',
      status: 'Active',
      lastLogin: '2026-04-20 08:30 AM',
      citizenCount: 1250,
      activeRequests: 45,
      workloadStatus: 'Overloaded'
    },
    {
      id: 'GN-5002',
      name: 'Sunil Shantha',
      division: 'M/22',
      email: 'sunil.s@example.com',
      phone: '+94 71 987 6543',
      status: 'Active',
      lastLogin: '2026-04-20 07:15 AM',
      citizenCount: 850,
      activeRequests: 12,
      workloadStatus: 'Optimal'
    },
    {
      id: 'GN-5003',
      name: 'Kamala Silva',
      division: 'N/10',
      email: 'kamala.s@example.com',
      phone: '+94 75 456 7890',
      status: 'Inactive',
      lastLogin: '2026-04-17 04:45 PM',
      citizenCount: 450,
      activeRequests: 5,
      workloadStatus: 'Underutilized'
    }
  ];

  transferRequests: TransferRequest[] = [
    {
      id: 'TR-1001',
      officerName: 'Nimal Perera',
      currentDivision: 'J/60',
      requestedDivision: 'K/12',
      reason: 'Relocation to hometown due to family reasons.',
      date: '2026-04-15',
      status: 'Pending'
    },
    {
      id: 'TR-1002',
      officerName: 'Sunil Shantha',
      currentDivision: 'M/22',
      requestedDivision: 'L/05',
      reason: 'Personal health issues.',
      date: '2026-04-18',
      status: 'Pending'
    }
  ];

  selectedOfficer: GNOfficer | null = null;
  selectedTransfer: TransferRequest | null = null;
  isOfficerDetailsOpen = false;
  isTransferDetailsOpen = false;

  // New GN Form
  newGN = {
    name: '',
    nic: '',
    division: '',
    email: '',
    phone: '',
    tempPassword: ''
  };

  constructor() { }

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      if (params['tab']) {
        this.activeTab = params['tab'] as any;
      }
    });
    this.generateTempPassword();
  }

  setActiveTab(tab: 'monitoring' | 'transfers' | 'registration') {
    this.activeTab = tab;
    if (tab === 'registration') {
      this.generateTempPassword();
    }
  }

  generateTempPassword() {
    this.newGN.tempPassword = 'GN@' + Math.floor(1000 + Math.random() * 9000);
  }

  registerGN() {
    const officer: GNOfficer = {
      id: 'GN-' + Math.floor(6000 + Math.random() * 1000),
      name: this.newGN.name,
      division: this.newGN.division,
      email: this.newGN.email,
      phone: this.newGN.phone,
      status: 'Active',
      lastLogin: 'Never',
      citizenCount: 0,
      activeRequests: 0,
      workloadStatus: 'Underutilized'
    };
    this.gnOfficers.unshift(officer);
    alert(`GN Officer registered successfully.\nTemporary Password: ${this.newGN.tempPassword}`);
    this.setActiveTab('monitoring');
  }

  openOfficerDetails(officer: GNOfficer) {
    this.selectedOfficer = officer;
    this.isOfficerDetailsOpen = true;
  }

  closeOfficerDetails() {
    this.isOfficerDetailsOpen = false;
    this.selectedOfficer = null;
  }

  openTransferDetails(request: TransferRequest) {
    this.selectedTransfer = request;
    this.isTransferDetailsOpen = true;
  }

  closeTransferDetails() {
    this.isTransferDetailsOpen = false;
    this.selectedTransfer = null;
  }

  approveTransfer(id: string) {
    const request = this.transferRequests.find(r => r.id === id);
    if (request) {
      request.status = 'Approved';
      // Simulate automatic division update
      const officer = this.gnOfficers.find(o => o.name === request.officerName);
      if (officer) officer.division = request.requestedDivision;
      
      this.closeTransferDetails();
      alert(`Transfer Approved! Officer ${request.officerName} reassigned to ${request.requestedDivision}.`);
    }
  }

  rejectTransfer(id: string) {
    const request = this.transferRequests.find(r => r.id === id);
    if (request) {
      request.status = 'Rejected';
      this.closeTransferDetails();
    }
  }

  getStatusClass(status: string): string {
    return status.toLowerCase();
  }
}
