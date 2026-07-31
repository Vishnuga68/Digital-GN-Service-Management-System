import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute, Router } from '@angular/router';
import { HttpClient, HttpHeaders } from '@angular/common/http';

@Component({
  selector: 'app-request-details-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './request-details-page.component.html',
  styleUrls: ['./request-details-page.component.scss']
})
export class RequestDetailsPageComponent implements OnInit {
  isLoading = true;
  isGeneratingLoading = false;
  requestId: string | null = null;
  requestDetails: any = null;
  updateStatusValue: string = '';
  newRemark: string = '';
  editingRemarkId: number | null = null;
  editingRemarkText: string = '';
  hasReassignmentWarning = false;

  showConfirmModal = false;
  showAcceptConfirmModal = false;
  showMissingDetailsModal = false;
  showNotificationModal = false;
  showReleaseConfirmModal = false;
  showRevokeModal = false;
  revokeReason = '';
  notificationTitle = '';
  notificationMessage = '';
  notificationType = 'success';
  isProcessing = false;

  showStartReviewButton = false;
  showAcceptButton = false;
  showRejectButton = false;
  showReleaseButton = false;
  showGenerateCertificateButton = false;
  showReleaseCertificateButton = false;
  isDocumentRequest = false;
  isSimpleRequestFlow = false;
  showVerifyButton = false;
  isRemarkEditable = false;
  showSimpleStartReviewConfirm = false;
  showSimpleVerifyConfirm = false;
  showSimpleAcceptConfirm = false;
  showSimpleRejectConfirm = false;
  isRemarkReadOnlyVisible = false;

  constructor(private route: ActivatedRoute, private router: Router, private http: HttpClient) {
    const navigation = this.router.getCurrentNavigation();
    if (navigation?.extras?.state?.['showGeneratingLoading']) {
      this.isGeneratingLoading = true;
    }
  }

  ngOnInit(): void {
    this.requestId = this.route.snapshot.paramMap.get('id');
    
    const hasSuccessParam = this.route.snapshot.queryParamMap.get('success') === 'true';
    if (this.requestId && !hasSuccessParam) {
      this.fetchRequestDetails(this.requestId);
    } else if (!this.requestId) {
      this.isLoading = false;
    }

    this.route.queryParams.subscribe(params => {
      if (params['success'] === 'true') {
        this.showNotificationModal = true;
        this.notificationType = 'success';
        this.notificationTitle = 'Success';
        this.notificationMessage = 'Document verification finalized successfully! The verified PDF is generated.';
        
        if (this.requestId) {
          this.fetchRequestDetails(this.requestId);
        }
        
        // Clear query parameters
        this.router.navigate([], {
          relativeTo: this.route,
          queryParams: { success: null },
          queryParamsHandling: 'merge'
        });
      }
    });
  }

