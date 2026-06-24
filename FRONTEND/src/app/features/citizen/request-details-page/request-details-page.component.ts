import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-request-details-page',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './request-details-page.component.html',
  styleUrls: ['./request-details-page.component.scss']
})
export class RequestDetailsPageComponent implements OnInit {
  isLoading = true;
  requestId: string | null = null;
  requestDetails: any = null;

  constructor(private route: ActivatedRoute, private router: Router) {}

  ngOnInit(): void {
    this.requestId = this.route.snapshot.paramMap.get('id');
    
    // Simulate loading details
    setTimeout(() => {
      this.loadMockData(this.requestId);
      this.isLoading = false;
    }, 1200);
  }

  loadMockData(id: string | null) {
    // Generate mock details based on whatever ID is passed
    const isApproved = id === 'REQ-2023-001' || id === 'REQ-2023-005';
    const isRejected = id === 'REQ-2023-004';
    const isPending = id === 'REQ-2023-003' || id === 'REQ-2023-006';
    
    let currentStatus = 'In Progress';
    if (isApproved) currentStatus = 'Approved';
    if (isRejected) currentStatus = 'Rejected';
    if (isPending) currentStatus = 'Pending';

    let currentStep = 2; // Default to 'In Progress' phase
    if (currentStatus === 'Pending') currentStep = 1;
    if (currentStatus === 'Approved' || currentStatus === 'Rejected') currentStep = 4;

    this.requestDetails = {
      id: id || 'REQ-UNKNOWN',
      serviceType: 'Income Certificate',
      category: 'Certificates',
      description: 'Requesting an income certificate for university scholarship application purposes. Required for the year 2023.',
      submittedDate: new Date('2023-10-01T10:00:00'),
      lastUpdated: new Date('2023-10-05T14:30:00'),
      status: currentStatus,
      citizenName: 'Nimal Perera',
      citizenNIC: '199012345678',
      citizenAddress: 'No. 123, Galle Road, Colombo 03',
      
      // Progress Timeline
      currentStep: currentStep,
      timeline: [
        { step: 1, title: 'Submitted', timestamp: new Date('2023-10-01T10:00:00'), completed: true },
        { step: 2, title: 'Under Review', timestamp: currentStep >= 2 ? new Date('2023-10-02T11:15:00') : null, completed: currentStep >= 2, officer: currentStep >= 2 ? 'KMP' : null },
        { step: 3, title: 'Verified', timestamp: currentStep >= 3 ? new Date('2023-10-03T09:30:00') : null, completed: currentStep >= 3, officer: currentStep >= 3 ? 'KMP' : null },
        { 
          step: 4, 
          title: currentStatus === 'Rejected' ? 'Rejected' : (currentStatus === 'Approved' ? 'Approved' : 'Final Decision'), 
          timestamp: currentStep === 4 ? new Date('2023-10-05T14:30:00') : null, 
          completed: currentStep === 4, 
          officer: currentStep === 4 ? 'JD' : null 
        }
      ],

      // Attached Documents
      documents: [
        { name: 'National_ID_Front_Back.pdf', type: 'pdf', date: new Date('2023-10-01T10:00:00'), size: '1.2 MB' },
        { name: 'Salary_Slip_Last_3_Months.png', type: 'image', date: new Date('2023-10-01T10:02:00'), size: '2.5 MB' }
      ],

      // Remarks/Comments
      remarks: [
        { author: 'GN Officer', role: 'Reviewer', text: 'All documents are present. Forwarding to DS for final approval.', date: new Date('2023-10-03T09:35:00') }
      ],
      
      // Outcome
      rejectionReason: currentStatus === 'Rejected' ? 'The provided salary slips are illegible. Please re-apply with clear scans.' : null,
      finalDocumentUrl: currentStatus === 'Approved' ? '/assets/dummy-certificate.pdf' : null
    };

    if (currentStatus === 'Rejected') {
      this.requestDetails.remarks.push({ author: 'Divisional Sec.', role: 'Approver', text: 'Rejected due to unclear documents.', date: new Date('2023-10-05T14:30:00') });
    } else if (currentStatus === 'Approved') {
      this.requestDetails.remarks.push({ author: 'Divisional Sec.', role: 'Approver', text: 'Approved. Certificate generated successfully.', date: new Date('2023-10-05T14:30:00') });
    }
  }

  goBack() {
    this.router.navigate(['/citizen/service-requests']);
  }
}
