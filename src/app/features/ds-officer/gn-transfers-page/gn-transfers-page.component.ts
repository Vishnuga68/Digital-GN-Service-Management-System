import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

interface TransferRequest {
  id: string;
  officerName: string;
  currentDivision: string;
  requestedDivision: string;
  reason: string;
  date: string;
  status: 'Pending' | 'Approved' | 'Rejected';
  documentUrl?: string;
  documentName?: string;
}

@Component({
  selector: 'app-gn-transfers-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './gn-transfers-page.component.html',
  styleUrls: ['./gn-transfers-page.component.scss']
})
export class GnTransfersPageComponent implements OnInit {
  requests: TransferRequest[] = [
    { 
      id: 'TR-1001', 
      officerName: 'Nimal Perera', 
      currentDivision: 'J/60', 
      requestedDivision: 'K/12', 
      reason: 'Relocation to hometown due to family reasons.', 
      date: '2026-04-15', 
      status: 'Pending',
      documentUrl: 'assets/docs/transfer_request_nimal.pdf',
      documentName: 'Transfer_Request_Form.pdf'
    },
    { 
      id: 'TR-1002', 
      officerName: 'Sunil Shantha', 
      currentDivision: 'M/22', 
      requestedDivision: 'L/05', 
      reason: 'Personal health issues requiring specialist care nearby.', 
      date: '2026-04-18', 
      status: 'Pending',
      documentUrl: 'assets/docs/medical_report_sunil.pdf',
      documentName: 'Medical_Support_Docs.pdf'
    }
  ];

  selectedRequest: TransferRequest | null = null;
  isModalOpen = false;

  constructor() { }
  ngOnInit(): void { }

  openDetails(request: TransferRequest) {
    this.selectedRequest = request;
    this.isModalOpen = true;
  }

  viewDocument(url?: string) {
    if (url) {
      window.open(url, '_blank');
    } else {
      alert('No document attached to this request.');
    }
  }

  closeModal() {
    this.isModalOpen = false;
    this.selectedRequest = null;
  }

  approveRequest(id: string) {
    const req = this.requests.find(r => r.id === id);
    if (req) {
      req.status = 'Approved';
      alert(`Transfer for ${req.officerName} approved!`);
      this.closeModal();
    }
  }

  rejectRequest(id: string) {
    const req = this.requests.find(r => r.id === id);
    if (req) {
      req.status = 'Rejected';
      this.closeModal();
    }
  }

  getStatusClass(status: string): string { return status.toLowerCase(); }
}