  fetchRequestDetails(id: string) {
    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    this.http.get(`http://localhost:5058/api/CitizenRequest/details/${id}`, { headers }).subscribe({
      next: (data: any) => {
        this.requestDetails = data;
        this.requestDetails.id = data.requestId;
        this.requestDetails.requestDbId = data.requestDbId;
        
        // Map the backend status to display status and step number
        const status = this.requestDetails.currentStatus;
        const statusUpper = status ? status.toUpperCase() : '';
        // Determine if this is a Document Request where certificate generation incorrectly sets status to UNDER_REVIEW
        const isDocumentRequest = this.requestDetails.category && this.requestDetails.category.includes('Document Request');
        let effectiveStatus = statusUpper;
        if (isDocumentRequest && statusUpper === 'UNDER_REVIEW') {
          // Treat as VERIFIED for UI purposes
          effectiveStatus = 'VERIFIED';
        }
        let step = 2; // Default In Progress
        if (effectiveStatus === 'SUBMITTED' || effectiveStatus === 'PENDING') {
          step = 1;
        } else if (effectiveStatus === 'UNDER_REVIEW' || effectiveStatus === 'CHANGES_REQUESTED' || effectiveStatus === 'RESUBMITTED' || effectiveStatus === 'IN_PROGRESS' || effectiveStatus === 'IN PROGRESS') {
          // GN module: when there is no explicit Under Review entry, treat as Pending
          step = 1;
          this.requestDetails.status = 'Pending';
        } else if (effectiveStatus === 'VERIFIED') {
          step = 3;
        } else if (effectiveStatus === 'APPROVED' || effectiveStatus === 'REJECTED' || effectiveStatus === 'ACCEPTED' || effectiveStatus === 'REVOKED') {
          step = 4;
        }
        this.requestDetails.currentStep = step;
        // Preserve original status for display purposes
        const statusMap: any = {
          'PENDING': 'Pending',
          'Pending': 'Pending',
          'SUBMITTED': 'Pending',
          'Submitted': 'Pending',
          'UNDER_REVIEW': isDocumentRequest ? 'Verified' : 'Under Review',
          'Under Review': isDocumentRequest ? 'Verified' : 'Under Review',
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
        if (!this.requestDetails.status) { this.requestDetails.status = statusMap[status] || status; }

        if (this.requestDetails.timeline) {
          this.requestDetails.timeline.forEach((t: any) => {
            t.completed = t.step <= step;
            // Assign precise timestamps based on DTO fields when missing
            if (!t.timestamp) {
              switch (t.step) {
                case 1:
                  t.timestamp = this.requestDetails.submittedDate ? new Date(this.requestDetails.submittedDate) : null;
                  break;
                case 2:
                  // Only set timestamp for Under Review if the step is active (i.e., status is truly Under Review)
                  if (step >= 2 && this.requestDetails.lastUpdated) {
                    t.timestamp = new Date(this.requestDetails.lastUpdated);
                  } else {
                    t.timestamp = null;
                  }
                  break;
                case 3:
                  t.timestamp = this.requestDetails.verificationDetails?.verifiedDate ? new Date(this.requestDetails.verificationDetails.verifiedDate) : null;
                  break;
                case 4:
                  t.timestamp = this.requestDetails.approvedDate ? new Date(this.requestDetails.approvedDate) : (this.requestDetails.rejectedDate ? new Date(this.requestDetails.rejectedDate) : null);
                  break;
              }
            }
          });
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
        this.requestDetails.verificationDetails = data.verificationDetails;
        
        // Determine applicant info for GN View
        if (data.applicant) {
            this.requestDetails.citizenName = data.applicant.fullName;
            this.requestDetails.citizenNIC = data.applicant.nic;
            this.requestDetails.citizenAddress = data.applicant.address;
        }

        // Map Applicant Type to Applicant Category for GN Officer Display
        // Also sanitize all field values: parse JSON arrays, clean empty arrays
        if (this.requestDetails.dynamicFields) {
          this.requestDetails.dynamicFields = this.requestDetails.dynamicFields.map((field: any) => {
            let sanitizedValue = this.sanitizeDynamicFieldValue(field.value);

            if (field.label && field.label.trim().toLowerCase() === 'applicant type') {
              const valLower = sanitizedValue.trim().toLowerCase();
              if (valLower === 'myself') {
                sanitizedValue = 'Account Holder';
              } else if (valLower === 'child under 18' || valLower === 'child') {
                sanitizedValue = 'Minor Child (Under 18)';
              }
              return {
                ...field,
                label: 'Applicant Category',
                value: sanitizedValue
              };
            }
            return { ...field, value: sanitizedValue };
          });
        }

        const allowedStatuses = ['In Progress', 'Verified', 'Accepted', 'Rejected', 'SUBMITTED', 'UNDER_REVIEW', 'CHANGES_REQUESTED', 'RESUBMITTED'];
        if (allowedStatuses.includes(this.requestDetails.status)) {
            this.updateStatusValue = this.requestDetails.status;
        } else if (this.requestDetails.status === 'Approved') {
            this.updateStatusValue = 'Accepted'; // 'Approved' maps to 'Accepted' in dropdown
        } else {
            this.updateStatusValue = 'In Progress'; // Default placeholder
        }

        // Map rejectionReason dynamically for GN display
        const rejectEntry = this.requestDetails.timeline?.find((t: any) => t.step === 4 && (t.title?.toLowerCase() === 'rejected' || t.title?.toLowerCase() === 'revoked'));
        if (rejectEntry && rejectEntry.remarks) {
          this.requestDetails.rejectionReason = rejectEntry.remarks;
        } else {
          const statusUpper2 = this.requestDetails.status ? this.requestDetails.status.toUpperCase() : '';
          if ((statusUpper2 === 'REJECTED' || statusUpper2 === 'REVOKED') && this.requestDetails.remarks && this.requestDetails.remarks.length > 0) {
            this.requestDetails.rejectionReason = this.requestDetails.remarks[0].text;
          }
        }
        
        // Detect reassignment warning
        this.hasReassignmentWarning = false;
        if (this.requestDetails.timeline) {
          this.hasReassignmentWarning = this.requestDetails.timeline.some((t: any) => 
            t.remarks && t.remarks.toLowerCase().includes('reassigned from gn division')
          );
        }
        if (!this.hasReassignmentWarning && this.requestDetails.remarks) {
          this.hasReassignmentWarning = this.requestDetails.remarks.some((r: any) => 
            r.text && r.text.toLowerCase().includes('reassigned from gn division')
          );
        }

        this.updateButtonVisibility();
        this.isLoading = false;
        this.isGeneratingLoading = false;
      },
      error: (err) => {
        console.error('Error fetching request details', err);
        this.isLoading = false;
        this.isGeneratingLoading = false;
      }
    });
  }

  promptStartReview() {
    if (!this.requestId) return;
    this.showConfirmModal = true;
  }

  cancelStartReview() {
    this.showConfirmModal = false;
  }

  confirmStartReview() {
    this.showConfirmModal = false;
    this.startVerificationReview();
  }

  startVerificationReview() {
    if (!this.requestId) return;
    this.isProcessing = true;
    const token = sessionStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };
    const body = { Remarks: this.newRemark || 'Review started.' };

    this.http.post(`http://localhost:5058/api/CitizenRequest/verification/${this.requestId}/start-review`, body, { headers }).subscribe({
      next: (res: any) => {
        this.isProcessing = false;
        this.newRemark = '';
        
        this.notificationTitle = 'Review Started';
        this.notificationMessage = 'Verification review started successfully. The request status has been updated to Under Review.';
        this.notificationType = 'success';
        this.showNotificationModal = true;

        this.fetchRequestDetails(this.requestId!);
      },
      error: (err) => {
        console.error('Error starting review', err);
        this.isProcessing = false;

        this.notificationTitle = 'Failed to Start Review';
        this.notificationMessage = err?.error?.message || 'Failed to start review. Please try again.';
        this.notificationType = 'error';
        this.showNotificationModal = true;
      }
    });
  }

  rejectVerificationRequest() {
    if (!this.requestId || !this.newRemark.trim()) {
      this.notificationTitle = 'Remark Required';
      this.notificationMessage = 'Please enter a remark or reason for rejection.';
      this.notificationType = 'error';
      this.showNotificationModal = true;
      return;
    }
    this.isProcessing = true;
    const token = sessionStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };

    this.http.post(`http://localhost:5058/api/CitizenRequest/verification/${this.requestId}/reject`, { Remarks: this.newRemark }, { headers }).subscribe({
      next: (res: any) => {
        this.newRemark = '';
        this.isProcessing = false;
        
        this.notificationTitle = 'Request Rejected';
        this.notificationMessage = 'The document verification request has been rejected successfully.';
        this.notificationType = 'success';
        this.showNotificationModal = true;

        this.fetchRequestDetails(this.requestId!);
      },
      error: (err) => {
        console.error('Error rejecting request', err);
        this.isProcessing = false;

        this.notificationTitle = 'Failed to Reject';
        this.notificationMessage = err?.error?.message || 'Failed to reject request. Please try again.';
        this.notificationType = 'error';
        this.showNotificationModal = true;
      }
    });
  }

