import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-request-details-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './request-details-page.component.html',
  styleUrls: ['./request-details-page.component.scss']
})
export class RequestDetailsPageComponent implements OnInit {
  isLoading = true;
  requestId: string | null = null;
  requestDetails: any = null;
  updateStatusValue: string = '';
  newRemark: string = '';

  constructor(private route: ActivatedRoute, private router: Router, private http: HttpClient) {}

  ngOnInit(): void {
    this.requestId = this.route.snapshot.paramMap.get('id');
    
    if (this.requestId) {
      this.fetchRequestDetails(this.requestId);
    } else {
      this.isLoading = false;
    }
  }

  fetchRequestDetails(id: string) {
    this.http.get(`http://localhost:5058/api/CitizenRequest/details/${id}`).subscribe({
      next: (data: any) => {
        this.requestDetails = data;
        this.requestDetails.id = data.requestId;
        
        // Ensure currentStep is set correctly for UI based on status
        if (this.requestDetails.currentStatus === 'Pending') this.requestDetails.currentStep = 1;
        else if (this.requestDetails.currentStatus === 'Verified') this.requestDetails.currentStep = 3;
        else if (this.requestDetails.currentStatus === 'Approved' || this.requestDetails.currentStatus === 'Rejected' || this.requestDetails.currentStatus === 'Accepted') this.requestDetails.currentStep = 4;
        else this.requestDetails.currentStep = 2; // Default In Progress

        // Ensure status property exists for UI binding
        this.requestDetails.status = this.requestDetails.currentStatus;
        
        // Ensure documents mapping
        this.requestDetails.documents = data.attachments?.map((a: any) => ({
          name: a.fileName,
          type: a.fileType?.includes('pdf') ? 'pdf' : 'image',
          date: a.uploadedDate,
          size: a.fileSize || 'Unknown',
          url: a.fileUrl?.startsWith('/') ? `http://localhost:5058${a.fileUrl}` : a.fileUrl
        })) || [];

        // Determine applicant info for GN View
        if (data.applicant) {
            this.requestDetails.citizenName = data.applicant.fullName;
            this.requestDetails.citizenNIC = data.applicant.nic;
            this.requestDetails.citizenAddress = data.applicant.address;
        }

        const allowedStatuses = ['In Progress', 'Verified', 'Accepted', 'Rejected'];
        if (allowedStatuses.includes(this.requestDetails.status)) {
            this.updateStatusValue = this.requestDetails.status;
        } else if (this.requestDetails.status === 'Approved') {
            this.updateStatusValue = 'Accepted'; // 'Approved' maps to 'Accepted' in dropdown
        } else {
            this.updateStatusValue = 'In Progress'; // Default placeholder
        }
        
        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error fetching request details', err);
        this.isLoading = false;
      }
    });
  }

  updateRequestStatus() {
    if (!this.requestDetails) return;
    this.requestDetails.status = this.updateStatusValue;
    
    // Auto-update timeline based on status
    if (this.updateStatusValue === 'Pending') {
      this.requestDetails.currentStep = 1;
    } else if (this.updateStatusValue === 'In Progress') {
      this.requestDetails.currentStep = 2;
    } else if (this.updateStatusValue === 'Verified') {
      this.requestDetails.currentStep = 3;
      this.requestDetails.timeline[2].completed = true;
    } else if (this.updateStatusValue === 'Accepted' || this.updateStatusValue === 'Rejected') {
       this.requestDetails.currentStep = 4;
       this.requestDetails.timeline[3].title = this.updateStatusValue === 'Accepted' ? 'Approved' : 'Rejected';
       this.requestDetails.timeline[3].completed = true;
       
       if (this.updateStatusValue === 'Accepted') {
         this.requestDetails.status = 'Approved'; // Normalize for UI badges
         this.requestDetails.finalDocumentUrl = '/assets/dummy-certificate.pdf';
       } else {
         this.requestDetails.rejectionReason = this.newRemark || 'Rejected by GN Officer';
       }
    }

    // Sync timeline based on current status
    this.requestDetails.timeline.forEach((step: any) => {
      // Logic for determining completed status
      if (step.step < this.requestDetails.currentStep) {
        step.completed = true;
      } else if (step.step === this.requestDetails.currentStep) {
        // Special case: Step 3 is completed when status is 'Verified'
        // Step 4 is completed when status is 'Accepted'/'Rejected'
        step.completed = (this.updateStatusValue === 'Verified' && step.step === 3) || 
                         ((this.updateStatusValue === 'Accepted' || this.updateStatusValue === 'Rejected') && step.step === 4);
      } else {
        // Clear future steps if moving backward (e.g., from Verified to In Progress)
        step.completed = false;
        // Reset Step 4 title if it was previously set to Approved/Rejected
        if (step.step === 4) step.title = 'Final Decision';
      }
    });

    if (this.newRemark.trim()) {
       this.requestDetails.remarks.unshift({
           author: 'GN Officer',
           role: 'Reviewer',
           text: this.newRemark,
           date: new Date()
       });
       this.newRemark = '';
    }
  }

  goBack() {
    this.router.navigate(['/gn-officer/requests']);
  }
}
