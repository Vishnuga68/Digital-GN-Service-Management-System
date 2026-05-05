import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

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
  selector: 'app-gn-change-requests-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './gn-change-requests-page.component.html',
  styleUrls: ['./gn-change-requests-page.component.scss']
})
export class GnChangeRequestsPageComponent implements OnInit {
  requests: TransferRequest[] = [
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
      reason: 'Personal health issues requiring proximity to specialized medical care.',
      date: '2026-04-18',
      status: 'Pending'
    },
    {
      id: 'TR-1003',
      officerName: 'Kamala Silva',
      currentDivision: 'N/10',
      requestedDivision: 'P/03',
      reason: 'Standard rotational transfer request.',
      date: '2026-04-10',
      status: 'Approved'
    }
  ];

  selectedRequest: TransferRequest | null = null;
  isViewModalOpen = false;

  constructor() { }

  ngOnInit(): void { }

  openViewModal(request: TransferRequest) {
    this.selectedRequest = request;
    this.isViewModalOpen = true;
  }

  closeViewModal() {
    this.isViewModalOpen = false;
    this.selectedRequest = null;
  }

  approveRequest(id: string) {
    const request = this.requests.find(r => r.id === id);
    if (request) {
      request.status = 'Approved';
      console.log(`System: GN Officer ${request.officerName}'s division updated from ${request.currentDivision} to ${request.requestedDivision} automatically.`);
      this.closeViewModal();
      alert(`Transfer Approved! Officer ${request.officerName} has been reassigned to ${request.requestedDivision}.`);
    }
  }

  rejectRequest(id: string) {
    const request = this.requests.find(r => r.id === id);
    if (request) {
      request.status = 'Rejected';
      this.closeViewModal();
    }
  }

  getStatusClass(status: string): string {
    return status.toLowerCase();
  }
}