  isNotAssignedToMe(): boolean {
    if (!this.requestDetails) return true;
    const userNic = sessionStorage.getItem('userNic');
    return this.requestDetails.assignedOfficerNic !== userNic;
  }

  isAcceptDisabled(): boolean {
    if (!this.requestDetails) return true;

    // 1. Must belong to Document Verification category
    if (!this.requestDetails.category || !this.requestDetails.category.includes('Document Verification')) return true;

    // 2. Must be assigned to authenticated GN Officer
    const userNic = sessionStorage.getItem('userNic');
    if (this.requestDetails.assignedOfficerNic !== userNic) return true;

    // 3. Current request status must be UNDER_REVIEW (or Under Review)
    const requestStatus = this.requestDetails.currentStatus;
    if (requestStatus !== 'UNDER_REVIEW' && requestStatus !== 'Under Review') return true;

    // 4. Verification status must be UNDER_REVIEW
    const verificationStatus = this.requestDetails.verificationDetails?.verificationStatus;
    if (verificationStatus !== 'UNDER_REVIEW') return true;

    // 5. One valid active main document exists
    const hasMainDoc = this.requestDetails.attachments?.some((a: any) => a.documentRole === 'MAIN_DOCUMENT' && a.isActive);
    if (!hasMainDoc) return true;

    // 6. Has not already been verified, rejected or revoked
    const isCompleted = ['VERIFIED', 'Verified', 'Rejected', 'REJECTED', 'Revoked', 'REVOKED'].includes(requestStatus) ||
                        ['VERIFIED', 'REJECTED', 'REVOKED'].includes(verificationStatus);
    if (isCompleted) return true;

    return false;
  }

