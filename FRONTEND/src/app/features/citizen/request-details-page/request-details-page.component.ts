import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';

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
  isSimpleRequestFlow = false;

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
    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    this.http.get(`http://localhost:5058/api/CitizenRequest/details/${id}`, { headers }).subscribe({
      next: (data: any) => {
        this.requestDetails = data;
        this.requestDetails.id = data.requestId;
        
        this.isSimpleRequestFlow = [44, 45, 46].includes(data.categoryId);

        // Map the backend status to display status and step number
        const status = this.requestDetails.currentStatus;
        const statusUpper = status ? status.toUpperCase() : '';
        let effectiveStatus = statusUpper;
        if (this.isSimpleRequestFlow && this.requestDetails.approvedDate) {
          effectiveStatus = 'ACCEPTED';
        }

        let step = 2; // Default In Progress
        if (effectiveStatus === 'SUBMITTED' || effectiveStatus === 'PENDING') {
          step = 1;
        } else if (effectiveStatus === 'UNDER_REVIEW' || effectiveStatus === 'CHANGES_REQUESTED' || effectiveStatus === 'RESUBMITTED' || effectiveStatus === 'IN_PROGRESS' || effectiveStatus === 'IN PROGRESS') {
          step = 2;
        } else if (effectiveStatus === 'VERIFIED') {
          step = 3;
        } else if (effectiveStatus === 'APPROVED' || effectiveStatus === 'REJECTED' || effectiveStatus === 'ACCEPTED' || effectiveStatus === 'REVOKED') {
          step = 4;
        }
        this.requestDetails.currentStep = step;

        const statusMap: any = {
          'PENDING': 'Pending',
          'Pending': 'Pending',
          'SUBMITTED': 'Pending',
          'Submitted': 'Pending',
          'UNDER_REVIEW': 'Under Review',
          'Under Review': 'Under Review',
          'CHANGES_REQUESTED': 'Changes Requested',
          'Changes Requested': 'Changes Requested',
          'RESUBMITTED': 'Resubmitted',
          'Resubmitted': 'Resubmitted',
          'VERIFIED': 'Verified',
          'Verified': 'Verified',
          'APPROVED': 'Approved',
          'Approved': 'Approved',
          'ACCEPTED': 'Accepted',
          'Accepted': 'Accepted',
          'REJECTED': 'Rejected',
          'Rejected': 'Rejected',
          'REVOKED': 'Revoked',
          'Revoked': 'Revoked'
        };
        this.requestDetails.status = (this.isSimpleRequestFlow && this.requestDetails.approvedDate) ? 'Accepted' : (statusMap[status] || status);

        if (this.requestDetails.timeline) {
          this.requestDetails.timeline.forEach((t: any) => {
            t.completed = t.step <= step;
            if (t.step === 2 && !t.timestamp && step >= 2) {
              t.timestamp = this.requestDetails.lastUpdated;
            }
            if (t.step === 3 && !t.timestamp && step >= 3) {
              t.timestamp = this.requestDetails.lastUpdated;
            }
            if (t.step === 4 && !t.timestamp && step >= 4) {
              t.timestamp = this.requestDetails.approvedDate ? this.requestDetails.approvedDate : (this.requestDetails.rejectedDate ? this.requestDetails.rejectedDate : this.requestDetails.lastUpdated);
            }
          });
        }

        // Map rejectionReason dynamically for display
        const rejectEntry = this.requestDetails.timeline?.find((t: any) => t.step === 4 && (t.title?.toLowerCase() === 'rejected' || t.title?.toLowerCase() === 'revoked'));
        if (rejectEntry && rejectEntry.remarks) {
          this.requestDetails.rejectionReason = rejectEntry.remarks;
        } else {
          const statusUpper2 = this.requestDetails.status ? this.requestDetails.status.toUpperCase() : '';
          if ((statusUpper2 === 'REJECTED' || statusUpper2 === 'REVOKED') && this.requestDetails.remarks && this.requestDetails.remarks.length > 0) {
            this.requestDetails.rejectionReason = this.requestDetails.remarks[0].text;
          }
        }
        
        // Ensure documents mapping
        this.requestDetails.documents = data.attachments?.map((a: any) => ({
          name: a.fileName,
          type: a.fileType?.includes('pdf') ? 'pdf' : 'image',
          date: a.uploadedDate,
          size: a.fileSize || 'Unknown',
          url: a.fileUrl?.startsWith('/') ? `http://localhost:5058${a.fileUrl}` : a.fileUrl,
          documentRole: a.documentRole,
          isPrimary: a.isPrimary,
          isActive: a.isActive
        })) || [];

        // Distinguish main and supporting documents
        this.requestDetails.mainDocument = this.requestDetails.documents.find((d: any) => d.documentRole === 'MAIN_DOCUMENT' && d.isActive);
        this.requestDetails.supportingDocuments = this.requestDetails.documents.filter((d: any) => {
          if (d.documentRole === 'MAIN_DOCUMENT' || d.documentRole === 'GENERATED_CERTIFICATE' || d.documentRole === 'FINAL_VERIFIED_DOCUMENT') return false;
          return d.isActive;
        });

        this.isLoading = false;
      },
      error: (err) => {
        console.error('Error fetching request details', err);
        this.isLoading = false;
      }
    });
  }

  resubmitFile: File | null = null;
  isResubmitting = false;

  onResubmitFileChange(event: any) {
    const files = event.target.files;
    if (files.length > 0) {
      this.resubmitFile = files[0];
    }
  }

  submitResubmission() {
    if (!this.resubmitFile || !this.requestId) return;

    this.isResubmitting = true;
    const formData = new FormData();
    formData.append('MainDocument', this.resubmitFile);

    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    this.http.post(`http://localhost:5058/api/CitizenRequest/resubmit/${this.requestId}`, formData, { headers }).subscribe({
      next: (res: any) => {
        alert('Document replaced and request resubmitted successfully!');
        this.resubmitFile = null;
        this.isResubmitting = false;
        this.fetchRequestDetails(this.requestId!);
      },
      error: (err) => {
        console.error('Error during document replacement', err);
        alert(err?.error?.message || 'Failed to replace document. Please try again.');
        this.isResubmitting = false;
      }
    });
  }

  downloadFinalVerifiedDocument() {
    if (!this.requestDetails) return;
    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    this.http.get(`http://localhost:5058/api/CitizenRequest/citizen/download-final/${this.requestId}`, {
      headers,
      responseType: 'blob'
    }).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        const isCert = this.requestDetails.serviceType?.toLowerCase().includes('request');
        a.download = isCert ? `certificate_${this.requestId}.pdf` : `verified_${this.requestId}.pdf`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('Download failed', err);
        alert('Failed to download document.');
      }
    });
  }

  getFinalVerifiedDocUrl(): string {
    if (!this.requestDetails) return '#';
    return `http://localhost:5058/api/CitizenRequest/citizen/download-final/${this.requestId}`;
  }

  get finalVerifiedDocument(): any {
    if (!this.requestDetails || !this.requestDetails.documents) return null;
    return this.requestDetails.documents.find((d: any) => d.documentRole === 'FINAL_VERIFIED_DOCUMENT');
  }

  get generatedCertificate(): any {
    if (!this.requestDetails || !this.requestDetails.documents) return null;
    return this.requestDetails.documents.find((d: any) => d.documentRole === 'GENERATED_CERTIFICATE');
  }

  goBack() {
    this.router.navigate(['/citizen/service-requests']);
  }
}
