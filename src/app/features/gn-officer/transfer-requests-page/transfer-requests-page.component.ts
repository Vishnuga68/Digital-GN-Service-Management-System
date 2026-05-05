import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';

@Component({
  selector: 'app-transfer-requests-page',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './transfer-requests-page.component.html',
  styleUrls: ['./transfer-requests-page.component.scss']
})
export class TransferRequestsPageComponent implements OnInit {
  // Officer Profile Information
  officerDetails = {
    fullName: 'Nimal Perera',
    officerID: 'GN-1975-0102',
    nic: '197512345678',
    division: 'J/60',
    divisionName: 'Puliyankoodal',
    appointmentDate: new Date('2015-06-15'),
    address: 'No. 45, Temple Road, Jaffna',
    email: 'nimal.gn@example.com',
    phone: '+94 77 123 4567',
    status: 'Active',
    yearsInService: 0
  };

  // Transfer Form States
  isTransferModalOpen = false;
  transferReason = '';
  preferredDivision = '';
  selectedFile: File | null = null;
  selectedFileName = '';
  
  // Alert States
  showAlertModal = false;
  alertType: 'success' | 'error' = 'success';
  alertMessage = '';

  constructor() {}

  ngOnInit(): void {
    // Calculate service years
    const currentYear = new Date().getFullYear();
    const startYear = this.officerDetails.appointmentDate.getFullYear();
    this.officerDetails.yearsInService = currentYear - startYear;
  }

  toggleTransferModal() {
    this.isTransferModalOpen = !this.isTransferModalOpen;
    if (!this.isTransferModalOpen) {
      this.resetForm();
    }
  }

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      if (file.type === 'application/pdf' || file.type.includes('image')) {
        this.selectedFile = file;
        this.selectedFileName = file.name;
      } else {
        this.showToast('error', 'Please upload a valid PDF or Image file.');
      }
    }
  }

  removeFile() {
    this.selectedFile = null;
    this.selectedFileName = '';
  }

  submitTransferRequest() {
    if (!this.transferReason || !this.preferredDivision || !this.selectedFile) {
      this.showToast('error', 'Please complete all fields and upload the transfer letter.');
      return;
    }

    // Simulate API call
    console.log('Submitting transfer request...', {
      reason: this.transferReason,
      preferredDivision: this.preferredDivision,
      file: this.selectedFileName
    });

    setTimeout(() => {
      this.toggleTransferModal();
      this.showToast('success', 'Transfer request submitted successfully. You will be notified of the decision via the mobile app and email.');
    }, 1500);
  }

  showToast(type: 'success' | 'error', message: string) {
    this.alertType = type;
    this.alertMessage = message;
    this.showAlertModal = true;
  }

  closeAlert() {
    this.showAlertModal = false;
  }

  resetForm() {
    this.transferReason = '';
    this.preferredDivision = '';
    this.selectedFile = null;
    this.selectedFileName = '';
  }
}