  acceptRequest() {
    if (!this.requestDetails) return;
    const category = this.requestDetails.category;

    if (category && category.includes('Document Verification')) {
      this.showAcceptConfirmModal = true;
    } else {
      this.notificationTitle = 'Not Implemented';
      this.notificationMessage = 'Accept workflow is not implemented for this service category yet.';
      this.notificationType = 'info';
      this.showNotificationModal = true;
    }
  }

  cancelAccept() {
    this.showAcceptConfirmModal = false;
  }

  confirmAccept() {
    this.showAcceptConfirmModal = false;
    this.handleDocumentVerificationAccept();
  }

  handleDocumentVerificationAccept() {
    if (!this.requestDetails) return;

    if (this.isAcceptDisabled()) {
      this.notificationTitle = 'Action Not Allowed';
      this.notificationMessage = 'This request cannot be accepted. Please make sure the review has started and it is assigned to you.';
      this.notificationType = 'error';
      this.showNotificationModal = true;
      return;
    }

    this.isProcessing = true;
    const token = sessionStorage.getItem('token');
    const headers = { 'Authorization': `Bearer ${token}` };

    this.http.get(`http://localhost:5058/api/CitizenRequest/verification/${this.requestId}/editor`, { headers }).subscribe({
      next: (res: any) => {
        this.isProcessing = false;
        this.router.navigate(['/gn-officer/requests', this.requestId, 'verification-editor']);
      },
      error: (err) => {
        this.isProcessing = false;
        console.error('Validation error before editor routing:', err);

        const errMsg = typeof err.error === 'string' ? err.error : (err.error?.message || '');
        
        if (err.status === 403) {
          this.notificationTitle = 'Access Denied';
          this.notificationMessage = 'This request is not assigned to you or you do not have permission to verify it.';
          this.notificationType = 'error';
          this.showNotificationModal = true;
        } else if (err.status === 400 && (errMsg.includes('signature') || errMsg.includes('stamp') || errMsg.includes('details are not configured'))) {
          this.showMissingDetailsModal = true;
        } else {
          this.notificationTitle = 'Validation Failed';
          this.notificationMessage = errMsg || 'Failed to validate digital credentials. Please try again.';
          this.notificationType = 'error';
          this.showNotificationModal = true;
        }
      }
    });
  }

  revokeVerification() {
    if (!this.requestDetails) return;
    this.revokeReason = '';
    this.showRevokeModal = true;
  }

  cancelRevoke() {
    this.showRevokeModal = false;
    this.revokeReason = '';
  }

  confirmRevoke() {
    if (!this.revokeReason.trim()) return;
    this.showRevokeModal = false;

    this.isLoading = true;
    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    this.http.post(`http://localhost:5058/api/CitizenRequest/verification/${this.requestId}/revoke`, { RevocationReason: this.revokeReason }, { headers }).subscribe({
      next: (res: any) => {
        this.isLoading = false;
        this.revokeReason = '';
        this.notificationTitle = 'Verification Revoked';
        this.notificationMessage = 'The document verification has been successfully revoked.';
        this.notificationType = 'success';
        this.showNotificationModal = true;
        this.fetchRequestDetails(this.requestId!);
      },
      error: (err) => {
        this.isLoading = false;
        this.revokeReason = '';
        console.error('Revocation failed', err);
        this.notificationTitle = 'Revocation Failed';
        this.notificationMessage = err?.error?.message || 'Failed to revoke document verification.';
        this.notificationType = 'error';
        this.showNotificationModal = true;
      }
    });
  }

  updateRequestStatus() {
    if (!this.requestId || !this.requestDetails) return;
    this.isProcessing = true;
    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    
    // UI displays "Accepted" as "Approved", let's map Accepted -> Approved
    let statusValue = this.updateStatusValue;
    if (statusValue === 'Accepted') {
      statusValue = 'Approved';
    }

    if (statusValue === 'Rejected' && !this.newRemark.trim()) {
      this.notificationTitle = 'Remark Required';
      this.notificationMessage = 'Please enter a remark or reason for rejection.';
      this.notificationType = 'error';
      this.showNotificationModal = true;
      this.isProcessing = false;
      return;
    }

    const body = {
      Status: statusValue,
      Remarks: this.newRemark.trim()
    };

    this.http.post(`http://localhost:5058/api/CitizenRequest/${this.requestId}/status`, body, { headers }).subscribe({
      next: (res: any) => {
        this.isProcessing = false;
        this.newRemark = '';
        this.fetchRequestDetails(this.requestId!);
        
        this.notificationTitle = 'Status Updated';
        this.notificationMessage = 'The request status has been updated successfully.';
        this.notificationType = 'success';
        this.showNotificationModal = true;
      },
      error: (err) => {
        this.isProcessing = false;
        console.error('Error updating status', err);
        
        this.notificationTitle = 'Failed to Update';
        this.notificationMessage = err?.error?.message || 'Failed to update request status.';
        this.notificationType = 'error';
        this.showNotificationModal = true;
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
        a.download = `verified_${this.requestId}.pdf`;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('Download failed', err);
        this.notificationTitle = 'Download Failed';
        this.notificationMessage = 'Failed to download the final verified document.';
        this.notificationType = 'error';
        this.showNotificationModal = true;
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

  promptReleaseConfirm() {
    this.showReleaseConfirmModal = true;
  }

  confirmRelease() {
    this.showReleaseConfirmModal = false;
    if (this.requestDetails && (this.requestDetails.categoryId >= 1 && this.requestDetails.categoryId <= 6)) {
      this.executeReleaseCertificate();
    } else {
      this.executeReleaseVerification();
    }
  }

  executeReleaseVerification() {
    this.isProcessing = true;
    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    const body = {
      remarks: this.newRemark || 'Document verification finalized and released.'
    };

    this.http.post(`http://localhost:5058/api/CitizenRequest/verification/${this.requestId}/release`, body, { headers })
      .subscribe({
        next: (res: any) => {
          this.isProcessing = false;
          this.notificationTitle = 'Verification Released';
          this.notificationMessage = 'The verified document has been successfully published and released to the citizen.';
          this.notificationType = 'success';
          this.showNotificationModal = true;
          this.newRemark = '';
          this.fetchRequestDetails(this.requestId!);
        },
        error: (err) => {
          this.isProcessing = false;
          console.error('Failed to release verification', err);
          this.notificationTitle = 'Release Failed';
          this.notificationMessage = err.error?.message || 'Failed to release the verified document.';
          this.notificationType = 'error';
          this.showNotificationModal = true;
        }
      });
  }

  executeReleaseCertificate() {
    if (!this.requestId) return;
    this.isProcessing = true;
    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    const body = { remarks: this.newRemark || 'Family Card updated and released.' };

    this.http.post(`http://localhost:5058/api/CitizenRequest/certificate/release/${this.requestId}`, body, { headers }).subscribe({
      next: (res: any) => {
        this.isProcessing = false;
        this.notificationTitle = 'Family Card Released';
        this.notificationMessage = 'The updated Family Card has been successfully published and released to the citizen.';
        this.notificationType = 'success';
        this.showNotificationModal = true;
        this.newRemark = '';
        this.fetchRequestDetails(this.requestId!);
      },
      error: (err) => {
        console.error('Failed to release family card', err);
        this.isProcessing = false;
        this.notificationTitle = 'Release Failed';
        this.notificationMessage = err.error?.message || 'Failed to release the Family Card.';
        this.notificationType = 'error';
        this.showNotificationModal = true;
      }
    });
  }

  /**
   * Sanitize a dynamic field value coming from Request_Data JSON.
   * Handles:
   *  - JSON array strings like '["Elderly Assistance Allowance"]' → 'Elderly Assistance Allowance'
   *  - Empty array strings '[]' → 'Not provided'
   *  - Already-parsed arrays → joined comma-separated string
   *  - Plain strings → returned as-is (trimmed)
   *  - null / undefined → 'Not provided'
   */
  sanitizeDynamicFieldValue(value: any): string {
    if (value === null || value === undefined) return 'Not provided';

    // If it's already an array (parsed earlier)
    if (Array.isArray(value)) {
      return value.length === 0 ? 'No documents uploaded' : value.join(', ');
    }

    const str = String(value).trim();

    // Try to parse if it looks like a JSON array
    if (str.startsWith('[')) {
      try {
        const parsed = JSON.parse(str);
        if (Array.isArray(parsed)) {
          return parsed.length === 0 ? 'Not provided' : parsed.join(', ');
        }
      } catch {
        // Not valid JSON – fall through and return as plain string
      }
    }

    return str || 'Not provided';
  }

  goBack() {
    this.router.navigate(['/gn-officer/requests']);
  }

  updateButtonVisibility() {
    if (!this.requestDetails) return;
    
    const vDetails = this.requestDetails.verificationDetails;
    const vStatus = vDetails?.verificationStatus?.toUpperCase();
    const hasFinalPdf = !!vDetails?.finalVerifiedAttachmentId || !!this.finalVerifiedDocument;
    const status = this.requestDetails.status;
    
    this.isSimpleRequestFlow = [44, 45, 46].includes(this.requestDetails.categoryId);

    if (this.isSimpleRequestFlow) {
      this.isDocumentRequest = false;
      const statusUpper = status ? status.toUpperCase() : '';
      const isCompleted = ['ACCEPTED', 'APPROVED', 'REJECTED', 'REVOKED'].includes(statusUpper);
      
      this.showStartReviewButton = (statusUpper === 'PENDING' || statusUpper === 'SUBMITTED') && !isCompleted;
      this.showVerifyButton = (statusUpper === 'UNDER REVIEW' || statusUpper === 'UNDER_REVIEW' || statusUpper === 'IN PROGRESS' || statusUpper === 'IN_PROGRESS') && !isCompleted;
      this.showAcceptButton = (statusUpper === 'VERIFIED') && !isCompleted;
      this.showRejectButton = (statusUpper === 'VERIFIED') && !isCompleted;
      
      this.showGenerateCertificateButton = false;
      this.showReleaseCertificateButton = false;
      this.showReleaseButton = false;
      
      const rejectStatuses = ['REJECTED', 'Rejected', 'APPROVED', 'Approved', 'ACCEPTED', 'Accepted'];
      this.isRemarkEditable = !rejectStatuses.includes(status);
      this.isRemarkReadOnlyVisible = rejectStatuses.includes(status);
      // Prepopulate the editable remark field with the latest remark if editing is allowed
      if (this.isRemarkEditable) {
        this.newRemark = this.requestDetails.rejectionReason || '';
      }
    }
    else {
      this.showVerifyButton = false;
      this.isDocumentRequest = (this.requestDetails?.serviceType?.toLowerCase().includes('request') || (this.requestDetails?.categoryId >= 1 && this.requestDetails?.categoryId <= 6)) || false;

      if (this.isDocumentRequest) {
        const hasCert = (vStatus === 'GENERATED' || vStatus === 'PUBLISHED') || !!this.generatedCertificate;
        const isCompleted = ['Rejected', 'Accepted', 'Approved', 'REVOKED', 'Revoked'].includes(status) ||
                             ['PUBLISHED', 'REJECTED', 'REVOKED'].includes(vStatus);

        this.showStartReviewButton = (!vStatus || vStatus === 'PENDING' || vStatus === 'RESUBMITTED') && !isCompleted;
        this.showGenerateCertificateButton = ((vStatus === 'UNDER_REVIEW' || vStatus === 'VERIFIED') && !hasCert) && !isCompleted;
        this.showReleaseCertificateButton = (vStatus === 'GENERATED' && hasCert) && !isCompleted;
        this.showRejectButton = (vStatus === 'UNDER_REVIEW' && !hasCert) && !isCompleted;
        this.showAcceptButton = false;
        // No release button for document request flow
        this.showReleaseButton = false;

        // Remarks editable unless request is rejected
        const rejectStatuses = ['REJECTED', 'Rejected'];
        this.isRemarkEditable = !rejectStatuses.includes(status);
        this.isRemarkReadOnlyVisible = rejectStatuses.includes(status);
      } else {
        this.showGenerateCertificateButton = false;
        this.showReleaseCertificateButton = false;
        this.showStartReviewButton = (vStatus === 'PENDING' || vStatus === 'RESUBMITTED');
        this.showAcceptButton = (vStatus === 'UNDER_REVIEW' && !hasFinalPdf);
        this.showRejectButton = (vStatus === 'UNDER_REVIEW' && !hasFinalPdf);
        // Show release button after verification regardless of final PDF presence
        this.showReleaseButton = (vStatus === 'VERIFIED');
        // Determine if remarks can be edited: editable until request is Approved or Rejected
        // Remarks editable unless request is rejected
        const rejectStatuses = ['REJECTED', 'Rejected'];
        this.isRemarkEditable = !rejectStatuses.includes(status);
        this.isRemarkReadOnlyVisible = rejectStatuses.includes(status);
      }
    }
  }

  performSimpleFlowUpdate(targetStatus: string) {
    if (!this.requestId || !this.requestDetails) return;

    if (targetStatus === 'Rejected' && (!this.newRemark || !this.newRemark.trim())) {
      this.notificationTitle = 'Remark Required';
      this.notificationMessage = 'Please enter a remark or reason for rejection.';
      this.notificationType = 'error';
      this.showNotificationModal = true;
      return;
    }

    this.isProcessing = true;
    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);

    let statusValue = targetStatus;
    if (statusValue === 'Accepted') {
      statusValue = 'Approved';
    }

    const body = {
      Status: statusValue,
      Remarks: this.newRemark ? this.newRemark.trim() : (statusValue === 'Under Review' ? 'Review started' : (statusValue === 'Verified' ? 'Request verified' : 'Request accepted'))
    };

    this.http.post(`http://localhost:5058/api/CitizenRequest/${this.requestId}/status`, body, { headers }).subscribe({
      next: (res: any) => {
        this.isProcessing = false;
        this.newRemark = '';
        this.fetchRequestDetails(this.requestId!);
        
        this.notificationTitle = 'Status Updated';
        this.notificationMessage = `The request status has been updated to ${targetStatus} successfully.`;
        this.notificationType = 'success';
        this.showNotificationModal = true;
      },
      error: (err) => {
        this.isProcessing = false;
        console.error('Error updating status', err);
        
        this.notificationTitle = 'Failed to Update';
        this.notificationMessage = err?.error?.message || 'Failed to update request status.';
        this.notificationType = 'error';
        this.showNotificationModal = true;
      }
    });
  }

  isSimpleRequestFlowCompleted(): boolean {
    if (!this.requestDetails) return false;
    const status = this.requestDetails.status ? this.requestDetails.status.toUpperCase() : '';
    return ['ACCEPTED', 'APPROVED', 'REJECTED', 'REVOKED'].includes(status);
  }

  promptSimpleStartReview() {
    this.showSimpleStartReviewConfirm = true;
  }

  confirmSimpleStartReview() {
    this.showSimpleStartReviewConfirm = false;
    this.performSimpleFlowUpdate('Under Review');
  }

  promptSimpleVerify() {
    this.showSimpleVerifyConfirm = true;
  }

  confirmSimpleVerify() {
    this.showSimpleVerifyConfirm = false;
    this.performSimpleFlowUpdate('Verified');
  }

  promptSimpleAccept() {
    this.showSimpleAcceptConfirm = true;
  }

  confirmSimpleAccept() {
    this.showSimpleAcceptConfirm = false;
    this.performSimpleFlowUpdate('Accepted');
  }

  promptSimpleReject() {
    if (!this.newRemark || !this.newRemark.trim()) {
      this.notificationTitle = 'Remark Required';
      this.notificationMessage = 'Please enter a remark or reason for rejection.';
      this.notificationType = 'error';
      this.showNotificationModal = true;
      return;
    }
    this.showSimpleRejectConfirm = true;
  }

  confirmSimpleReject() {
    this.showSimpleRejectConfirm = false;
    this.performSimpleFlowUpdate('Rejected');
  }

  navigateToCertificateEditor() {
    if (!this.requestId) return;
    this.router.navigate(['/gn-officer/requests', this.requestId, 'certificate-editor']);
  }

  releaseCertificate() {
    this.promptReleaseConfirm();
  }

  startEditRemark(remark: any) {
    this.editingRemarkId = remark.id;
    this.editingRemarkText = remark.text;
  }

  cancelEditRemark() {
    this.editingRemarkId = null;
    this.editingRemarkText = '';
  }

  saveEditedRemark(remarkId: number) {
    if (!this.editingRemarkText.trim()) {
      this.notificationTitle = 'Remark Required';
      this.notificationMessage = 'Remark text cannot be empty.';
      this.notificationType = 'error';
      this.showNotificationModal = true;
      return;
    }
    const token = sessionStorage.getItem('token');
    const headers = new HttpHeaders().set('Authorization', `Bearer ${token}`);
    
    this.http.put(`http://localhost:5058/api/CitizenRequest/remark/${remarkId}`, { Remarks: this.editingRemarkText }, { headers }).subscribe({
      next: (res: any) => {
        this.editingRemarkId = null;
        this.editingRemarkText = '';
        if (this.requestId) {
          this.fetchRequestDetails(this.requestId);
        }
      },
      error: (err) => {
        console.error('Failed to update remark', err);
        this.notificationTitle = 'Update Failed';
        this.notificationMessage = err?.error?.message || 'Failed to update remark. Please try again.';
        this.notificationType = 'error';
        this.showNotificationModal = true;
      }
    });
  }

  canEditRemarks(): boolean {
    if (!this.requestDetails) return false;
    const status = this.requestDetails.status;
    const disabledStatuses = ['Accepted', 'Rejected', 'Approved', 'ACCEPTED', 'REJECTED', 'APPROVED'];
    return !disabledStatuses.includes(status);
  }
}
